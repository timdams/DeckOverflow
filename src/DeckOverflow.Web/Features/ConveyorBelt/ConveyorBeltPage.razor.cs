using DeckOverflow.ConveyorBelt.Belts;
using DeckOverflow.Web.Art;
using DeckOverflow.Web.Backend;
using DeckOverflow.Web.Interop;
using DeckOverflow.Web.Progress;
using DeckOverflow.Web.Text;
using DeckOverflow.Web.World;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Features.ConveyorBelt;

/// <summary>
/// De shell van de Lopende Band (H6), overgenomen uit spike 9. Bevat geen spelregels: de speler legt band en machines,
/// de motor laat elke kist van begin tot einde rijden (<see cref="Simulator"/>), en de shell speelt dat tik per tik af.
/// Een speler kan de afdeling nog niet ontgrendelen; de superuser kan er al in.
/// </summary>
public partial class ConveyorBeltPage : IAsyncDisposable
{
    /// <summary>Een level rechtstreeks openen; alleen voor de superuser.</summary>
    [SupplyParameterFromQuery(Name = "level")] public string? LevelQuery { get; set; }

    [Inject] private Strings S { get; set; } = default!;
    [Inject] private ArtStyle Art { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IProgressStore Store { get; set; } = default!;
    [Inject] private ConveyorBeltStage Stage { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private Superuser Superuser { get; set; } = default!;

    private sealed record Toast(int Id, string Text);
    private sealed record PanelPopup(int Id, string Key);

    private const string BeltTool = "belt";
    private const string EraseTool = "erase";
    private const int TickMs = 280;
    /// <summary>Zo lang blijft een uitslag staan voor de volgende kist vertrekt, op elke snelheid: de animatie moet je zien.</summary>
    private const int RevealPauseMs = 1100;
    private const int PopupMs = 4600;

    private ElementReference _host;
    private DotNetObjectReference<ConveyorBeltPage>? _self;
    private bool _stageReady;
    private Cell? _shownSelection;

    private PlayerProgress _progress = new();
    /// <summary>De superuser: de afdeling en alle levels open, zonder iets in de voortgang te schrijven.</summary>
    private bool _openAll;
    private int _levelIndex;
    private readonly Dictionary<string, Dictionary<Cell, Piece>> _boards = [];
    private Dictionary<Cell, Piece> _board = [];
    private string? _tool = BeltTool;
    private Cell? _selected;
    /// <summary>De richting van de laatst gelegde of gedraaide band: een nieuwe band gaat zo verder.</summary>
    private Dir _lastDir = Dir.Right;

    private LevelRun? _run;
    private int _case;
    private int _frame;
    private bool _playing;
    private int _speed = 2;
    private CancellationTokenSource? _cts;

    /// <summary>Testgevallen waarvan de kist al aankwam in beeld: pas dan verschijnt hun uitslag, met spanning.</summary>
    private readonly HashSet<int> _revealed = [];
    /// <summary>Het testgeval dat net uitkwam: zijn rij speelt de animatie (vuurwerk of een rode stempel).</summary>
    private int _fresh = -1;
    private int _freshSeq;
    /// <summary>De band waarvan de afloop al bewaard is, zodat terugspoelen niets opnieuw telt.</summary>
    private LevelRun? _finished;

    private bool _showCodex;
    private bool _showXRegister;
    private bool _showOptions;
    private readonly List<Toast> _toasts = [];
    private readonly List<PanelPopup> _popups = [];
    private int _seq;

    private Level Level => LevelCatalog.All[_levelIndex];
    private bool Editing => _run is null;
    private CaseRun? Shown => _run?.Cases[_case];
    private Frame? Current => Shown?.Frames[Math.Min(_frame, Shown.Frames.Count - 1)];
    private bool AtEnd => Shown is { } c && _frame >= c.Frames.Count - 1;
    private bool AllDone => _run is { } r && _case == r.Cases.Count - 1 && AtEnd;
    private bool AllRevealed => _run is { } run && _revealed.Count == run.Cases.Count;

    /// <summary>De tekening van wat deze bestelling aflevert: een vijand uit de Card Hall.</summary>
    private string ProductArt => Art.Actor(Level.Product) ?? "art/actors/knight.png";

    private bool Unlocked =>
        Departments.IsOpen(Departments.Get(Departments.ConveyorBelt), _progress.Unlocks, _openAll);

    /// <summary>Wat het dichte scherm zegt: de sleutel is er al, of waar je hem vindt.</summary>
    private string LockedText => Departments.IsEarned(Departments.Get(Departments.ConveyorBelt), _progress.Unlocks)
        ? S.T("ui.world.earned") : S.T("ui.world.conveyor-belt.needs");

    protected override async Task OnInitializedAsync()
    {
        if (!S.Loaded) await S.LoadAsync(Http);
        if (!Art.Loaded) await Art.LoadAsync(Http);
        S.Changed += OnLanguageChanged;
        _progress = await Store.LoadAsync();
        _openAll = await Superuser.IsActiveAsync();
        LoadBoards();
        int found = LevelQuery is null || !_openAll ? -1 : LevelCatalog.All.ToList().FindIndex(l => l.Key == LevelQuery);
        _levelIndex = found >= 0 ? found : FirstOpen();
        LoadBoard();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!S.Loaded || !Unlocked) return;
        if (!_stageReady)
        {
            _stageReady = true;
            _self = DotNetObjectReference.Create(this);
            await Stage.InitAsync(_host, _self);
            await PushLevelAsync();
        }
        if (_selected != _shownSelection)
        {
            _shownSelection = _selected;
            await Stage.SelectAsync(_selected);
        }
    }

    private void OnLanguageChanged() => _ = InvokeAsync(async () =>
    {
        await PushLevelAsync();
        StateHasChanged();
    });

    // ---------- Bestellingen ----------

    private ConveyorBeltRecord Record(string key) =>
        _progress.ConveyorBelt.TryGetValue(key, out var record) ? record : new ConveyorBeltRecord();

    private bool Beaten(string key) => Record(key).Beaten;

    private bool Open(int index) => _openAll || index == 0 || Beaten(LevelCatalog.All[index - 1].Key);

    private int FirstOpen()
    {
        for (int i = 0; i < LevelCatalog.All.Count; i++)
            if (!Beaten(LevelCatalog.All[i].Key)) return i;
        return LevelCatalog.All.Count - 1;
    }

    private async Task SelectLevel(int index)
    {
        Stop();
        _run = null;
        _levelIndex = index;
        LoadBoard();
        await PushLevelAsync();
    }

    private async Task PushLevelAsync()
    {
        if (!_stageReady) return;
        await Stage.SetLevelAsync(Level, ProductArt, StageLabels());
        await Stage.SetBoardAsync(Level, _board);
    }

    /// <summary>De woorden die de stage toont, in de taal van het spel.</summary>
    private Dictionary<string, string> StageLabels() => new()
    {
        ["approved"] = S.T("belt.stage.approved"),
        ["rejected"] = S.T("belt.stage.rejected"),
        ["overflow"] = S.T("belt.stage.overflow"),
        ["overheat"] = S.T("belt.stage.overheat"),
        ["in"] = S.T("belt.stage.in"),
        ["out"] = S.T("belt.stage.out"),
    };

    /// <summary>Een klik op het rooster in de stage.</summary>
    [JSInvokable]
    public Task CellClicked(int x, int y) => InvokeAsync(async () =>
    {
        await ClickCellAsync(new Cell(x, y));
        StateHasChanged();
    });

    private void LoadBoards()
    {
        foreach (var (key, record) in _progress.ConveyorBelt)
            _boards[key] = record.Board
                .Select(p => (Cell: new Cell(p.X, p.Y), Piece: PieceJson.Read(p.Json)))
                .Where(p => p.Piece is not null)
                .ToDictionary(p => p.Cell, p => p.Piece!);
    }

    private void LoadBoard()
    {
        if (!_boards.TryGetValue(Level.Key, out var board) || board.Count == 0 && Level.StartPieces.Count > 0)
            _boards[Level.Key] = board = new Dictionary<Cell, Piece>(Level.StartPieces);
        _board = board;
        _selected = null;
        _tool = BeltTool;
    }

    // ---------- Bouwen ----------

    private int Used(Offer offer) => _board.Values.Count(offer.Matches);

    private Offer? OfferFor(Piece piece) => Level.Palette.FirstOrDefault(o => o.Matches(piece));

    private async Task ClickCellAsync(Cell cell)
    {
        if (!Editing || !Level.Free(cell)) return;
        bool occupied = _board.TryGetValue(cell, out var piece);

        if (_tool == EraseTool)
        {
            if (occupied) _board.Remove(cell);
            _selected = null;
        }
        else if (_tool == BeltTool && (!occupied || piece is Belt))
        {
            // Een tweede klik op band draait hem: zo leg je snel een bocht
            var placed = piece is Belt belt ? new Belt(Next(belt.Out)) : new Belt(_lastDir);
            _board[cell] = placed;
            _lastDir = placed.Out;
            _selected = cell;
        }
        else if (_tool is { } id && Level.Palette.FirstOrDefault(o => o.Id == id) is { } offer && (!occupied || piece is Belt))
        {
            if (Used(offer) >= offer.Max) return;
            Dir dir = piece is Belt b ? b.Out : _lastDir;
            _board[cell] = offer.Create(dir);
            _selected = cell;
        }
        else
        {
            _selected = occupied ? cell : null;
        }
        await SaveBoardAsync();
    }

    private static Dir Next(Dir dir) => (Dir)(((int)dir + 1) % 4);

    /// <summary>Het gekozen stuk aanpassen via het paneel: een richting, een getal of een voorwaarde.</summary>
    private async Task ChangeAsync(Func<Piece, Piece> change)
    {
        if (_selected is not { } cell || !_board.TryGetValue(cell, out var piece)) return;
        _board[cell] = change(piece);
        await SaveBoardAsync();
    }

    /// <summary>Een band in het paneel draaien: ook die richting onthouden voor de volgende band.</summary>
    private Task TurnBeltAsync(Dir dir)
    {
        _lastDir = dir;
        return ChangeAsync(_ => new Belt(dir));
    }

    private Task RemoveSelectedAsync()
    {
        if (_selected is { } cell) _board.Remove(cell);
        _selected = null;
        return SaveBoardAsync();
    }

    private async Task ClearAsync()
    {
        _board.Clear();
        foreach (var (cell, piece) in Level.StartPieces) _board[cell] = piece;
        _selected = null;
        await SaveBoardAsync();
    }

    /// <summary>Elke verandering aan het bord gaat langs hier: de stage tekent ze, en het bord wordt bewaard.</summary>
    private async Task SaveBoardAsync()
    {
        await Stage.SetBoardAsync(Level, _board);
        var record = _progress.ConveyorBelt.GetValueOrDefault(Level.Key) ?? new ConveyorBeltRecord();
        record.Board = [.. _board.Select(p => new BeltPiece(p.Key.X, p.Key.Y, PieceJson.Write(p.Value)))];
        _progress.ConveyorBelt[Level.Key] = record;
        await Store.SaveAsync(_progress);
    }

    // ---------- Afspelen ----------

    private async Task RunAsync()
    {
        _run = Simulator.Run(Level, _board);
        _selected = null;
        await RestartAsync();
    }

    /// <summary>Hetzelfde bord nog eens laten draaien, ook na een overwinning: met dezelfde spanning en hetzelfde vuurwerk.</summary>
    private async Task RestartAsync()
    {
        Stop();
        _case = 0;
        _frame = 0;
        _revealed.Clear();
        _fresh = -1;
        await Stage.ClearCrateAsync();
        await ShowFrameAsync(instant: true);
        await PlayAsync();
    }

    private async Task PlayAsync()
    {
        if (_run is null) return;
        _playing = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        try
        {
            while (!token.IsCancellationRequested && !AllDone)
            {
                if (AtEnd)
                {
                    await Task.Delay(RevealPauseMs, token);
                    _case++;
                    _frame = 0;
                    await Stage.ClearCrateAsync();
                    await ShowFrameAsync(instant: true);
                }
                else
                {
                    _frame++;
                    StateHasChanged();
                    // De stage laat het product rijden; klaar als het op zijn vakje staat
                    await ShowFrameAsync(instant: false);
                    if (token.IsCancellationRequested) break;
                    if (AtEnd)
                    {
                        await Stage.EndingAsync(Shown!.Ending);
                        await RevealAsync(_case);
                    }
                }
                StateHasChanged();
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            _playing = false;
            StateHasChanged();
        }
    }

    private void Stop()
    {
        _cts?.Cancel();
        _playing = false;
    }

    private async Task StepOnce()
    {
        Stop();
        if (_run is null) return;
        if (AtEnd && _case < _run.Cases.Count - 1) { _case++; _frame = 0; await Stage.ClearCrateAsync(); }
        else if (!AtEnd) _frame++;
        await ShowFrameAsync(instant: false);
        if (AtEnd && !_revealed.Contains(_case))
        {
            await Stage.EndingAsync(Shown!.Ending);
            await RevealAsync(_case);
        }
    }

    private ValueTask ShowFrameAsync(bool instant) =>
        Current is { } f && Shown is { } shown
            ? Stage.FrameAsync(f, shown.Case.Expected, ProductArt, TickMs / _speed, instant)
            : ValueTask.CompletedTask;

    /// <summary>De kist van dit testgeval kwam aan in beeld: toon de uitslag, één keer met animatie.</summary>
    private async Task RevealAsync(int index)
    {
        if (!_revealed.Add(index)) return;
        _fresh = index;
        _freshSeq++;
        if (AllRevealed) await FinishAsync();
    }

    private async Task ShowCase(int index)
    {
        Stop();
        _case = index;
        _frame = Shown!.Frames.Count - 1;
        await ShowFrameAsync(instant: true);
        await RevealAsync(index);
    }

    private async Task Scrub(ChangeEventArgs e)
    {
        Stop();
        if (int.TryParse(e.Value?.ToString(), out int frame)) _frame = frame;
        await ShowFrameAsync(instant: true);
        if (AtEnd) await RevealAsync(_case);
    }

    private async Task Edit()
    {
        Stop();
        _run = null;
        await Stage.ClearCrateAsync();
    }

    // ---------- Afloop: score, Codex, panelen ----------

    /// <summary>Eén keer per band, zodra alle kisten in beeld aankwamen, ook via de tijdlijn.</summary>
    private async Task FinishAsync()
    {
        if (_run is not { } run || _finished == run) return;
        _finished = run;

        if (run.Solved)
        {
            var record = _progress.ConveyorBelt.GetValueOrDefault(Level.Key) ?? new ConveyorBeltRecord();
            record.BestMachines = Math.Min(record.BestMachines ?? int.MaxValue, run.Machines);
            record.BestCycles = Math.Min(record.BestCycles ?? int.MaxValue, run.Cycles);
            _progress.ConveyorBelt[Level.Key] = record;
        }

        // Ook een band die niet lukte, leert iets: een oneindige loop voel je het best als hij misloopt
        foreach (var moment in run.Moments)
        {
            if (!_progress.Codex.TryAdd(moment.Key, moment.Values)) continue;
            ShowToast(S.T("ui.codex.new", ("name", S.T($"codex.{moment.Key}.name"))));
        }
        foreach (string panel in run.Panels)
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
        _self?.Dispose();
    }
}
