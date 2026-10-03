using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Relics;

namespace DeckOverflow.Tests;

/// <summary>Vijf relics, elk rond één C#-regel: ?:, %, overflow, int.TryParse en afkappen.</summary>
public class NewRelicTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CombatSetup With(string enemy, string relic, params CardDefinition[] extra) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), [.. TestHelpers.TestDeck, .. extra], Relics: [relic]);

    // ---------- Ternary Plate ----------

    [Theory]
    [InlineData(20, 50, 10)]
    [InlineData(30, 50, 4)]
    [InlineData(24, 49, 4)]   // 49 / 2 is 24, en 24 < 24 is false
    public void Ternary_Plate_kiest_zijn_blok_met_een_voorwaarde(int hp, int maxHp, double block)
    {
        Assert.Equal(block, new TernaryPlateRelic().BlockAtCombatStartFor(hp, maxHp));
    }

    // ---------- Metronome ----------

    [Fact]
    public void Metronome_geeft_een_extra_energie_in_beurt_3()
    {
        var combat = Combat.Start(new CombatSetup(Scenarios.Player(200, 200), Bestiary.Create(Bestiary.Slime), TestHelpers.TestDeck, Relics: [RelicCatalog.Metronome]), 1);

        combat.Handle(new EndTurn());
        Assert.Equal(3, combat.Snapshot().Energy);
        var third = combat.Handle(new EndTurn());

        Assert.Equal(4, combat.Snapshot().Energy);
        Assert.Contains(third, e => e is RelicTriggered { RelicId: RelicCatalog.Metronome });
    }

    // ---------- Overflow Valve ----------

    [Fact]
    public void Overflow_Valve_geeft_energie_als_een_vijand_omklapt()
    {
        var combat = TestHelpers.StartWithHand(With(Bestiary.Golem, RelicCatalog.OverflowValve), CardCatalog.Mend.Id);

        var events = combat.Play(CardCatalog.Mend.Id, E);

        Assert.Contains(events, e => e is ValueOverflowed);
        Assert.Contains(events, e => e is RelicTriggered { RelicId: RelicCatalog.OverflowValve });
        Assert.Equal(3 - 1 + OverflowValveRelic.Energy, combat.Snapshot().Energy);
    }

    // ---------- TryParse Glove ----------

    [Fact]
    public void Met_de_handschoen_crasht_Read_niet_maar_wordt_ongeldige_tekst_0()
    {
        var ink = CardCatalog.Upgrade(CardCatalog.Ink)!;
        var read = CardCatalog.Upgrade(CardCatalog.Read)!;
        var combat = TestHelpers.StartWithHand(With(Bestiary.Knight, RelicCatalog.TryParseGlove, ink, read), ink.Id, read.Id, CardCatalog.FloatingStrike.Id);
        double hp = combat.Enemy().Hp;
        combat.Play(ink.Id, P);
        combat.Play(read.Id, P);

        // 2.5 + "1" is "2.51": int.Parse zou crashen, int.TryParse geeft 0
        var events = combat.Play(CardCatalog.FloatingStrike.Id, E).WithoutSeq();

        Assert.DoesNotContain(events, e => e is ExceptionThrown);
        Assert.Contains(events, e => e is ModifiersApplied m && m.Expression.Contains("int.TryParse"));
        Assert.Equal(hp, combat.Enemy().Hp);
    }

    [Fact]
    public void Zonder_handschoen_crasht_dezelfde_Read()
    {
        var ink = CardCatalog.Upgrade(CardCatalog.Ink)!;
        var read = CardCatalog.Upgrade(CardCatalog.Read)!;
        var combat = TestHelpers.StartWithHand(With(Bestiary.Knight, RelicCatalog.AnchorBarrel, ink, read), ink.Id, read.Id, CardCatalog.FloatingStrike.Id);
        combat.Play(ink.Id, P);
        combat.Play(read.Id, P);

        var events = combat.Play(CardCatalog.FloatingStrike.Id, E);

        Assert.Contains(events, e => e is ExceptionThrown { Exception: nameof(FormatException) });
    }

    // ---------- Half Shim ----------

    [Fact]
    public void Half_Shim_op_een_int_vijand_kapt_de_halve_weg()
    {
        var combat = TestHelpers.StartWithHand(With(Bestiary.PaperGolem, RelicCatalog.HalfShim), CardCatalog.Strike.Id);

        var events = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();

        Assert.Contains(events, e => e is ValueTruncated);
        Assert.Equal(32 - 6, combat.Enemy().Hp);
    }

    [Fact]
    public void Half_Shim_op_een_double_vijand_telt_elke_halve()
    {
        var combat = TestHelpers.StartWithHand(With(Bestiary.Dripper, RelicCatalog.HalfShim), CardCatalog.FloatingStrike.Id);

        combat.Play(CardCatalog.FloatingStrike.Id, E);

        // 3 × (2.5 + 0.5)
        Assert.Equal(19.5 - 9, combat.Enemy().Hp);
    }

    [Fact]
    public void De_nieuwe_relics_zitten_in_de_catalogus()
    {
        foreach (string id in new[] { RelicCatalog.TernaryPlate, RelicCatalog.Metronome, RelicCatalog.OverflowValve, RelicCatalog.TryParseGlove, RelicCatalog.HalfShim })
        {
            Assert.Contains(id, RelicCatalog.All);
            Assert.Equal(id, RelicCatalog.Create(id).Id);
        }
    }
}
