using DeckOverflow.CardHall.Cards;
using DeckOverflow.Core.Codex;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.Tests;

/// <summary>
/// Twee vijanden voor act 1: de Bool Ghost (een <c>bool</c> die elke treffer omdraait) en de
/// Rhythm Turtle (een schild dat alleen open is als <c>cards % 3 == 0</c>), met de kaarten Flip en Remainder.
/// </summary>
public class BoolAndModuloTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CombatSetup Against(string enemy, params CardDefinition[] extra) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), [.. TestHelpers.TestDeck, .. extra]);

    // ---------- Bool Ghost ----------

    [Fact]
    public void Floating_Bolts_raakt_de_Bool_Ghost_maar_twee_van_de_drie_keer()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.BoolGhost), CardCatalog.FloatingStrike.Id);

        var events = combat.Play(CardCatalog.FloatingStrike.Id, E).WithoutSeq();

        // Solid: raak (2.5 kapt af tot 2), doorzichtig: erdoor, solid: raak
        Assert.Equal(2, events.OfType<DamageDealt>().Count());
        Assert.Single(events.OfType<HitPassedThrough>());
        Assert.Equal(30 - 2 - 2, combat.Enemy().Hp);
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.Booleans);
    }

    [Fact]
    public void Wie_eindigt_terwijl_hij_doorzichtig_is_krijgt_14()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.BoolGhost), CardCatalog.Strike.Id);
        combat.Play(CardCatalog.Strike.Id, E);

        var events = combat.Handle(new EndTurn()).WithoutSeq();

        var attack = Assert.Single(events.OfType<AttackLaunched>());
        Assert.Equal("false ? 6 : 14", attack.Expression);
        Assert.Equal(14, attack.Value);
    }

    [Fact]
    public void Zonder_treffer_blijft_hij_solid_en_slaat_hij_6()
    {
        var combat = Combat.Start(Against(Bestiary.BoolGhost), 1);

        var attack = Assert.Single(combat.Handle(new EndTurn()).OfType<AttackLaunched>());

        Assert.Equal(6, attack.Value);
    }

    [Fact]
    public void Flip_draait_hem_om_zonder_treffer()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.BoolGhost, CardCatalog.Flip), CardCatalog.Flip.Id, CardCatalog.Strike.Id);
        combat.Play(CardCatalog.Flip.Id, E);

        var events = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();

        // Na Flip is hij doorzichtig: de Whack gaat erdoor en maakt hem weer solid
        Assert.Single(events.OfType<HitPassedThrough>());
        Assert.Equal(30, combat.Enemy().Hp);
        Assert.Equal("isSolid = true", combat.Snapshot().Combatants.Single(c => c.IsEnemy).Rule);
    }

    [Fact]
    public void Flip_op_een_vijand_zonder_bool_compileert_niet()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Knight, CardCatalog.Flip), CardCatalog.Flip.Id);

        var events = combat.Play(CardCatalog.Flip.Id, E);

        Assert.Equal("reject.not-bool", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
    }

    // ---------- Rhythm Turtle ----------

    [Fact]
    public void Alleen_je_derde_kaart_raakt_de_schildpad()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.RhythmTurtle), CardCatalog.Strike.Id, CardCatalog.Strike.Id, CardCatalog.Strike.Id);

        var first = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();
        combat.Play(CardCatalog.Strike.Id, E);
        var third = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();

        Assert.Contains(new HitBounced(E, "1 % 3", 1), first);
        Assert.DoesNotContain(first, e => e is DamageDealt);
        Assert.Contains(third, e => e is DamageDealt);
        Assert.Equal(30 - 6, combat.Enemy().Hp);
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.Modulo);
    }

    [Fact]
    public void Om_de_andere_beurt_slaat_hij_hard()
    {
        var combat = Combat.Start(new CombatSetup(Scenarios.Player(200, 200), Bestiary.Create(Bestiary.RhythmTurtle), TestHelpers.TestDeck), 1);

        var values = Enumerable.Range(0, 4).Select(_ => combat.Handle(new EndTurn()).OfType<AttackLaunched>().Single().Value).ToList();

        Assert.Equal([4, 16, 4, 16], values);
    }

    // ---------- Remainder ----------

    [Theory]
    [InlineData(Bestiary.Knight)]
    [InlineData(Bestiary.Colossus)]
    public void Remainder_maakt_de_aanval_kleiner_dan_5(string enemy)
    {
        var combat = TestHelpers.StartWithHand(Against(enemy, CardCatalog.Remainder), CardCatalog.Remainder.Id);
        double before = combat.Snapshot().Combatants.Single(c => c.IsEnemy).Intent!.Value!.Value;

        var events = combat.Play(CardCatalog.Remainder.Id, E).WithoutSeq();

        var assigned = Assert.Single(events.OfType<IntentAssigned>());
        Assert.Equal(before % 5, assigned.Value);
        Assert.True(assigned.Value < 5);
        Assert.Equal(before % 5, Assert.Single(combat.Handle(new EndTurn()).OfType<AttackLaunched>()).Value);
    }

    // ---------- In het spel ----------

    [Fact]
    public void Beide_zitten_in_act_1_en_de_kaarten_in_de_pool()
    {
        Assert.Contains(Bestiary.BoolGhost, Acts.VatValley.NormalPool);
        Assert.Contains(Bestiary.RhythmTurtle, Acts.VatValley.NormalPool);
        Assert.Contains(CardCatalog.Flip, CardCatalog.RewardPool);
        Assert.Contains(CardCatalog.Remainder, CardCatalog.RewardPool);
        Assert.Equal(1, CodexCatalog.Get(CodexCatalog.Modulo).MinAct);
    }
}
