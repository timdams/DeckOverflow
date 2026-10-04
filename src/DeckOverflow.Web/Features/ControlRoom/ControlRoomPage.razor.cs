using System.Net.Http.Json;
using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.ControlRoom.Levels;
using DeckOverflow.Core.Codex;
using DeckOverflow.Web.Art;
using DeckOverflow.Web.Interop;
using DeckOverflow.Web.Progress;
using DeckOverflow.Web.Text;
using DeckOverflow.Web.World;
using Microsoft.AspNetCore.Components;

namespace DeckOverflow.Web.Features.ControlRoom;

/// <summary>
/// De shell van de Controlekamer. Bevat geen spelregels: ze laat de speler een regelbord opstellen, laat de motor
/// het hele duel vooraf uitrekenen (<see cref="DuelRecording"/>) en speelt het dan zet per zet af op de stage.
/// Omdat het duel al vastligt, kan de tijdlijn naar elke zet springen, ook terug.
/// </summary>
public partial class ControlRoomPage : IAsyncDisposable
{
    [SupplyParameterFromQuery(Name = "all")] public string? AllQuery { get; set; }
    [SupplyParameterFromQuery(Name = "level")] public string? LevelQuery { get; set; }

    [Inject] private Strings S { get; set; } = default!;
    [Inject] private ArtStyle Art { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IProgressStore Store { get; set; } = default!;
    [Inject] private ControlRoomStage Stage { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private sealed record LogLine(string Text, string Kind);
    private sealed record Toast(int Id, string Text);
    private sealed record PanelPopup(int Id, string Key);

    /// <summary>Zo lang blijft een melding staan; gelijk aan de animatie in run.css.</summary>
    private const int PopupMs = 4600;
    private const int LogLength = 8;

    private ElementReference _host;
    private PlayerProgress _progress = new();
    private bool _ready;
    private bool _openAll;
    private int _levelIndex;
    private List<RuleDraft> _drafts = [];
    private List<RuleDraft> _enemyDrafts = [];
    private Dictionary<string, Histogram> _histograms = [];

    private DuelRecording? _recording;
    /// <summary>De zet die nu in beeld is: -1 is de beginstand, daarna een index in de frames.</summary>
    private int _frame = -1;
    private bool _playing;
    private bool _applied;
    private int _speed = 1;
    private CancellationTokenSource? _cts;

    private bool _showCode;
    private bool _showCodex;
    private string? _codexFocus;
    private bool _showXRegister;
    private bool _showOptions;
    private bool _soundOn = true;
    private readonly List<Toast> _toasts = [];
    private readonly List<PanelPopup> _popups = [];
    private int _seq;

    private Level Level => LevelCatalog.All[_levelIndex];
    private bool Editing => _recording is null;
    private bool AtEnd => _recording is { } r && _frame == r.Frames.Count - 1;
    private DuelSnapshot? Shown => _recording is not { } r ? null : _frame < 0 ? r.Start : r.Frames[_frame].Snapshot;

    private bool Unlocked => _openAll || Departments.IsOpen(Departments.All.First(d => d.Key == Departments.ControlRoom), _progress.Unlocks);

    protected override async Task OnInitializedAsync()
    {
        if (!S.Loaded) await S.LoadAsync(Http);
        if (!Art.Loaded) await Art.LoadAsync(Http);
        S.Changed += OnLanguageChanged;
        _progress = await Store.LoadAsync();
        try { _histograms = await Http.GetFromJsonAsync<Dictionary<string, Histogram>>("control-room/solutions.json") ?? []; }
        catch (HttpRequestException) { /* zonder histogram speel je gewoon verder */ }

        _openAll = AllQuery is not null || LevelQuery is not null;
        int found = LevelQuery is null ? -1 : LevelCatalog.IndexOf(LevelQuery);
        _levelIndex = found >= 0 ? found : FirstUnbeaten();
        LoadBoard();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_ready || !Unlocked) return;
        _ready = true;
        await Stage.InitAsync(_host);
        _soundOn = await Stage.SoundOnAsync();
        await Stage.ShowAsync(Fresh());
    }

    private void OnLanguageChanged() => _ = InvokeAsync(async () =>
    {
        await Stage.SetLanguageAsync(S.Language);
        StateHasChanged();
    });

    // ---------- Gevechten kiezen ----------

    private int FirstUnbeaten()
    {
        for (int i = 0; i < LevelCatalog.All.Count; i++)
            if (!Record(LevelCatalog.All[i].Key).Beaten) return i;
        return LevelCatalog.All.Count - 1;
    }

    private bool LevelOpen(int index) => _openAll || index == 0 || Record(LevelCatalog.All[index - 1].Key).Beaten;

    private ControlRoomRecord Record(string key) =>
        _progress.ControlRoom.TryGetValue(key, out var record) ? record : new ControlRoomRecord();

    private async Task SelectLevelAsync(int index)
    {
        Stop();
        _recording = null;
        _levelIndex = index;
        LoadBoard();
        if (Stage.Ready) await Stage.ShowAsync(Fresh());
    }

    /// <summary>Het bord van dit gevecht: waaraan je laatst werkte, of het startbord.</summary>
    private void LoadBoard()
    {
        var saved = Record(Level.Key).Board;
        _drafts = (saved.Count > 0 ? saved : Level.StartRules).Select(RuleDraft.From).ToList();
        _enemyDrafts = Level.EnemyRules.Select(RuleDraft.From).ToList();
    }

    private DuelSnapshot Fresh() => LevelCatalog.Start(Level, []).Snapshot();

    private async Task BoardChangedAsync()
    {
        var record = _progress.ControlRoom.GetValueOrDefault(Level.Key) ?? new ControlRoomRecord();
        record.Board = _drafts.Select(d => d.ToRule()).ToList();
        _progress.ControlRoom[Level.Key] = record;
        await Store.SaveAsync(_progress);
    }

    // ---------- Afspelen ----------

    private async Task RunAsync()
    {
        if (_drafts.Count == 0) return;
        _recording = DuelRecording.Of(Level, _drafts.Select(d => d.ToRule()).ToList());
        _frame = -1;
        _applied = false;
        await Stage.SyncAsync(_recording.Start);
        await ResumeAsync();
    }

    private async Task ResumeAsync()
    {
        if (_recording is null || AtEnd) return;
        _playing = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        try
        {
            while (!AtEnd && !token.IsCancellationRequested)
                await AdvanceAsync();
        }
        finally
        {
            _playing = false;
            StateHasChanged();
        }
    }

    /// <summary>Eén zet: de stage speelt haar events af, daarna is de momentopname de waarheid.</summary>
    private async Task AdvanceAsync()
    {
        if (_recording is not { } r || AtEnd) return;
        int from = _frame;
        var frame = r.Frames[from + 1];
        await Stage.PlayAsync(frame.Events);
        // Intussen teruggespoeld of een ander duel: deze zet telt niet meer
        if (_recording != r || _frame != from) return;
        _frame++;
        await Stage.SyncAsync(frame.Snapshot);
        if (AtEnd) await FinishAsync();
        StateHasChanged();
    }

    private async Task StepAsync()
    {
        Stop();
        await AdvanceAsync();
    }

    private void Stop()
    {
        _cts?.Cancel();
        _playing = false;
    }

    /// <summary>De tijdlijn: naar elke zet springen, zonder animatie.</summary>
    private async Task ScrubAsync(ChangeEventArgs e)
    {
        if (_recording is not { } r || !int.TryParse(e.Value?.ToString(), out int frame)) return;
        Stop();
        _frame = Math.Clamp(frame, -1, r.Frames.Count - 1);
        await Stage.SyncAsync(Shown!);
        if (AtEnd) await FinishAsync();
    }

    private async Task SetSpeedAsync(int speed)
    {
        _speed = speed;
        if (Stage.Ready) await Stage.SetSpeedAsync(speed);
    }

    /// <summary>Terug naar het bord: het duel verdwijnt, je regels blijven.</summary>
    private async Task EditAsync()
    {
        Stop();
        _recording = null;
        _frame = -1;
        if (Stage.Ready) await Stage.ShowAsync(Fresh());
    }

    // ---------- Afloop: score, Codex, panelen ----------

    /// <summary>Eén keer per duel, zodra het einde in beeld komt, ook via de tijdlijn.</summary>
    private async Task FinishAsync()
    {
        if (_applied || _recording is not { } r) return;
        _applied = true;

        if (r.Score is { } score)
        {
            var record = _progress.ControlRoom.GetValueOrDefault(Level.Key) ?? new ControlRoomRecord();
            record.BestTurns = Math.Min(record.BestTurns ?? int.MaxValue, score.Turns);
            record.BestRules = Math.Min(record.BestRules ?? int.MaxValue, score.Rules);
            record.Board = [.. r.Rules];
            _progress.ControlRoom[Level.Key] = record;
        }

        foreach (var moment in r.Moments)
        {
            if (!_progress.Codex.TryAdd(moment.Key, moment.Values)) continue;
            ShowToast(S.T("ui.codex.new", ("name", S.T($"codex.{moment.Key}.name"))));
        }
        foreach (string panel in r.Panels)
        {
            if (!_progress.XPanels.Add(panel)) continue;
            var popup = new PanelPopup(++_seq, panel);
            _popups.Add(popup);
            _ = RemoveLaterAsync(() => _popups.Remove(popup));
        }
        await Store.SaveAsync(_progress);
    }

    private void ShowToast(string text)
    {
        var toast = new Toast(++_seq, text);
        _toasts.Add(toast);
        _ = RemoveLaterAsync(() => _toasts.Remove(toast));
    }

    private async Task RemoveLaterAsync(Func<bool> remove)
    {
        await Task.Delay(PopupMs);
        remove();
        await InvokeAsync(StateHasChanged);
    }

    private async Task NextLevelAsync()
    {
        if (_levelIndex < LevelCatalog.All.Count - 1) await SelectLevelAsync(_levelIndex + 1);
    }

    // ---------- Wat er in beeld staat ----------

    /// <summary>De regel die bij de zet in beeld vuurde, per kant, of -1.</summary>
    private int FlashOf(Side side) =>
        _recording is { } r && _frame >= 0
            ? r.Frames[_frame].Events.OfType<RuleFired>().FirstOrDefault(f => f.Side == side)?.RuleIndex ?? -1
            : -1;

    /// <summary>Het log van de laatste zetten tot de zet in beeld, nieuwste bovenaan. Volgt de tijdlijn.</summary>
    private IEnumerable<LogLine> Log()
    {
        if (_recording is not { } r) yield break;
        int shown = 0;
        for (int f = _frame; f >= 0 && shown < LogLength; f--)
        {
            foreach (var line in r.Frames[f].Events.Reverse().Select(Line).OfType<LogLine>())
            {
                yield return line;
                if (++shown >= LogLength) yield break;
            }
        }
    }

    private LogLine? Line(DuelEvent e) => e switch
    {
        TurnStarted t => new(S.T("room.log.turn", ("turn", t.Turn)), "turn"),
        NoRuleMatched n => new(S.T($"room.log.{Who(n.Side)}.none"), Who(n.Side)),
        Whacked w => new(S.T(w.WasCharged ? $"room.log.{Who(w.Side)}.whack-charged" : $"room.log.{Who(w.Side)}.whack", ("damage", w.Damage))
                         + (w.Absorbed > 0 ? " " + S.T("room.log.absorbed", ("absorbed", w.Absorbed)) : ""), Who(w.Side)),
        Braced b => new(S.T($"room.log.{Who(b.Side)}.brace", ("total", b.Total)), Who(b.Side)),
        Repaired p => new(S.T($"room.log.{Who(p.Side)}.repair", ("amount", p.Amount)), Who(p.Side)),
        RepairEmpty p => new(S.T($"room.log.{Who(p.Side)}.repair-empty"), Who(p.Side) + " wasted"),
        ValueTruncated t => new(S.T("room.log.truncated", ("before", Num(t.Before)), ("after", t.After)), Who(t.Target == Side.Player ? Side.Enemy : Side.Player) + " wasted"),
        ValueOverflowed o => new(S.T($"room.log.{Who(o.Side)}.overflow", ("before", o.Before), ("added", o.Added), ("after", o.After)), Who(o.Side)),
        ValueRounded r => new(S.T("room.log.rounded", ("before", Num(r.Before)), ("after", r.After)), Who(r.Target == Side.Player ? Side.Enemy : Side.Player)),
        TextAppended t => new(S.T("room.log.appended", ("before", t.Before), ("added", t.Added), ("after", t.After)), Who(t.Target == Side.Player ? Side.Enemy : Side.Player)),
        TextCrashed c => new(S.T("room.log.text-crashed", ("length", c.Length)), Who(c.Target == Side.Player ? Side.Enemy : Side.Player)),
        CounterOverflowed o => new(S.T($"room.log.{Who(o.Side)}.counter", ("before", o.Before), ("after", o.After)), Who(o.Side)),
        TypeChanged c => new(S.T(c.Method == "cast" ? "room.log.cast" : "room.log.convert", ("to", c.To.ToString().ToLowerInvariant()), ("before", c.Before), ("after", c.After)), Who(c.Target == Side.Player ? Side.Enemy : Side.Player)),
        TypeUnchanged u => new(S.T("room.log.unchanged", ("kind", u.Kind.ToString().ToLowerInvariant())), Who(u.Target == Side.Player ? Side.Enemy : Side.Player) + " wasted"),
        ConversionCrashed x => new(S.T("room.log.convert-crash", ("value", x.Value)), Who(x.Target == Side.Player ? Side.Enemy : Side.Player)),
        MoveSkipped k => new(S.T($"room.log.{Who(k.Side)}.skipped"), Who(k.Side) + " wasted"),
        WoundUp u => new(S.T(u.WasCharged ? $"room.log.{Who(u.Side)}.wind-up-again" : $"room.log.{Who(u.Side)}.wind-up"), Who(u.Side) + (u.WasCharged ? " wasted" : "")),
        _ => null
    };

    private static string Who(Side side) => side == Side.Player ? "player" : "enemy";

    private static string Num(double value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Hoeveel staven het histogram hoog is: de hoogste staaf vult het vak.</summary>
    private static double BarHeight(IReadOnlyDictionary<int, int> bars, int key) =>
        bars.Count == 0 ? 0 : 100.0 * bars.GetValueOrDefault(key) / bars.Values.Max();

    // ---------- Opties ----------

    private async Task OpenOptionsAsync()
    {
        if (Stage.Ready) _soundOn = await Stage.SoundOnAsync();
        _showOptions = true;
    }

    private async Task ToggleSoundAsync()
    {
        _soundOn = !_soundOn;
        if (Stage.Ready) await Stage.SetSoundAsync(_soundOn);
    }

    private void OpenCodex(string? focus)
    {
        _codexFocus = focus;
        _showCodex = true;
    }

    /// <summary>Een Codex-blad omgeslagen. Wie een pagina tot het einde leest, overtreedt de laatste regel.</summary>
    private async Task TurnCodexPageAsync((string Key, int Layer) turn)
    {
        if (turn.Layer <= _progress.CodexRead.GetValueOrDefault(turn.Key, 1)) return;
        _progress.CodexRead[turn.Key] = turn.Layer;
        if (turn.Layer >= 4 && _progress.XPanels.Add(World.XPanels.ReadTheManual))
            _popups.Add(new PanelPopup(++_seq, World.XPanels.ReadTheManual));
        await Store.SaveAsync(_progress);
    }

    private void ToFloorPlan() => Nav.NavigateTo("./");

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        S.Changed -= OnLanguageChanged;
        await Stage.DisposeAsync();
    }
}
