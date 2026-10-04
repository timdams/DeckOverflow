using System.Text.Json;
using DeckOverflow.Engine.Belts;
using DeckOverflow.Web.Components;
using DeckOverflow.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Pages;

/// <summary>
/// De shell van de Lopende Band. Bevat geen spelregels: de speler legt band en machines, de motor laat elke kist
/// van begin tot einde rijden (<see cref="Simulator"/>), en de shell speelt dat tik per tik af.
/// </summary>
public partial class BeltPage : IAsyncDisposable
{
    [SupplyParameterFromQuery(Name = "all")] public string? AllQuery { get; set; }
    [SupplyParameterFromQuery(Name = "level")] public string? LevelQuery { get; set; }

    [Inject] private Strings S { get; set; } = default!;
    [Inject] private HttpClient Http { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;
    [Inject] private BeltStage Stage { get; set; } = default!;

    private ElementReference _host;
    private DotNetObjectReference<BeltPage>? _self;
    private bool _stageReady;
    private Cell? _shownSelection;

    /// <summary>De tekening van wat deze puzzel aflevert.</summary>
    private string Product => Orders.Product(Level.Key);

    private const string BeltTool = "belt";
    private const string EraseTool = "erase";
    private const int TickMs = 280;

    private int _levelIndex;
    private bool _openAll;
    private readonly HashSet<string> _beaten = [];
    private readonly Dictionary<string, (int Machines, int Cycles)> _best = [];
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

    /// <summary>Het duel waarvan de score al bewaard is, zodat terugspoelen ze niet opnieuw telt.</summary>
    private LevelRun? _finished;

    /// <summary>Zo lang blijft een uitslag staan voor de volgende kist vertrekt, op elke snelheid: de animatie moet je zien.</summary>
    private const int RevealPauseMs = 1100;

    private Level Level => LevelCatalog.All[_levelIndex];
    private bool Editing => _run is null;
    private CaseRun? Shown => _run?.Cases[_case];
    private Frame? Current => Shown?.Frames[Math.Min(_frame, Shown.Frames.Count - 1)];
    private bool AtEnd => Shown is { } c && _frame >= c.Frames.Count - 1;
    private bool AllDone => _run is { } r && _case == r.Cases.Count - 1 && AtEnd;

    protected override async Task OnInitializedAsync()
    {
        await S.LoadAsync(Http);
        _openAll = AllQuery is not null || LevelQuery is not null;
        await LoadProgressAsync();
        int found = LevelQuery is null ? -1 : LevelCatalog.All.ToList().FindIndex(l => l.Key == LevelQuery);
        _levelIndex = found >= 0 ? found : Math.Min(_beaten.Count, LevelCatalog.All.Count - 1);
        LoadBoard();
    }

    // ---------- Puzzels ----------

    private bool Open(int index) => _openAll || index == 0 || _beaten.Contains(LevelCatalog.All[index - 1].Key);

    private async Task SelectLevel(int index)
    {
        Stop();
        _run = null;
        _levelIndex = index;
        LoadBoard();
        await PushLevelAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!S.Loaded) return;
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

    private async Task PushLevelAsync()
    {
        await Stage.SetLevelAsync(Level, Product);
        await Stage.SetBoardAsync(Level, _board);
    }

    /// <summary>Een klik op het rooster in de stage.</summary>
    [JSInvokable]
    public Task CellClicked(int x, int y) => InvokeAsync(async () =>
    {
        await ClickCellAsync(new Cell(x, y));
        StateHasChanged();
    });

    private void LoadBoard()
    {
        if (!_boards.TryGetValue(Level.Key, out var board))
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
        await SaveAsync();
    }

    private static Dir Next(Dir dir) => (Dir)(((int)dir + 1) % 4);

    /// <summary>Het gekozen stuk aanpassen via het paneel: een richting, een getal of een voorwaarde.</summary>
    private async Task ChangeAsync(Func<Piece, Piece> change)
    {
        if (_selected is not { } cell || !_board.TryGetValue(cell, out var piece)) return;
        _board[cell] = change(piece);
        await SaveAsync();
    }

    /// <summary>Een band in het paneel draaien: ook die richting onthouden voor de volgende band.</summary>
    private Task TurnBeltAsync(Dir dir)
    {
        _lastDir = dir;
        return ChangeAsync(_ => new Belt(dir));
    }

    /// <summary>Hetzelfde bord nog eens laten draaien, ook na een overwinning: met dezelfde spanning en hetzelfde vuurwerk.</summary>
    private Task ReplayAsync()
    {
        Stop();
        _case = 0;
        _frame = 0;
        _revealed.Clear();
        _fresh = -1;
        return ReplayFromStartAsync();
    }

    private async Task ReplayFromStartAsync()
    {
        await Stage.ClearCrateAsync();
        await ShowFrameAsync(instant: true);
        await PlayAsync();
    }

    private Task RemoveSelectedAsync()
    {
        if (_selected is { } cell) _board.Remove(cell);
        _selected = null;
        return SaveAsync();
    }

    private async Task ClearAsync()
    {
        _board.Clear();
        foreach (var (cell, piece) in Level.StartPieces) _board[cell] = piece;
        _selected = null;
        await SaveAsync();
    }

    // ---------- Afspelen ----------

    private async Task RunAsync()
    {
        _run = Simulator.Run(Level, _board);
        _case = 0;
        _frame = 0;
        _revealed.Clear();
        _fresh = -1;
        _selected = null;
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
                        Reveal(_case);
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
            Reveal(_case);
        }
    }

    private ValueTask ShowFrameAsync(bool instant) =>
        Current is { } f && Shown is { } shown
            ? Stage.FrameAsync(f, shown.Case.Expected, Product, TickMs / _speed, instant)
            : ValueTask.CompletedTask;

    /// <summary>De kist van dit testgeval kwam aan in beeld: toon de uitslag, één keer met animatie.</summary>
    private void Reveal(int index)
    {
        if (!_revealed.Add(index)) return;
        _fresh = index;
        _freshSeq++;
        if (AllRevealed) _ = FinishAsync();
    }

    private bool AllRevealed => _run is { } run && _revealed.Count == run.Cases.Count;

    private async Task ShowCase(int index)
    {
        Stop();
        _case = index;
        _frame = Shown!.Frames.Count - 1;
        await ShowFrameAsync(instant: true);
        Reveal(index);
    }

    private async Task Scrub(ChangeEventArgs e)
    {
        Stop();
        if (int.TryParse(e.Value?.ToString(), out int frame)) _frame = frame;
        await ShowFrameAsync(instant: true);
        if (AtEnd) Reveal(_case);
    }

    private async Task Edit()
    {
        Stop();
        _run = null;
        await Stage.ClearCrateAsync();
    }

    private async Task FinishAsync()
    {
        if (_run is not { Solved: true } run || _finished == run) return;
        _finished = run;
        _beaten.Add(Level.Key);
        if (!_best.TryGetValue(Level.Key, out var best))
            _best[Level.Key] = (run.Machines, run.Cycles);
        else
            _best[Level.Key] = (Math.Min(best.Machines, run.Machines), Math.Min(best.Cycles, run.Cycles));
        await SaveAsync();
    }

    // ---------- Bewaren in de browser ----------

    private sealed record Saved(List<string> Beaten, Dictionary<string, int[]> Best, Dictionary<string, List<SavedPiece>> Boards);
    private sealed record SavedPiece(int X, int Y, string Json);

    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };

    private async Task SaveAsync()
    {
        // Elke verandering aan het bord gaat langs hier: ook de stage tekent ze
        await Stage.SetBoardAsync(Level, _board);
        var saved = new Saved(
            [.. _beaten],
            _best.ToDictionary(b => b.Key, b => new[] { b.Value.Machines, b.Value.Cycles }),
            _boards.ToDictionary(b => b.Key, b => b.Value.Select(p => new SavedPiece(p.Key.X, p.Key.Y, PieceJson.Write(p.Value))).ToList()));
        await Js.InvokeVoidAsync("belt.save", "belt.progress", JsonSerializer.Serialize(saved, Json));
    }

    private async Task LoadProgressAsync()
    {
        try
        {
            string? json = await Js.InvokeAsync<string?>("belt.load", "belt.progress");
            if (string.IsNullOrEmpty(json) || JsonSerializer.Deserialize<Saved>(json, Json) is not { } saved) return;
            _beaten.UnionWith(saved.Beaten);
            foreach (var (key, best) in saved.Best) _best[key] = (best[0], best[1]);
            foreach (var (key, pieces) in saved.Boards)
                _boards[key] = pieces.Select(p => (Cell: new Cell(p.X, p.Y), Piece: PieceJson.Read(p.Json)))
                    .Where(p => p.Piece is not null).ToDictionary(p => p.Cell, p => p.Piece!);
        }
        catch (JsonException) { /* kapotte opslag: gewoon opnieuw beginnen */ }
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        await Stage.DisposeAsync();
        _self?.Dispose();
    }
}
