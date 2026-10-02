using System.Text.Json;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;

namespace DeckOverflow.Tests;

public class CombatTests
{
    [Fact]
    public void Standaardseed_heeft_Herstel_in_eerste_hand()
    {
        var combat = TestHelpers.StartDefault();

        Assert.Contains(combat.Snapshot().Hand, c => c.Id == "herstel");
    }

    [Fact]
    public void Zelfde_seed_en_commands_geven_zelfde_events()
    {
        static List<GameEvent> Run()
        {
            var combat = TestHelpers.StartDefault();
            var all = new List<GameEvent>();
            all.AddRange(combat.Play("slag", Combat.EnemyId));
            all.AddRange(combat.Handle(new EndTurn()));
            all.AddRange(combat.Handle(new PlayCard(0, Combat.EnemyId)));
            all.AddRange(combat.Handle(new EndTurn()));
            return all;
        }

        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void PadB_herstel_op_golem_laat_byte_overlopen_en_wint()
    {
        var combat = TestHelpers.StartDefault();

        var events = combat.Play("herstel", Combat.EnemyId).WithoutSeq();

        GameEvent[] expected =
        [
            new CardPlayed("herstel", Combat.PlayerId, Combat.EnemyId),
            new ValueOverflowed(Combat.EnemyId, Before: 250, Added: 6, After: 0, Max: 255),
            new CombatantDied(Combat.EnemyId),
            new CombatEnded(Won: true)
        ];
        Assert.Equal(expected, events);
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    [Fact]
    public void PadA_slag_schild_einde_beurt_kapt_halve_schade_af()
    {
        var combat = TestHelpers.StartDefault();

        combat.Play("slag", Combat.EnemyId);
        combat.Play("schild", Combat.PlayerId);
        var events = combat.Handle(new EndTurn()).WithoutSeq();

        GameEvent[] expected =
        [
            new TurnEnded(1),
            new AttackLaunched(Combat.EnemyId, Combat.PlayerId, "15 / 2.0", 7.5),
            new BlockAbsorbed(Combat.PlayerId, Absorbed: 5, Remaining: 0),
            new ValueTruncated(Combat.PlayerId, Before: 2.5, After: 2, Lost: 0.5),
            new DamageDealt(Combat.PlayerId, Amount: 2, HpBefore: 50, HpAfter: 48),
            new Healed(Combat.EnemyId, Amount: 2, HpAfter: 246),
            new TurnStarted(2, Energy: 3),
            new IntentRevealed(Combat.EnemyId, "15 / 2.0", 7.5)
        ];
        Assert.Equal(expected, events);

        var snapshot = combat.Snapshot();
        Assert.Equal(5, snapshot.Hand.Count);
        Assert.Equal(3, snapshot.Energy);
    }

    [Fact]
    public void Zonder_blok_kapt_7_5_af_naar_7()
    {
        var combat = TestHelpers.StartDefault();

        var events = combat.Handle(new EndTurn());

        var truncated = Assert.Single(events.OfType<ValueTruncated>());
        Assert.Equal(7, truncated.After);
        var damage = Assert.Single(events.OfType<DamageDealt>());
        Assert.Equal(43, damage.HpAfter);
    }

    [Fact]
    public void Variant_golem_op_252_landt_op_2_en_sneuvelt_door_een_slag()
    {
        var setup = ByteGolemScenario.Create();
        setup = setup with { Enemy = setup.Enemy with { Stats = setup.Enemy.Stats with { Hp = 252 } } };
        var combat = Combat.Start(setup, ByteGolemScenario.DefaultSeed);

        var heal = combat.Play("herstel", Combat.EnemyId);
        var hit = combat.Play("slag", Combat.EnemyId);

        var overflow = Assert.Single(heal.OfType<ValueOverflowed>());
        Assert.Equal(2, overflow.After);
        Assert.Contains(hit, e => e is CombatantDied { TargetId: Combat.EnemyId });
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    [Fact]
    public void Herstel_op_speler_gaat_niet_boven_max()
    {
        var combat = TestHelpers.StartDefault();

        var events = combat.Play("herstel", Combat.PlayerId);

        var healed = Assert.Single(events.OfType<Healed>());
        Assert.Equal(0, healed.Amount);
        Assert.Equal(50, healed.HpAfter);
    }

    [Fact]
    public void Slag_op_jezelf_wordt_geweigerd()
    {
        var combat = TestHelpers.StartDefault();

        var events = combat.Play("slag", Combat.PlayerId);

        Assert.IsType<PlayRejected>(Assert.Single(events));
        Assert.Equal(3, combat.Energy);
    }

    [Fact]
    public void Vierde_kaart_zonder_energie_wordt_geweigerd()
    {
        var combat = TestHelpers.StartDefault();
        combat.Play("slag", Combat.EnemyId);
        combat.Play("slag", Combat.EnemyId);
        combat.Play("schild", Combat.PlayerId);

        var events = combat.Handle(new PlayCard(0, Combat.EnemyId));

        Assert.IsType<PlayRejected>(Assert.Single(events));
    }

    [Fact]
    public void Na_het_gevecht_wordt_alles_geweigerd()
    {
        var combat = TestHelpers.StartDefault();
        combat.Play("herstel", Combat.EnemyId);

        var events = combat.Handle(new EndTurn());

        Assert.IsType<PlayRejected>(Assert.Single(events));
    }

    [Fact]
    public void Seq_loopt_door_over_commands()
    {
        var combat = TestHelpers.StartDefault();

        var first = combat.Play("slag", Combat.EnemyId);
        var second = combat.Handle(new EndTurn());

        int[] seqs = [.. first.Concat(second).Select(e => e.Seq)];
        Assert.Equal(Enumerable.Range(1, seqs.Length), seqs);
    }

    [Fact]
    public void Events_serialiseren_naar_het_contract()
    {
        var combat = TestHelpers.StartDefault();
        var events = combat.Play("herstel", Combat.EnemyId);

        // Dezelfde opties als Blazor's IJSRuntime
        string json = JsonSerializer.Serialize(events, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Contains("""{"type":"ValueOverflowed","targetId":1,"before":250,"added":6,"after":0,"max":255,"seq":2}""", json);
    }
}
