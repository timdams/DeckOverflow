using DeckOverflow.Engine.Belts;

namespace DeckOverflow.Tests;

/// <summary>De natuurwetten van de band: echt .NET-gedrag, en een band die de kist eerlijk volgt.</summary>
public class BeltTests
{
    private static readonly Level Open = new("test", 6, 3, new Cell(0, 1), Dir.Right, new Cell(5, 1),
        [new TestCase(Value.Int(0), Value.Int(0))], []);

    [Fact]
    public void Een_byte_loopt_over_voorbij_255()
    {
        var (result, note) = new Op(OpKind.Add, 20).Apply(Value.Byte(250));
        Assert.Equal((Value.Byte(14), Note.Overflow), (result, note));
    }

    [Fact]
    public void Een_byte_loopt_ook_onder_0_over()
    {
        var (result, note) = new Op(OpKind.Subtract, 10).Apply(Value.Byte(4));
        Assert.Equal((Value.Byte(250), Note.Overflow), (result, note));
    }

    [Fact]
    public void Een_int_delen_door_een_int_kapt_af()
    {
        var (result, note) = new Op(OpKind.Divide, 2).Apply(Value.Int(25));
        Assert.Equal((Value.Int(12), Note.Truncated), (result, note));
    }

    [Fact]
    public void Een_double_delen_houdt_de_komma()
    {
        Assert.Equal(Value.Double(12.5), new Op(OpKind.Divide, 2).Apply(Value.Double(25)).Result);
    }

    [Fact]
    public void Met_tekst_plakt_plus_ook_een_getal()
    {
        Assert.Equal(Value.String("ab4"), new Op(OpKind.Add, 4).Apply(Value.String("ab")).Result);
        Assert.Equal(Value.String("ab-"), new Op(OpKind.Append, Text: "-").Apply(Value.String("ab")).Result);
    }

    [Fact]
    public void Een_kist_op_een_leeg_vakje_valt_van_de_band()
    {
        var board = new Dictionary<Cell, Piece> { [new(1, 1)] = new Belt(Dir.Right) };
        var run = Simulator.Run(Open, board, Open.Cases[0]);
        Assert.Equal(Ending.FellOff, run.Ending);
        Assert.Equal(new Cell(2, 1), run.Frames[^1].At);
    }

    [Fact]
    public void Een_band_in_een_kring_zonder_uitweg_is_een_oneindige_lus()
    {
        var board = new Dictionary<Cell, Piece>
        {
            [new(1, 1)] = new Belt(Dir.Down),
            [new(1, 2)] = new Belt(Dir.Right),
            [new(2, 2)] = new Belt(Dir.Up),
            [new(2, 1)] = new Belt(Dir.Left),
        };
        var run = Simulator.Run(Open, board, Open.Cases[0]);
        Assert.Equal(Ending.Forever, run.Ending);
        Assert.True(run.Ticks < 20);
    }

    [Fact]
    public void Een_teller_stuurt_de_kist_n_keer_de_lus_in_en_begint_dan_opnieuw()
    {
        var level = LevelCatalog.Get("three-times");
        var board = LevelTests.WhileLoop(new Condition(CondKind.Less, 0), new Op(OpKind.Add, 4));
        board[new(2, 2)] = new Counter(3, Dir.Down, Dir.Right);

        var run = Simulator.Run(level, board, level.Cases[0]);

        Assert.Equal(3, run.Frames.Count(f => f.At == new Cell(2, 3)));
        Assert.Equal(0, run.Frames[^1].Counters[new Cell(2, 2)]);
    }

    [Fact]
    public void Hetzelfde_bord_geeft_altijd_hetzelfde_resultaat()
    {
        var level = LevelCatalog.Get("nested");
        var a = Simulator.Run(level, LevelTests.Nested());
        var b = Simulator.Run(level, LevelTests.Nested());
        Assert.Equal((a.Cycles, a.Machines), (b.Cycles, b.Machines));
    }

    [Fact]
    public void De_score_telt_machines_maar_geen_band()
    {
        var run = Simulator.Run(LevelCatalog.Get("nested"), LevelTests.Nested());
        Assert.Equal(3, run.Machines);
    }
}
