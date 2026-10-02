using System.Text.Json;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Tests;

public class CombatTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    // ---------- Vlottende Geest (double) ----------

    [Fact]
    public void Geest_als_double_vangt_een_int_slag_exact_op()
    {
        var combat = TestHelpers.StartWithHand("geest", "slag");

        var events = combat.Play("slag", E).WithoutSeq();

        GameEvent[] expected =
        [
            new CardPlayed("slag", P, E),
            new BlockAbsorbed(E, Absorbed: 6, Remaining: 6.5)
        ];
        Assert.Equal(expected, events);
    }

    [Fact]
    public void Geest_als_double_neemt_decimale_schade_exact()
    {
        var combat = TestHelpers.StartWithHand("geest", "slag", "slag", "vlottende-slag");
        combat.Play("slag", E);
        combat.Play("slag", E);

        var events = combat.Play("vlottende-slag", E).WithoutSeq();

        GameEvent[] expected =
        [
            new CardPlayed("vlottende-slag", P, E),
            new BlockAbsorbed(E, Absorbed: 0.5, Remaining: 0),
            new DamageDealt(E, Amount: 2, HpBefore: 24.5, HpAfter: 22.5),
            new DamageDealt(E, Amount: 2.5, HpBefore: 22.5, HpAfter: 20),
            new DamageDealt(E, Amount: 2.5, HpBefore: 20, HpAfter: 17.5)
        ];
        Assert.Equal(expected, events);
    }

    [Fact]
    public void Geest_naar_int_verliest_zijn_restjes()
    {
        var combat = TestHelpers.StartWithHand("geest", "giet-int");

        var events = combat.Play("giet-int", E).WithoutSeq();

        GameEvent[] expected =
        [
            new CardPlayed("giet-int", P, E),
            new ValueTruncated(E, Before: 24.5, After: 24, Lost: 0.5, ValueSubject.Hp),
            new TypeChanged(E, ValueKind.Double, ValueKind.Int, HpBefore: 24.5, HpAfter: 24, MaxHpAfter: 24, BlockAfter: 12, Wrapped: false)
        ];
        Assert.Equal(expected, events);
        Assert.Equal(ValueKind.Int, combat.Enemy().Kind);
    }

    [Fact]
    public void Int_schild_kost_een_hele_punt_per_halve_treffer()
    {
        var combat = TestHelpers.StartWithHand("geest", "giet-int", "vlottende-slag");
        combat.Play("giet-int", E);

        var events = combat.Play("vlottende-slag", E);

        // 3 × 2.5 schade kost een int-schild 9 punten, een double-schild maar 7.5
        Assert.Equal([3.0, 3.0, 3.0], events.OfType<BlockAbsorbed>().Select(b => b.Absorbed));
        Assert.Equal(3, combat.Enemy().Block);
    }

    [Fact]
    public void Geest_beurt_schild_vervalt_aanval_kapt_af_schild_komt_terug()
    {
        var combat = TestHelpers.StartWithHand("geest");

        var events = combat.Handle(new EndTurn()).WithoutSeq();

        GameEvent[] expected =
        [
            new TurnEnded(1),
            new BlockExpired(E, 12.5),
            new AttackLaunched(E, P, "9 / 2.0", 4.5),
            new ValueTruncated(P, Before: 4.5, After: 4, Lost: 0.5, ValueSubject.Damage),
            new DamageDealt(P, Amount: 4, HpBefore: 50, HpAfter: 46),
            new BlockGained(E, Amount: 12.5, Total: 12.5),
            new TurnStarted(2, Energy: 3),
            new IntentRevealed(E, "9 / 2.0", 4.5)
        ];
        Assert.Equal(expected, events);
    }

    [Fact]
    public void Omgegoten_geest_bouwt_een_afgekapt_schild_op()
    {
        var combat = TestHelpers.StartWithHand("geest", "giet-int");
        combat.Play("giet-int", E);

        var events = combat.Handle(new EndTurn()).WithoutSeq();

        Assert.Contains(new ValueTruncated(E, Before: 12.5, After: 12, Lost: 0.5, ValueSubject.Block), events);
        Assert.Contains(new BlockGained(E, Amount: 12, Total: 12), events);
    }

    // ---------- Tinnen Kolos (int) ----------

    [Fact]
    public void Kolos_naar_byte_klapt_om_en_herstel_laat_hem_overlopen()
    {
        var combat = TestHelpers.StartWithHand("kolos", "giet-byte", "herstel");

        var cast = combat.Play("giet-byte", E).WithoutSeq();
        var heal = combat.Play("herstel", E).WithoutSeq();

        GameEvent[] expectedCast =
        [
            new CardPlayed("giet-byte", P, E),
            new TypeChanged(E, ValueKind.Int, ValueKind.Byte, HpBefore: 506, HpAfter: 250, MaxHpAfter: 255, BlockAfter: 0, Wrapped: true)
        ];
        GameEvent[] expectedHeal =
        [
            new CardPlayed("herstel", P, E),
            new ValueOverflowed(E, Before: 250, Added: 6, After: 0, Max: 255),
            new CombatantDied(E),
            new CombatEnded(Won: true)
        ];
        Assert.Equal(expectedCast, cast);
        Assert.Equal(expectedHeal, heal);
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    [Fact]
    public void Double_aanval_op_int_doelwit_kapt_elke_treffer_af()
    {
        var combat = TestHelpers.StartWithHand("kolos", "vlottende-slag");

        var events = combat.Play("vlottende-slag", E);

        Assert.Equal(3, events.OfType<ValueTruncated>().Count(t => t.Subject == ValueSubject.Damage));
        Assert.Equal([2.0, 2.0, 2.0], events.OfType<DamageDealt>().Select(d => d.Amount));
        Assert.Equal(500, combat.Enemy().Hp);
    }

    [Fact]
    public void Omgieten_dat_op_0_landt_doodt_meteen()
    {
        var setup = Scenarios.Kolos();
        setup = setup with { Enemy = setup.Enemy with { Stats = setup.Enemy.Stats with { Hp = 512, MaxHp = 512 } } };
        var combat = TestHelpers.StartWithHand(setup, "giet-byte");

        var events = combat.Play("giet-byte", E);

        Assert.Equal(0, Assert.Single(events.OfType<TypeChanged>()).HpAfter);
        Assert.Contains(events, e => e is CombatantDied { TargetId: E });
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    [Fact]
    public void Omgieten_naar_hetzelfde_type_wordt_geweigerd_zonder_energie()
    {
        var combat = TestHelpers.StartWithHand("kolos", "giet-int");

        var events = combat.Play("giet-int", E);

        Assert.IsType<PlayRejected>(Assert.Single(events));
        Assert.Equal(3, combat.Energy);
        Assert.Contains(combat.Snapshot().Hand, c => c.Id == "giet-int");
    }

    [Fact]
    public void Kolos_valt_aan_met_operatorvoorrang()
    {
        var combat = TestHelpers.StartWithHand("kolos");

        var events = combat.Handle(new EndTurn());

        var attack = Assert.Single(events.OfType<AttackLaunched>());
        Assert.Equal(18, attack.Value);
        Assert.Equal(32, combat.Player().Hp);
    }

    // ---------- Algemeen ----------

    [Fact]
    public void Omgieten_kan_alleen_op_een_vijand()
    {
        var combat = TestHelpers.StartWithHand("geest", "giet-byte");

        var events = combat.Play("giet-byte", P);

        Assert.IsType<PlayRejected>(Assert.Single(events));
        Assert.Equal(ValueKind.Int, combat.Player().Kind);
    }

    [Fact]
    public void Herstel_op_speler_gaat_niet_boven_max()
    {
        var combat = TestHelpers.StartWithHand("geest", "herstel");

        var events = combat.Play("herstel", P);

        var healed = Assert.Single(events.OfType<Healed>());
        Assert.Equal(0, healed.Amount);
        Assert.Equal(50, healed.HpAfter);
    }

    [Fact]
    public void Kaarten_dragen_hun_type()
    {
        var combat = TestHelpers.StartWithHand("geest", "slag", "vlottende-slag", "schild");
        var hand = combat.Snapshot().Hand;

        Assert.Equal(ValueKind.Int, hand.First(c => c.Id == "slag").Kind);
        Assert.Equal(ValueKind.Double, hand.First(c => c.Id == "vlottende-slag").Kind);
        Assert.Null(hand.First(c => c.Id == "schild").Kind);
    }

    [Theory]
    [InlineData("geest")]
    [InlineData("kolos")]
    public void Zelfde_seed_en_commands_geven_zelfde_events(string scenario)
    {
        List<GameEvent> Run()
        {
            var combat = Combat.Start(Scenarios.Create(scenario), Scenarios.DefaultSeed);
            var all = new List<GameEvent>();
            all.AddRange(combat.Handle(new PlayCard(0, E)));
            all.AddRange(combat.Handle(new EndTurn()));
            all.AddRange(combat.Handle(new PlayCard(0, E)));
            all.AddRange(combat.Handle(new EndTurn()));
            return all;
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void Seq_loopt_door_over_commands()
    {
        var combat = TestHelpers.StartWithHand("geest", "slag");

        var first = combat.Play("slag", E);
        var second = combat.Handle(new EndTurn());

        int[] seqs = [.. first.Concat(second).Select(e => e.Seq)];
        Assert.Equal(Enumerable.Range(1, seqs.Length), seqs);
    }

    [Fact]
    public void Na_het_gevecht_wordt_alles_geweigerd()
    {
        var combat = TestHelpers.StartWithHand("kolos", "giet-byte", "herstel");
        combat.Play("giet-byte", E);
        combat.Play("herstel", E);

        var events = combat.Handle(new EndTurn());

        Assert.IsType<PlayRejected>(Assert.Single(events));
    }

    [Fact]
    public void ValueTruncated_zegt_wat_er_afgekapt_wordt()
    {
        var combat = TestHelpers.StartWithHand("geest", "giet-int");
        var events = combat.Play("giet-int", E);

        string json = JsonSerializer.Serialize(events, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("""{"type":"ValueTruncated","targetId":1,"before":24.5,"after":24,"lost":0.5,"subject":"Hp","seq":2}""", json);
    }

    [Fact]
    public void TypeChanged_serialiseert_naar_het_contract()
    {
        var combat = TestHelpers.StartWithHand("kolos", "giet-byte");
        var events = combat.Play("giet-byte", E);

        // Dezelfde opties als Blazor's IJSRuntime
        string json = JsonSerializer.Serialize(events, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("""{"type":"TypeChanged","targetId":1,"from":"Int","to":"Byte","hpBefore":506,"hpAfter":250,"maxHpAfter":255,"blockAfter":0,"wrapped":true,"seq":2}""", json);
    }
}
