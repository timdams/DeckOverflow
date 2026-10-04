using DeckOverflow.Engine.Belts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Components;

/// <summary>
/// De enige plek die met de stage van de band praat. De stage tekent en speelt af; ze kent geen regels.
/// Zonder stage (ze laadde niet) werkt de spike gewoon verder, zonder beeld.
/// </summary>
public sealed class BeltStage(IJSRuntime js) : IAsyncDisposable
{
    private IJSObjectReference? _stage;

    public async Task InitAsync<T>(ElementReference host, DotNetObjectReference<T> page) where T : class
    {
        if (_stage is not null) return;
        try
        {
            var stage = await js.InvokeAsync<IJSObjectReference>("import", "./stage/stage.js");
            await stage.InvokeVoidAsync("init", host, page);
            _stage = stage;
        }
        catch (JSException e)
        {
            Console.WriteLine($"De stage laadde niet: {e.Message}");
        }
    }

    /// <summary>Een nieuwe puzzel: het rooster en de tekening van wat er besteld is.</summary>
    public ValueTask SetLevelAsync(Level level, string product) =>
        Call("setLevel", new { width = level.Width, height = level.Height, product });

    public ValueTask SetBoardAsync(Level level, IReadOnlyDictionary<Cell, Piece> board) =>
        Call("setBoard", (object)Cells(level, board));

    public ValueTask SelectAsync(Cell? cell) => Call("select", cell?.X, cell?.Y);

    /// <summary>Eén tik: het product rijdt naar zijn vakje. Klaar als het er is.</summary>
    public ValueTask FrameAsync(Frame frame, Value expected, string product, int ms, bool instant) =>
        Call("frame", new
        {
            x = frame.At.X,
            y = frame.At.Y,
            value = frame.Value.ToString(),
            kind = PieceText.KindClass(frame.Value.Kind),
            note = frame.Note.ToString(),
            gate = frame.Gate,
            growth = Growth(frame.Value, expected),
            counters = frame.Counters.Select(c => new[] { c.Key.X, c.Key.Y, c.Value }).ToArray(),
            product,
        }, ms, instant);

    /// <summary>De afloop van een testgeval. Klaar als het product weg is (of afgekeurd).</summary>
    public ValueTask EndingAsync(Ending ending) => Call("ending", new { ending = ending.ToString() });

    public ValueTask ClearCrateAsync() => Call("clearCrate");

    /// <summary>Hoe ver het product is tegenover de bestelling: de stage laat het groeien.</summary>
    private static double Growth(Value value, Value expected) => value.Kind == Kind.String
        ? (double)value.Text.Length / Math.Max(1, expected.Text.Length)
        : expected.Number == 0 ? 1 : value.Number / expected.Number;

    private static object[] Cells(Level level, IReadOnlyDictionary<Cell, Piece> board) =>
        [.. level.Layout(board).Select(p => (object)(p.Value switch
        {
            Belt b => new { x = p.Key.X, y = p.Key.Y, kind = "belt", @out = b.Out.ToString(), @fixed = !level.Free(p.Key) },
            Machine m => new { x = p.Key.X, y = p.Key.Y, kind = "machine", text = PieceText.Op(m.Op), @out = m.Out.ToString(), @fixed = !level.Free(p.Key) },
            Gate g => new { x = p.Key.X, y = p.Key.Y, kind = "gate", text = PieceText.Condition(g.When), ifTrue = g.IfTrue.ToString(), ifFalse = g.IfFalse.ToString(), @fixed = !level.Free(p.Key) },
            Counter c => new { x = p.Key.X, y = p.Key.Y, kind = "counter", times = c.Times, loop = c.Loop.ToString(), done = c.Done.ToString(), @fixed = !level.Free(p.Key) },
            Source s => new { x = p.Key.X, y = p.Key.Y, kind = "source", @out = s.Out.ToString(), @fixed = true },
            Output => (object)new { x = p.Key.X, y = p.Key.Y, kind = "output", @fixed = true },
            _ => new { x = p.Key.X, y = p.Key.Y, kind = "empty", @fixed = false },
        }))];

    private ValueTask Call(string method, params object?[] args) =>
        _stage is null ? ValueTask.CompletedTask : _stage.InvokeVoidAsync(method, args);

    public async ValueTask DisposeAsync()
    {
        if (_stage is null) return;
        try
        {
            await _stage.InvokeVoidAsync("dispose");
            await _stage.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        _stage = null;
    }
}
