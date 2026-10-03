using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Relics;
using DeckOverflow.Core.Values;

namespace DeckOverflow.Tests;

public class CardTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CombatSetup Setup(string enemy, IEnumerable<CardDefinition> deck, params string[] relics) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), [.. deck], Relics: relics);

    private static IEnumerable<CardDefinition> Many(CardDefinition card, int count) => Enumerable.Repeat(card, count);

    // ---------- Toekenning: Zet op 1 ----------

    [Fact]
    public void Zet_op_1_overschrijft_de_aanval_voor_een_beurt()
    {
        var setup = Setup("colossus", [CardCatalog.SetTo1, .. Many(CardCatalog.Strike, 9)]);
        var combat = TestHelpers.StartWithHand(setup, "set-to-1");

        var assign = combat.Play("set-to-1", E).WithoutSeq();
        var turn = combat.Handle(new EndTurn());

        Assert.Contains(new IntentAssigned(E, ExpressionBefore: "12 + 3 * 2", Value: 1), assign);
        Assert.Equal(1, Assert.Single(turn.OfType<AttackLaunched>()).Value);
        Assert.Equal(49, combat.Player().Hp);
        // Volgende beurt staat zijn eigen aanval er weer
        Assert.Equal("12 + 3 * 2", combat.Enemy().Intent!.Expression);
    }

    // ---------- Modifiers: de volgorde telt ----------

    [Fact]
    public void Eerst_plus_3_dan_maal_2_geeft_18()
    {
        var setup = Setup("colossus", [CardCatalog.Add, CardCatalog.DoubleUp, .. Many(CardCatalog.Strike, 8)]);
        var combat = TestHelpers.StartWithHand(setup, "add", "double-up", "strike");
        combat.Play("add", P);
        combat.Play("double-up", P);

        var events = combat.Play("strike", E).WithoutSeq();

        Assert.Contains(new ModifiersApplied("strike", Before: 6, After: 18, Expression: "(6 + 3) × 2"), events);
        Assert.Equal(506 - 18, combat.Enemy().Hp);
    }

    [Fact]
    public void Eerst_maal_2_dan_plus_3_geeft_15()
    {
        var setup = Setup("colossus", [CardCatalog.Add, CardCatalog.DoubleUp, .. Many(CardCatalog.Strike, 8)]);
        var combat = TestHelpers.StartWithHand(setup, "add", "double-up", "strike");
        combat.Play("double-up", P);
        combat.Play("add", P);

        var events = combat.Play("strike", E).WithoutSeq();

        Assert.Contains(new ModifiersApplied("strike", Before: 6, After: 15, Expression: "6 × 2 + 3"), events);
        Assert.Equal(506 - 15, combat.Enemy().Hp);
    }

    [Fact]
    public void Modifier_werkt_op_elke_treffer_en_een_int_kapt_daarna_af()
    {
        var setup = Setup("colossus", [CardCatalog.Add, .. Many(CardCatalog.FloatingStrike, 9)]);
        var combat = TestHelpers.StartWithHand(setup, "add", "floating-strike");
        combat.Play("add", P);

        var events = combat.Play("floating-strike", E);

        // 2.5 + 3 = 5.5 per treffer, op een int telkens 5
        Assert.Equal([5.0, 5.0, 5.0], events.OfType<DamageDealt>().Select(d => d.Amount));
    }

    [Fact]
    public void Omgieten_verbruikt_geen_modifier_Herstel_wel()
    {
        var setup = Setup("colossus", [CardCatalog.Add, CardCatalog.RemoldByte, CardCatalog.Mend, .. Many(CardCatalog.Shield, 7)]);
        var combat = TestHelpers.StartWithHand(setup, "add", "remold-byte", "mend");
        combat.Play("add", P);
        combat.Play("remold-byte", E);

        var events = combat.Play("mend", E);

        // (byte)506 = 250, en 250 + 9 klapt om naar 3
        Assert.Contains(new ValueOverflowed(E, Before: 250, Added: 9, After: 3, Max: 255), events.WithoutSeq());
        Assert.Empty(combat.Snapshot().Modifiers);
    }

    [Fact]
    public void Wachtende_modifiers_staan_in_de_snapshot()
    {
        var setup = Setup("colossus", [CardCatalog.Add, CardCatalog.DoubleUp, .. Many(CardCatalog.Strike, 8)]);
        var combat = TestHelpers.StartWithHand(setup, "add", "double-up");
        combat.Play("add", P);
        var events = combat.Play("double-up", P).WithoutSeq();

        Assert.Equal(["+3", "×2"], combat.Snapshot().Modifiers);
        Assert.Contains(new ModifierQueued("×2", Pending: "+3 ×2"), events);
    }

    // ---------- Byteval ----------

    [Fact]
    public void Byteval_giet_om_en_laat_meteen_overlopen()
    {
        var setup = Setup("colossus", Many(CardCatalog.ByteTrap, 10));
        var combat = TestHelpers.StartWithHand(setup, "byte-trap");

        var events = combat.Play("byte-trap", E);

        Assert.Single(events.OfType<TypeChanged>());
        Assert.Single(events.OfType<ValueOverflowed>());
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    [Fact]
    public void Byteval_op_een_byte_slaat_het_omgieten_over()
    {
        var setup = Setup("golem", Many(CardCatalog.ByteTrap, 10));
        var combat = TestHelpers.StartWithHand(setup, "byte-trap");

        var events = combat.Play("byte-trap", E);

        Assert.Empty(events.OfType<TypeChanged>());
        Assert.DoesNotContain(events, e => e is PlayRejected);
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    // ---------- Relics ----------

    [Fact]
    public void Teller_maakt_elke_derde_kaart_gratis()
    {
        var setup = Setup("colossus", Many(CardCatalog.Strike, 10), RelicCatalog.Counter);
        var combat = TestHelpers.StartWithHand(setup);
        combat.Play("strike", E);
        combat.Play("strike", E);

        Assert.All(combat.Snapshot().Hand, c => Assert.Equal(0, c.Cost));
        var events = combat.Play("strike", E).WithoutSeq();

        Assert.Contains(new RelicTriggered(RelicCatalog.Counter), events);
        Assert.Equal(1, combat.Energy);
        Assert.All(combat.Snapshot().Hand, c => Assert.Equal(1, c.Cost));
    }

    [Fact]
    public void Vlottende_Komma_maakt_alleen_de_eerste_Vlottende_kaart_gratis()
    {
        var setup = Setup("colossus", Many(CardCatalog.FloatingStrike, 10), RelicCatalog.FloatingPoint);
        var combat = TestHelpers.StartWithHand(setup);

        var first = combat.Play("floating-strike", E).WithoutSeq();
        combat.Play("floating-strike", E);

        Assert.Contains(new RelicTriggered(RelicCatalog.FloatingPoint), first);
        Assert.Equal(2, combat.Energy);
    }

    [Fact]
    public void Restzak_vuurt_af_bij_3_afgekapt()
    {
        var setup = Setup("colossus", Many(CardCatalog.FloatingStrike, 10), RelicCatalog.ScrapPouch);
        var combat = TestHelpers.StartWithHand(setup);
        combat.Play("floating-strike", E);
        Assert.Equal("1.5/3", combat.Snapshot().Relics.Single().Counter);

        var events = combat.Play("floating-strike", E).WithoutSeq();

        Assert.Contains(new RelicTriggered(RelicCatalog.ScrapPouch), events);
        Assert.Equal(new DamageDealt(E, Amount: 3, HpBefore: 494, HpAfter: 491), events.OfType<DamageDealt>().Last());
        Assert.Equal("0/3", combat.Snapshot().Relics.Single().Counter);
    }

    // ---------- De Rekenmeester ----------

    [Fact]
    public void Rekenmeester_verbergt_zijn_totaal_tot_hij_aanvalt()
    {
        var combat = Combat.Start(Scenarios.Create("reckoner"), Scenarios.DefaultSeed);

        Assert.Null(combat.Enemy().Intent!.Value);
        var events = combat.Handle(new EndTurn());

        Assert.Equal(11, Assert.Single(events.OfType<AttackLaunched>()).Value);
        var next = Assert.Single(events.OfType<IntentRevealed>());
        Assert.Equal("(3 + 2) * 4", next.Expression);
        Assert.Null(next.Value);
    }

    [Fact]
    public void Rekenmeester_rekent_integer_deling_en_modulo_zoals_CSharp()
    {
        var pattern = Bestiary.Create("reckoner").Pattern;

        Assert.Equal([11, 20, 5, 14], pattern.Select(i => i.Value));
    }

    // ---------- Smeltkroes en rustvuur ----------

    [Fact]
    public void Omgieten_in_de_Smeltkroes_maakt_een_Vlottende_kaart_gratis_en_heel()
    {
        var poured = CardCatalog.Pour(CardCatalog.FloatingStrike);

        Assert.Equal("molded-strike", poured.Id);
        Assert.Equal(0, poured.Cost);
        Assert.Equal(ValueKind.Int, poured.Kind);
        Assert.Equal(new DamageEffect(2, Hits: 3), poured.Effect);
        // De tekst volgt uit het effect: "3× 2 damage."
        var text = CardText.Of(poured);
        Assert.Equal("effect.damage-hits", text.Key);
        Assert.Equal("2", text.Args!["amount"]);
        Assert.Equal("3", text.Args["hits"]);
    }

    [Fact]
    public void Een_verbeterde_Vlottende_kaart_blijft_verbeterd_na_omgieten()
    {
        var poured = CardCatalog.Pour(CardCatalog.Upgrade(CardCatalog.FloatingStrike)!);

        Assert.Equal("molded-strike+", poured.Id);
        Assert.Equal(new DamageEffect(3, Hits: 3), poured.Effect);
    }

    [Fact]
    public void Alleen_kaarten_die_een_double_afvuren_kunnen_in_de_kroes()
    {
        Assert.True(CardCatalog.CanPour(CardCatalog.FloatingRain));
        Assert.False(CardCatalog.CanPour(CardCatalog.Strike));
        Assert.False(CardCatalog.CanPour(CardCatalog.Pour(CardCatalog.FloatingStrike)));
    }

    [Fact]
    public void Verbeteren_kan_maar_een_keer()
    {
        var better = CardCatalog.Upgrade(CardCatalog.Strike);

        Assert.NotNull(better);
        Assert.Equal("strike+", better.Id);
        Assert.Equal(new DamageEffect(9), better.Effect);
        Assert.Null(CardCatalog.Upgrade(better));
    }

    [Fact]
    public void Elke_kaart_uit_de_pool_en_het_starterdeck_kan_verbeterd_worden()
    {
        var all = CardCatalog.RewardPool.Concat(CardCatalog.StarterDeck()).Distinct();

        Assert.All(all, c => Assert.NotNull(CardCatalog.Upgrade(c)));
    }
}
