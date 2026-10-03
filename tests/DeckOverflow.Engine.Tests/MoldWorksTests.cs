using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Tests;

/// <summary>De vijanden en kaarten van act 2: afronden, groei die afkapt, Convert die crasht, een baas die zichzelf omgiet.</summary>
public class MoldWorksTests
{
    private const int E = Combat.EnemyId;

    private static CombatSetup Against(string enemy, params CardDefinition[] extra) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), [.. TestHelpers.TestDeck, .. extra]);

    // ---------- The Rounder ----------

    [Fact]
    public void De_Rounder_rondt_elke_treffer_af_met_bankiersafronding()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Rounder), CardCatalog.FloatingStrike.Id);

        var events = combat.Play(CardCatalog.FloatingStrike.Id, E).WithoutSeq();

        // 3× 2.5: elke treffer wordt 2, want 2.5 rondt af naar het even getal
        Assert.Equal(3, events.OfType<ValueRounded>().Count(r => r is { Before: 2.5, After: 2 }));
        Assert.Equal(34, combat.Enemy().Hp);
    }

    [Fact]
    public void Naar_int_omgegoten_kapt_de_Rounder_af_in_plaats_van_af_te_ronden()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Rounder), CardCatalog.RemoldInt.Id, CardCatalog.FloatingStrike.Id);
        combat.Play(CardCatalog.RemoldInt.Id, E);

        var events = combat.Play(CardCatalog.FloatingStrike.Id, E).WithoutSeq();

        Assert.DoesNotContain(events, e => e is ValueRounded);
        Assert.Equal(3, events.OfType<ValueTruncated>().Count());
    }

    // ---------- The Index ----------

    [Fact]
    public void The_Index_groeit_elke_beurt_maar_kapt_de_groei_af()
    {
        var combat = Combat.Start(Against(Bestiary.Index), 1);

        var events = combat.Handle(new EndTurn()).WithoutSeq();

        // (int)(90 * 1.05) = (int)94.5 = 94
        Assert.Contains(events, e => e is ValueGrew { Before: 90, After: 94 });
        Assert.Contains(events, e => e is ValueTruncated { Subject: ValueSubject.Hp });
        Assert.Equal(94, combat.Enemy().Hp);
    }

    [Fact]
    public void Onder_20_HP_eet_het_afkappen_de_groei_van_The_Index_op()
    {
        var weak = Bestiary.Create(Bestiary.Index) with { Stats = Bestiary.Create(Bestiary.Index).Stats with { Hp = 19 } };
        var combat = Combat.Start(new CombatSetup(Scenarios.Player(), weak, TestHelpers.TestDeck), 1);

        combat.Handle(new EndTurn());

        // (int)(19 * 1.05) = (int)19.95 = 19
        Assert.Equal(19, combat.Enemy().Hp);
    }

    // ---------- Measure Twice: Convert.ToByte ----------

    [Fact]
    public void Measure_Twice_rondt_af_naar_byte_waar_een_cast_zou_afkappen()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Ghost, CardCatalog.MeasureTwice), CardCatalog.MeasureTwice.Id);

        var events = combat.Play(CardCatalog.MeasureTwice.Id, E).WithoutSeq();

        // Convert.ToByte(24.5) is 24: bankiersafronding naar het even getal
        Assert.Contains(events, e => e is ValueRounded { Before: 24.5, After: 24, Subject: ValueSubject.Hp });
        Assert.Equal(ValueKind.Byte, combat.Enemy().Kind);
    }

    [Fact]
    public void Measure_Twice_op_een_reus_crasht_en_kost_hem_zijn_aanval()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Colossus, CardCatalog.MeasureTwice), CardCatalog.MeasureTwice.Id);
        double hpBefore = combat.Player().Hp;

        var played = combat.Play(CardCatalog.MeasureTwice.Id, E).WithoutSeq();
        var turn = combat.Handle(new EndTurn()).WithoutSeq();

        Assert.Contains(new ConversionCrashed(E, ValueKind.Byte, 506), played);
        Assert.Equal(ValueKind.Int, combat.Enemy().Kind);
        Assert.Contains(new AttackSkipped(E), turn);
        Assert.DoesNotContain(turn, e => e is AttackLaunched);
        Assert.Equal(hpBefore, combat.Player().Hp);
    }

    [Fact]
    public void Na_een_crash_valt_de_vijand_de_beurt_erna_weer_aan()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Colossus, CardCatalog.MeasureTwice), CardCatalog.MeasureTwice.Id);
        combat.Play(CardCatalog.MeasureTwice.Id, E);
        combat.Handle(new EndTurn());

        var next = combat.Handle(new EndTurn());

        Assert.Contains(next, e => e is AttackLaunched);
    }

    [Fact]
    public void Measure_Twice_op_een_byte_wordt_geweigerd()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Golem, CardCatalog.MeasureTwice), CardCatalog.MeasureTwice.Id);

        var events = combat.Play(CardCatalog.MeasureTwice.Id, E);

        Assert.IsType<PlayRejected>(Assert.Single(events));
    }

    // ---------- The Caster ----------

    [Fact]
    public void The_Caster_giet_zichzelf_elke_beurt_om_en_klapt_om_als_byte()
    {
        var combat = Combat.Start(Against(Bestiary.Caster), 1);

        combat.Handle(new EndTurn());
        Assert.Equal(ValueKind.Double, combat.Enemy().Kind);

        var events = combat.Handle(new EndTurn()).WithoutSeq();

        // (byte)300 is 44: boven 255 past hij niet in zijn nieuwe mal
        var cast = Assert.Single(events.OfType<TypeChanged>());
        Assert.Equal(ValueKind.Byte, cast.To);
        Assert.True(cast.Wrapped);
        Assert.Equal(44, combat.Enemy().Hp);

        combat.Handle(new EndTurn());
        Assert.Equal(ValueKind.Int, combat.Enemy().Kind);
    }
}
