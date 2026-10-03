using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Runs;
using DeckOverflow.Engine.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Pages;

/// <summary>
/// Dunne shell rond een hele run: vertaalt klikken naar commands, geeft gevechtsevents
/// door aan de stage en toont de andere schermen uit de snapshot. Geen spelregels hier.
/// </summary>
public partial class RunPage
{
    [SupplyParameterFromQuery(Name = "seed")]
    public string? SeedQuery { get; set; }

    /// <summary>Om één vijand snel te proberen: een run van één knoop.</summary>
    [SupplyParameterFromQuery(Name = "fight")]
    public string? FightQuery { get; set; }

    [Inject] private HttpClient Http { get; set; } = default!;

    /// <summary>Waar de startpunten in de browser staan: de hoogste act die je ooit bereikte.</summary>
    private const string ReachedActKey = "deckoverflow.reached-act";

    /// <summary>Open Codex-pagina's, over runs heen: per pagina de getallen van het eerste moment.</summary>
    private const string CodexKey = "deckoverflow.codex";

    /// <summary>Hoe ver elke Codex-pagina gelezen is, over runs heen.</summary>
    private const string CodexReadKey = "deckoverflow.codex-read";

    /// <summary>Verdiende ✗-panelen, over runs heen.</summary>
    private const string XPanelsKey = "deckoverflow.xpanels";

    private ElementReference _host;
    private DotNetObjectReference<RunPage>? _self;
    private Run? _run;
    private RunSnapshot? _snap;
    private double _loadMs;
    private bool _starting;
    private bool _busy;
    private bool _showDeck;
    private int _startAct = 1;
    private int _reachedAct = 1;

    private Dictionary<string, IReadOnlyDictionary<string, string>> _codex = [];
    /// <summary>Pagina's die na het laatste gevecht opengingen, voor de melding op het volgende scherm.</summary>
    private readonly List<string> _newPages = [];
    private bool _showCodex;
    private string? _codexFocus;
    private Dictionary<string, int> _codexRead = [];
    private readonly HashSet<string> _xpanels = [];
    private bool _showXRegister;

    /// <summary>Een verdiend paneel dat net opsprong.</summary>
    private sealed record PanelPopup(int Id, string Key);
    private readonly List<PanelPopup> _panelPopups = [];

    /// <summary>Zo lang blijft een paneel staan voor het weer wegzakt; gelijk aan de animatie in run.css.</summary>
    private const int PanelPopupMs = 4600;

    /// <summary>De vijand van het laatste gevecht, voor het verhaal in de Codex op het beloningsscherm.</summary>
    private string? _lastEnemy;
    private Picker? _picker;
    private readonly List<Toast> _toasts = [];
    private int _toastId;

    /// <summary>Een kaart uit je deck kiezen, en welk command daar dan uit volgt.</summary>
    private sealed record Picker(string Title, string? Hint, IReadOnlyList<int>? Eligible, bool ShowUpgrade, Func<int, ICommand> Command);

    private sealed record Toast(int Id, string Text, string Class);

    protected override async Task OnInitializedAsync()
    {
        if (!S.Loaded) await S.LoadAsync(Http);
        if (!Art.Loaded) await Art.LoadAsync(Http);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        // Meetpunt "laadtijd tot speelbaar": de Speel-knop staat op het scherm
        _loadMs = await JS.InvokeAsync<double>("deckOverflow.now");

        string? reached = await JS.InvokeAsync<string?>("deckOverflow.load", ReachedActKey);
        if (int.TryParse(reached, out int act) && act > 1) _reachedAct = Math.Min(act, Acts.All[^1].Number);

        string? codex = await JS.InvokeAsync<string?>("deckOverflow.load", CodexKey);
        if (!string.IsNullOrEmpty(codex))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(codex) ?? [];
                _codex = saved.ToDictionary(p => p.Key, p => (IReadOnlyDictionary<string, string>)p.Value);
            }
            catch (JsonException) { /* kapotte opslag: gewoon met een lege Codex verder */ }
        }

        _codexRead = await LoadJsonAsync<Dictionary<string, int>>(CodexReadKey) ?? [];
        foreach (string panel in await LoadJsonAsync<List<string>>(XPanelsKey) ?? []) _xpanels.Add(panel);
        StateHasChanged();
    }

    /// <summary>Start na een klik, zodat de browser audio toelaat.</summary>
    private async Task StartAsync(int startAct)
    {
        _starting = true;
        _startAct = startAct;
        _self = DotNetObjectReference.Create(this);
        await Stage.InitAsync(_host, _self, _loadMs);

        ulong seed = ulong.TryParse(SeedQuery, out ulong s) ? s : NewSeed();
        await NewRunAsync(seed);
        _starting = false;
    }

    private async Task NewRunAsync(ulong seed)
    {
        RunSetup setup = Bestiary.Exists(FightQuery)
            ? new RunSetup(Map: SingleFight(FightQuery!), Opening: false, Deck: TestDeck)
            : new RunSetup(StartAct: _startAct);
        _run = Run.Start(seed, setup);
        _snap = _run.Snapshot();
        _picker = null;
        _showDeck = false;
        _lastEnemy = null;
        _toasts.Clear();

        var query = new Dictionary<string, object?> { ["seed"] = seed.ToString(CultureInfo.InvariantCulture) };
        if (FightQuery is not null) query["fight"] = FightQuery;
        Nav.NavigateTo(Nav.GetUriWithQueryParameters(query), replace: true);

        await Stage.ResetAsync();
    }

    /// <summary>Voor <c>?fight=</c>: het starterdeck plus de kaarten die de puzzelvijanden nodig hebben.</summary>
    private static IReadOnlyList<CardDefinition> TestDeck =>
        [.. CardCatalog.StarterDeck(), CardCatalog.RemoldByte, CardCatalog.MeasureTwice, CardCatalog.ReadTheLabel, CardCatalog.Ink, CardCatalog.Read];

    private static ActMap SingleFight(string enemy)
    {
        NodeKind kind = Bestiary.Bosses.Contains(enemy) ? NodeKind.Boss
            : Bestiary.Elites.Contains(enemy) ? NodeKind.Elite
            : NodeKind.Fight;
        return ActMap.Path((kind, enemy), (NodeKind.Rest, null));
    }

    // ---------- Buiten het gevecht ----------

    private Task ChooseNodeAsync(int nodeId) => DoAsync(new ChooseNode(nodeId));

    private async Task DoAsync(ICommand command)
    {
        if (_run is null || _busy) return;

        var before = _snap!;
        var events = _run.Handle(command);
        _snap = _run.Snapshot();
        ShowToasts(events);
        await RememberActAsync(events);
        await RememberCodexAsync(events);
        await RememberXPanelsAsync(events);

        // Een nieuw gevecht: de stage begint met een schone lei
        if (_snap.Phase == RunPhase.Combat && before.Phase != RunPhase.Combat)
        {
            await Stage.ResetAsync();
            await Stage.SyncAsync(_run.CombatSnapshot()!);
        }
    }

    private Task ChooseEventOptionAsync(int index, EventOptionView option)
    {
        if (option.EligibleCards.Count == 0) return DoAsync(new ChooseEventOption(index));

        _picker = new Picker(S.T(option.Label), S.T(option.Detail), option.EligibleCards, ShowUpgrade: false,
            i => new ChooseEventOption(index, i));
        return Task.CompletedTask;
    }

    private void PickUpgrade(RestView rest) =>
        _picker = new Picker(S.T("ui.rest.pick"), null, rest.UpgradableCards, ShowUpgrade: true, i => new RestUpgrade(i));

    private void PickRemoval(ShopView shop) =>
        _picker = new Picker(S.T("ui.shop.remove.pick"), S.T("ui.shop.remove.hint", ("price", shop.RemovalPrice)), null, ShowUpgrade: false, i => new BuyRemoval(i));

    private async Task PickedAsync(int deckIndex)
    {
        if (_picker is not { } picker) return;
        _picker = null;
        await DoAsync(picker.Command(deckIndex));
        StateHasChanged();
    }

    // ---------- Gevecht ----------

    /// <summary>Aangeroepen door de stage wanneer een kaart op een doelwit valt.</summary>
    [JSInvokable]
    public Task OnCardPlayed(int handIndex, int targetId) =>
        RunCombatAsync(new PlayCard(handIndex, targetId));

    private Task EndTurnAsync() => RunCombatAsync(new EndTurn());

    /// <summary>TIJDELIJK: sneltoets W wint het lopende gevecht meteen.</summary>
    [JSInvokable]
    public Task OnDebugWin() => RunCombatAsync(new DebugWin());

    private async Task RunCombatAsync(ICommand command)
    {
        if (_run is null || _busy || _run.Phase != RunPhase.Combat) return;

        _busy = true;
        await InvokeAsync(StateHasChanged);
        try
        {
            _lastEnemy = _run.CombatSnapshot()?.Combatants.FirstOrDefault(c => c.IsEnemy)?.Key;
            if (_run.CombatSnapshot()?.Turn == 1) _newPages.Clear();
            long start = Stopwatch.GetTimestamp();
            var events = _run.Handle(command);
            double engineMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;

            await Stage.PlayAsync(events, engineMs);
            if (_run.CombatSnapshot() is { } combat) await Stage.SyncAsync(combat);

            // Even blijven staan op VICTORY of CRASHED voor het volgende scherm komt
            if (_run.Phase != RunPhase.Combat) await Task.Delay(700);

            _snap = _run.Snapshot();
            // Pas na het gevecht: goud, relic. Tijdens het gevecht spreekt de stage.
            if (_snap.Phase != RunPhase.Combat) ShowToasts(events);
            await RememberCodexAsync(events);
            await RememberXPanelsAsync(events);
        }
        finally
        {
            _busy = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Een nieuwe act bereikt: die wordt een startpunt voor volgende runs.</summary>
    private async Task RememberActAsync(IEnumerable<GameEvent> events)
    {
        foreach (var started in events.OfType<ActStarted>())
        {
            if (started.Act <= _reachedAct) continue;
            _reachedAct = started.Act;
            await JS.InvokeVoidAsync("deckOverflow.save", ReachedActKey, _reachedAct.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>Nieuwe Codex-pagina's bewaren. Een pagina houdt de getallen van het eerste moment waarop ze openging.</summary>
    private async Task RememberCodexAsync(IEnumerable<GameEvent> events)
    {
        bool changed = false;
        foreach (var unlocked in events.OfType<CodexUnlocked>())
        {
            if (_codex.ContainsKey(unlocked.Key)) continue;
            _codex[unlocked.Key] = unlocked.Values;
            _newPages.Add(unlocked.Key);
            changed = true;
        }
        if (changed) await JS.InvokeVoidAsync("deckOverflow.save", CodexKey, JsonSerializer.Serialize(_codex));
    }

    /// <summary>Een Codex-blad omgeslagen. Wie een pagina tot het einde leest, overtreedt de laatste regel.</summary>
    private async Task TurnCodexPageAsync((string Key, int Layer) turn)
    {
        if (turn.Layer <= _codexRead.GetValueOrDefault(turn.Key, 1)) return;
        _codexRead[turn.Key] = turn.Layer;
        await JS.InvokeVoidAsync("deckOverflow.save", CodexReadKey, JsonSerializer.Serialize(_codexRead));
        if (turn.Layer >= 4) await EarnXPanelAsync(Engine.Achievements.XRegister.ReadTheManual);
    }

    /// <summary>Panelen uit de motor bewaren en melden.</summary>
    private async Task RememberXPanelsAsync(IEnumerable<GameEvent> events)
    {
        foreach (var earned in events.OfType<XPanelEarned>()) await EarnXPanelAsync(earned.Key);
    }

    private async Task EarnXPanelAsync(string key)
    {
        if (!_xpanels.Add(key)) return;
        var popup = new PanelPopup(++_toastId, key);
        _panelPopups.Add(popup);
        _ = RemovePopupLaterAsync(popup);
        await JS.InvokeVoidAsync("deckOverflow.save", XPanelsKey, JsonSerializer.Serialize(_xpanels));
    }

    private async Task RemovePopupLaterAsync(PanelPopup popup)
    {
        await Task.Delay(PanelPopupMs);
        _panelPopups.Remove(popup);
        await InvokeAsync(StateHasChanged);
    }

    private async Task<T?> LoadJsonAsync<T>(string key)
    {
        string? json = await JS.InvokeAsync<string?>("deckOverflow.load", key);
        if (string.IsNullOrEmpty(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; }
    }

    private void OpenCodex(string? focus)
    {
        _codexFocus = focus;
        _showCodex = true;
    }

    /// <summary>Een melding per nieuwe pagina; een klik opent de Codex op die pagina.</summary>
    private RenderFragment NewPages() => builder =>
    {
        int seq = 0;
        foreach (string key in _newPages)
        {
            builder.OpenElement(seq++, "button");
            builder.AddAttribute(seq++, "class", "codex-new");
            builder.AddAttribute(seq++, "onclick", EventCallback.Factory.Create(this, () => OpenCodex(key)));
            builder.AddContent(seq++, S.T("ui.codex.new", ("name", S.T($"codex.{key}.name"))));
            builder.CloseElement();
        }
    };

    // ---------- Meldingen ----------

    private void ShowToasts(IEnumerable<GameEvent> events)
    {
        foreach (var e in events)
        {
            (string Text, string Class)? toast = e switch
            {
                GoldChanged g => (S.T("ui.toast.gold", ("amount", Signed(g.Amount))), "gold"),
                RunHpChanged h when h.Amount != 0 => (S.T("ui.toast.hp", ("amount", Signed(h.Amount))), h.Amount > 0 ? "heal" : "hurt"),
                CardAdded c => (S.T(TextRef.Of("ui.toast.card-added", ("card", c.CardId))), "card"),
                CardRemoved c => (S.T(TextRef.Of("ui.toast.card-removed", ("card", c.CardId))), "card"),
                CardTransformed t => (S.T(TextRef.Of("ui.toast.card-changed", ("from", t.FromId), ("to", t.ToId))), "card"),
                RelicGained r => (S.T(TextRef.Of("ui.toast.relic", ("relic", r.RelicId))), "relic"),
                ActStarted a => (S.T("ui.act", ("act", a.Act), ("name", S.T($"act.{Acts.Get(a.Act).Key}"))), "act"),
                RunRejected r => (S.T(r.Reason), "rejected"),
                _ => null
            };
            if (toast is { } t2) AddToast(t2.Text, t2.Class);
        }
    }

    private void AddToast(string text, string cssClass)
    {
        var toast = new Toast(++_toastId, text, cssClass);
        _toasts.Add(toast);
        _ = RemoveLaterAsync(toast);
    }

    private async Task RemoveLaterAsync(Toast toast)
    {
        await Task.Delay(2600);
        _toasts.Remove(toast);
        await InvokeAsync(StateHasChanged);
    }

    // ---------- Hulp ----------

    private static string Signed(int amount) => amount > 0 ? $"+{amount}" : amount.ToString(CultureInfo.InvariantCulture);

    private static string Num(double value) => (Math.Round(value * 100) / 100).ToString(CultureInfo.InvariantCulture);

    private static string Percent(double value, double max) =>
        (max <= 0 ? 0 : Math.Clamp(value / max * 100, 0, 100)).ToString("0.#", CultureInfo.InvariantCulture);

    private static ulong NewSeed() => (ulong)Random.Shared.NextInt64(1, 1_000_000);

    public async ValueTask DisposeAsync()
    {
        await Stage.DisposeAsync();
        _self?.Dispose();
    }
}
