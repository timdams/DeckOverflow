using DeckOverflow.ConveyorBelt.Belts;

namespace DeckOverflow.ConveyorBelt.Tests;

/// <summary>
/// Per puzzel een bord dat werkt, met de hand gelegd. Zo is elke puzzel bewezen op te lossen, en tonen de tests
/// wat de puzzel wil leren. Faalt er een na het bijstellen van een puzzel, kijk dan eerst wat ze nu vraagt.
/// </summary>
public class LevelTests
{
    private static Condition C(CondKind kind, int n) => new(kind, n);

    /// <summary>
    /// De gewone lus op rij 2: een poort op (2,2) die bij <paramref name="loopWhen"/> naar beneden de lus in stuurt,
    /// de machine op (2,3), terug via (1,3) en (1,2). Dat is een while: eerst kijken, dan werken.
    /// </summary>
    public static Dictionary<Cell, Piece> WhileLoop(Condition when, Op op, bool loopWhen = true)
    {
        var board = new Dictionary<Cell, Piece>
        {
            [new(1, 2)] = new Belt(Dir.Right),
            [new(2, 2)] = loopWhen ? new Gate(when, Dir.Down, Dir.Right) : new Gate(when, Dir.Right, Dir.Down),
            [new(2, 3)] = new Machine(op, Dir.Left),
            [new(1, 3)] = new Belt(Dir.Up),
        };
        Exit(board, 3, 2);
        return board;
    }

    /// <summary>Dezelfde lus, maar de machine staat voor de poort: een do while, eerst werken, dan kijken.</summary>
    public static Dictionary<Cell, Piece> DoWhileLoop(Condition when, Op op)
    {
        var board = new Dictionary<Cell, Piece>
        {
            [new(1, 2)] = new Machine(op, Dir.Right),
            [new(2, 2)] = new Gate(when, Dir.Down, Dir.Right),
            [new(2, 3)] = new Belt(Dir.Left),
            [new(1, 3)] = new Belt(Dir.Up),
        };
        Exit(board, 3, 2);
        return board;
    }

    private static void Exit(Dictionary<Cell, Piece> board, int fromX, int y)
    {
        for (int x = fromX; x < 8; x++) board[new(x, y)] = new Belt(Dir.Right);
    }

    public static IEnumerable<(string Key, Dictionary<Cell, Piece> Board)> Solutions()
    {
        var straight = new Dictionary<Cell, Piece>();
        Exit(straight, 1, 2);
        straight[new(4, 2)] = new Machine(new Op(OpKind.Add, 5), Dir.Right);
        yield return ("first-belt", straight);

        yield return ("double-up", WhileLoop(C(CondKind.Less, 30), new Op(OpKind.Multiply, 2)));

        var counted = WhileLoop(C(CondKind.Less, 0), new Op(OpKind.Add, 4));
        counted[new(2, 2)] = new Counter(3, Dir.Down, Dir.Right);
        yield return ("three-times", counted);

        yield return ("at-least-once", DoWhileLoop(C(CondKind.Less, 10), new Op(OpKind.Add, 3)));

        yield return ("nested", Nested());

        yield return ("round-up", WhileLoop(C(CondKind.ModZero, 5), new Op(OpKind.Add, 1), loopWhen: false));
        yield return ("byte-loop", WhileLoop(C(CondKind.Greater, 100), new Op(OpKind.Add, 20)));
        yield return ("halving", WhileLoop(C(CondKind.Greater, 5), new Op(OpKind.Divide, 2)));
        yield return ("padding", WhileLoop(C(CondKind.LengthLess, 6), new Op(OpKind.Append, Text: "-")));
        yield return ("zune", ZuneFixed());
    }

    /// <summary>Een teller van 4 met daarin een teller van 6: 24 rondjes langs de +1.</summary>
    public static Dictionary<Cell, Piece> Nested()
    {
        var board = new Dictionary<Cell, Piece>
        {
            [new(1, 1)] = new Belt(Dir.Right),
            [new(2, 1)] = new Counter(4, Dir.Down, Dir.Right),
            [new(2, 2)] = new Belt(Dir.Down),
            [new(2, 3)] = new Counter(6, Dir.Right, Dir.Left),
            [new(3, 3)] = new Machine(new Op(OpKind.Add, 1), Dir.Down),
            [new(3, 4)] = new Belt(Dir.Left),
            [new(2, 4)] = new Belt(Dir.Up),
            [new(1, 3)] = new Belt(Dir.Up),
            [new(1, 2)] = new Belt(Dir.Up),
        };
        Exit(board, 3, 1);
        return board;
    }

    /// <summary>Het startbord van Zune, met een uitweg voor precies 366: een poort op (1,3) en een omweg langs onder.</summary>
    public static Dictionary<Cell, Piece> ZuneFixed()
    {
        var board = new Dictionary<Cell, Piece>(LevelCatalog.Get("zune").StartPieces)
        {
            [new(1, 3)] = new Gate(C(CondKind.Equal, 366), Dir.Left, Dir.Up),
            [new(0, 3)] = new Belt(Dir.Down),
            [new(0, 4)] = new Belt(Dir.Down),
            [new(0, 5)] = new Belt(Dir.Right),
            [new(8, 5)] = new Belt(Dir.Up),
            [new(8, 4)] = new Belt(Dir.Up),
            [new(8, 3)] = new Belt(Dir.Up),
        };
        for (int x = 1; x < 8; x++) board[new(x, 5)] = new Belt(Dir.Right);
        return board;
    }

    public static TheoryData<string> Keys => new(LevelCatalog.All.Select(l => l.Key));

    [Fact]
    public void Elke_puzzel_heeft_een_oplossing_die_mag_en_werkt()
    {
        var solved = Solutions().ToList();
        Assert.Equal(LevelCatalog.All.Count, solved.Count);
        foreach (var (key, board) in solved)
        {
            var level = LevelCatalog.Get(key);
            Assert.Empty(level.Problems(board));
            var run = Simulator.Run(level, board);
            Assert.True(run.Solved, $"{key}: " + string.Join(", ", run.Cases.Select(c => $"{c.Case.Input} → {c.Result} ({c.Ending})")));
        }
    }

    [Theory, MemberData(nameof(Keys))]
    public void Het_startbord_lost_niets_op(string key)
    {
        var level = LevelCatalog.Get(key);
        Assert.False(Simulator.Run(level, level.StartPieces).Solved);
    }

    [Fact]
    public void At_least_once_een_while_laat_50_onaangeroerd_een_do_while_niet()
    {
        var level = LevelCatalog.Get("at-least-once");
        var asWhile = Simulator.Run(level, WhileLoop(C(CondKind.Less, 10), new Op(OpKind.Add, 3)));

        Assert.False(asWhile.Solved);
        var fifty = asWhile.Cases.Single(c => c.Case.Input == Value.Int(50));
        Assert.Equal((Ending.Wrong, Value.Int(50)), (fifty.Ending, fifty.Result));
    }

    [Fact]
    public void Byte_loop_een_poort_kleiner_dan_300_stopt_nooit_want_een_byte_haalt_geen_300()
    {
        var level = LevelCatalog.Get("byte-loop");
        var run = Simulator.Run(level, WhileLoop(C(CondKind.Less, 300), new Op(OpKind.Add, 20)));

        Assert.All(run.Cases, c => Assert.Equal(Ending.Forever, c.Ending));
        Assert.Contains(run.Cases[0].Frames, f => f.Note == Note.Overflow);
    }

    [Fact]
    public void Zune_het_startbord_blijft_hangen_op_dag_366()
    {
        var level = LevelCatalog.Get("zune");
        var run = Simulator.Run(level, level.StartPieces);

        Assert.True(run.Cases.Single(c => c.Case.Input == Value.Int(400)).Passed);
        Assert.Equal(Ending.Forever, run.Cases.Single(c => c.Case.Input == Value.Int(366)).Ending);
    }

    [Fact]
    public void Three_times_kan_ook_met_twee_keer_plus_6()
    {
        var level = LevelCatalog.Get("three-times");
        var board = WhileLoop(C(CondKind.Less, 0), new Op(OpKind.Add, 6));
        board[new(2, 2)] = new Counter(2, Dir.Down, Dir.Right);
        Assert.True(Simulator.Run(level, board).Solved);
    }

    [Fact]
    public void Te_veel_machines_of_iets_wat_niet_in_de_bak_zit_mag_niet()
    {
        var level = LevelCatalog.Get("double-up");
        var board = WhileLoop(C(CondKind.Less, 30), new Op(OpKind.Multiply, 2));
        board[new(3, 2)] = new Machine(new Op(OpKind.Multiply, 2), Dir.Right);
        board[new(4, 2)] = new Machine(new Op(OpKind.Add, 7), Dir.Right);

        Assert.Equal(2, level.Problems(board).Count);
    }

    [Fact]
    public void Op_een_vast_stuk_of_de_bron_mag_je_niets_leggen()
    {
        var level = LevelCatalog.Get("zune");
        var board = new Dictionary<Cell, Piece> { [new(2, 2)] = new Belt(Dir.Right), [new(0, 2)] = new Belt(Dir.Right) };
        Assert.Equal(2, level.Problems(board).Count);
    }
}
