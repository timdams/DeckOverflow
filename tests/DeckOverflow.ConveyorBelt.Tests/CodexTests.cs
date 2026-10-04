using DeckOverflow.ConveyorBelt.Achievements;
using DeckOverflow.ConveyorBelt.Belts;
using DeckOverflow.Core.Codex;

namespace DeckOverflow.ConveyorBelt.Tests;

/// <summary>Wat de band de Codex en het ✗-register meldt: eerst ervaren, de naam komt in de Codex.</summary>
public class CodexTests
{
    private static LevelRun Solve(string key) =>
        Simulator.Run(LevelCatalog.Get(key), LevelTests.Solutions().Single(s => s.Key == key).Board);

    [Fact]
    public void Een_poort_die_de_kist_terugziet_opent_while()
    {
        var moment = Assert.Single(Solve("double-up").Moments, m => m.Key == CodexCatalog.WhileLoop);
        Assert.Equal("v < 30", moment.Values["condition"]);
    }

    [Fact]
    public void Een_teller_die_klaar_is_opent_for()
    {
        Assert.Contains(Solve("three-times").Moments, m => m.Key == CodexCatalog.ForLoop && m.Values["times"] == "3");
    }

    [Fact]
    public void Een_teller_in_een_teller_opent_geneste_loops()
    {
        var moment = Assert.Single(Solve("nested").Moments, m => m.Key == CodexCatalog.NestedLoops);
        Assert.Equal(("4", "6", "24"), (moment.Values["outer"], moment.Values["inner"], moment.Values["total"]));
    }

    [Fact]
    public void Een_band_die_nooit_stopt_opent_de_oneindige_loop_en_een_paneel()
    {
        var level = LevelCatalog.Get("zune");
        var run = Simulator.Run(level, level.StartPieces);
        Assert.Contains(run.Moments, m => m.Key == CodexCatalog.InfiniteLoop && m.Values["value"] == "366");
        Assert.Contains(XRegister.BeltForever, run.Panels);
    }

    [Fact]
    public void De_terugblik_opent_de_paginas_van_de_Card_Hall()
    {
        Assert.Contains(Solve("round-up").Moments, m => m.Key == CodexCatalog.Modulo);
        Assert.Contains(Solve("byte-loop").Moments, m => m.Key == CodexCatalog.Overflow && m.Values["target"] == "golem");
        Assert.Contains(Solve("halving").Moments, m => m.Key == CodexCatalog.IntegerDivision && m.Values["expression"] == "25 / 2");
        Assert.Contains(Solve("padding").Moments, m => m.Key == CodexCatalog.StringLength);
    }

    [Fact]
    public void De_laatste_bestelling_afleveren_sluit_de_Lopende_Band_af()
    {
        Assert.Contains(XRegister.ConveyorCleared, Solve("zune").Panels);
        Assert.DoesNotContain(XRegister.ConveyorCleared, Solve("nested").Panels);
    }

    [Fact]
    public void Een_kist_die_van_de_band_valt_verdient_een_verborgen_paneel()
    {
        var run = Simulator.Run(LevelCatalog.Get("first-belt"), new Dictionary<Cell, Piece>());
        Assert.Contains(XRegister.DroppedCrate, run.Panels);
    }
}
