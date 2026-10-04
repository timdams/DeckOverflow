using DeckOverflow.CardHall.Achievements;
using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Relics;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Core.Codex;

namespace DeckOverflow.Tests;

/// <summary>
/// De rest van hoofdstuk 2 in act 1: <c>++</c> (de Tighten-kaarten en de Twin Shooters), namen (The Nameless),
/// <c>const</c> (de patch na te vaak Wrong Label) en operatorvoorrang (Move the Brackets).
/// </summary>
public class IncrementAndNamesTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CombatSetup Against(string enemy, params CardDefinition[] extra) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), [.. TestHelpers.TestDeck, .. extra]);

    private static CombatSetup OnlyWith(EnemySetup enemy, params CardDefinition[] deck) =>
        new(Scenarios.Player(hp: 200, maxHp: 200), enemy, deck);

    private static Run OnPath(RunSetup setup, params (NodeKind, string?)[] steps) =>
        Run.Start(255, setup with { Map = ActMap.Path(steps), Opening = false });

    // ---------- De Tighten-kaarten: count++ en ++count ----------

    [Fact]
    public void Hit_Then_Tighten_slaat_met_de_oude_waarde_en_telt_dan_op()
    {
        var combat = Combat.Start(OnlyWith(Bestiary.Create(Bestiary.Slime), CardCatalog.HitThenTighten), 1);

        var events = combat.Play(CardCatalog.HitThenTighten.Id, E).WithoutSeq();

        Assert.Equal(IncrementEffect.Start, Assert.Single(events.OfType<DamageDealt>()).Amount);
        var counted = Assert.Single(events.OfType<VariableIncremented>());
        Assert.Equal(("count++", 3, 4), (counted.Expression, counted.Before, counted.After));
        // count++ telt pas op na de treffer
        Assert.True(events.IndexOf(counted) > events.FindIndex(e => e is DamageDealt));
    }

    [Fact]
    public void Tighten_Then_Hit_telt_eerst_op_en_slaat_dan_met_de_nieuwe_waarde()
    {
        var combat = Combat.Start(OnlyWith(Bestiary.Create(Bestiary.Slime), CardCatalog.TightenThenHit), 1);

        var events = combat.Play(CardCatalog.TightenThenHit.Id, E).WithoutSeq();

        // ++count * 2 met count 3: eerst 4, dan 8
        Assert.Equal(8, Assert.Single(events.OfType<DamageDealt>()).Amount);
        Assert.True(events.FindIndex(e => e is VariableIncremented) < events.FindIndex(e => e is DamageDealt));
    }

    [Fact]
    public void Eerst_goedkoop_optellen_maakt_de_afmaker_zwaarder_en_de_kaart_toont_dat()
    {
        var setup = OnlyWith(Bestiary.Create(Bestiary.Golem), CardCatalog.HitThenTighten, CardCatalog.HitThenTighten, CardCatalog.TightenThenHit);
        var combat = Combat.Start(setup, 1);
        combat.Play(CardCatalog.HitThenTighten.Id, E);
        combat.Play(CardCatalog.HitThenTighten.Id, E);

        var finisher = combat.Snapshot().Hand.Single(c => c.Id == CardCatalog.TightenThenHit.Id);
        Assert.Equal("12", finisher.Text.Args!["amount"]);   // ++5 * 2

        var events = combat.Play(CardCatalog.TightenThenHit.Id, E);
        Assert.Contains(events, e => e is VariableIncremented { Value: 12, After: 6 });
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.Increment);
    }

    [Fact]
    public void Een_Tighten_kaart_neemt_modifiers_mee_zoals_elke_aanval()
    {
        var setup = OnlyWith(Bestiary.Create(Bestiary.Golem), CardCatalog.DoubleUp, CardCatalog.HitThenTighten);
        var combat = Combat.Start(setup, 1);
        combat.Play(CardCatalog.DoubleUp.Id, P);

        var events = combat.Play(CardCatalog.HitThenTighten.Id, E);

        Assert.Equal(6, Assert.Single(events.OfType<ModifiersApplied>()).After);   // 3 × 2
    }

    // ---------- De Twin Shooters: shots++ + ++shots ----------

    [Fact]
    public void De_Twin_Shooters_slaan_de_oude_en_de_nieuwe_waarde_samen()
    {
        var combat = Combat.Start(Against(Bestiary.TwinShooters), 1);
        Assert.Equal("shots = 1", combat.Enemy().Rule);

        var first = combat.Handle(new EndTurn());
        var second = combat.Handle(new EndTurn());

        var attack = Assert.Single(first.OfType<AttackLaunched>());
        Assert.Equal(("1 + 3", 4.0), (attack.Expression, attack.Value));
        Assert.Equal(8, Assert.Single(second.OfType<AttackLaunched>()).Value);   // 3 + 5
        Assert.Equal("shots = 5", combat.Enemy().Rule);
        Assert.Equal("3", combat.Moments.Single(m => m.Key == CodexCatalog.Increment).Values["after"]);
    }

    [Fact]
    public void Wrong_Label_op_de_Twin_Shooters_houdt_hun_teller_stil()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.TwinShooters, CardCatalog.SetTo1), CardCatalog.SetTo1.Id);
        combat.Play(CardCatalog.SetTo1.Id, E);

        var events = combat.Handle(new EndTurn());

        Assert.Equal(1, Assert.Single(events.OfType<AttackLaunched>()).Value);
        Assert.DoesNotContain(events, e => e is VariableIncremented);
        Assert.Equal("shots = 1", combat.Enemy().Rule);
    }

    // ---------- The Nameless: namen ----------

    [Fact]
    public void The_Nameless_is_te_raken_als_hij_shadow_heet()
    {
        var combat = TestHelpers.StartWithHand(Bestiary.Nameless, CardCatalog.Strike.Id);
        Assert.Equal("name: shadow", combat.Enemy().Rule);

        combat.Play(CardCatalog.Strike.Id, E);

        Assert.Equal(44 - 6, combat.Enemy().Hp);
    }

    [Theory]
    [InlineData(2, "reject.name-unknown")]     // Shadow: een andere naam
    [InlineData(4, "reject.name-digit")]       // 2shadow
    [InlineData(6, "reject.name-character")]   // sha-dow
    public void Een_verkeerde_naam_compileert_niet_en_de_kaart_weigert(int turn, string reason)
    {
        var setup = OnlyWith(Bestiary.Create(Bestiary.Nameless), CardCatalog.Strike, CardCatalog.Strike, CardCatalog.Strike, CardCatalog.Strike, CardCatalog.Strike, CardCatalog.Shield);
        var combat = Combat.Start(setup, 1);
        for (int t = 1; t < turn; t++) combat.Handle(new EndTurn());

        var events = combat.Handle(new PlayCard(combat.HandIndexOf(CardCatalog.Strike.Id), E));

        Assert.Equal(reason, Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
        Assert.Equal("shadow", combat.Moments.Single(m => m.Key == CodexCatalog.Identifiers).Values["real"]);
    }

    [Fact]
    public void Op_een_verkeerde_naam_kan_je_nog_blokken_en_klaarzetten()
    {
        var setup = OnlyWith(Bestiary.Create(Bestiary.Nameless), CardCatalog.Shield, CardCatalog.Add, CardCatalog.Shield, CardCatalog.Add, CardCatalog.Shield);
        var combat = Combat.Start(setup, 1);
        combat.Handle(new EndTurn());   // beurt 2: Shadow

        Assert.Contains(combat.Play(CardCatalog.Shield.Id, P), e => e is BlockGained);
        Assert.Contains(combat.Play(CardCatalog.Add.Id, P), e => e is ModifierQueued);
    }

    // ---------- const: de patch ----------

    [Fact]
    public void Op_een_const_aanval_compileert_toekennen_niet()
    {
        var foe = Bestiary.Create(Bestiary.Slime) with { ConstAttack = true };
        var combat = Combat.Start(OnlyWith(foe, CardCatalog.SetTo1, CardCatalog.Remainder, CardCatalog.SetTo1, CardCatalog.Remainder, CardCatalog.SetTo1), 1);

        Assert.Equal("reject.const", Assert.IsType<PlayRejected>(Assert.Single(combat.Play(CardCatalog.SetTo1.Id, E))).Reason);
        Assert.Equal("reject.const", Assert.IsType<PlayRejected>(Assert.Single(combat.Play(CardCatalog.Remainder.Id, E))).Reason);
        Assert.Equal("const attack", combat.Enemy().Rule);
        Assert.Equal("attack = 1", combat.Moments.Single(m => m.Key == CodexCatalog.Constants).Values["expression"]);
    }

    [Fact]
    public void Na_drie_keer_Wrong_Label_op_dezelfde_vijand_wordt_zijn_aanval_const()
    {
        var steps = Enumerable.Repeat<(NodeKind, string?)>((NodeKind.Fight, Bestiary.Slime), 4).Append((NodeKind.Rest, null)).ToArray();
        var run = OnPath(new RunSetup(Hp: 50, Deck: Enumerable.Repeat(CardCatalog.SetTo1, 5).ToList()), steps);
        var all = new List<GameEvent>();

        for (int fight = 0; fight < 3; fight++)
        {
            all.AddRange(run.Handle(new ChooseNode(fight)));
            all.AddRange(run.Handle(new PlayCard(0, E)));
            all.AddRange(run.Handle(new DebugWin()));
            run.Handle(new SkipReward());
        }
        run.Handle(new ChooseNode(3));

        Assert.Equal(Bestiary.Slime, Assert.Single(all.OfType<EnemyPatched>()).EnemyKey);
        var rejected = run.Handle(new PlayCard(0, E));
        Assert.Equal("reject.const", Assert.IsType<PlayRejected>(Assert.Single(rejected)).Reason);
    }

    // ---------- Move the Brackets ----------

    [Fact]
    public void Haakjes_verschuiven_maakt_van_20_een_11_bij_de_Reckoner()
    {
        var setup = OnlyWith(Bestiary.Create(Bestiary.Reckoner), [.. Enumerable.Repeat(CardCatalog.Brackets, 10)]);
        var combat = Combat.Start(setup, 1);
        Assert.Equal("reject.no-brackets", Assert.IsType<PlayRejected>(Assert.Single(combat.Play(CardCatalog.Brackets.Id, E))).Reason);   // 3 + 2 * 4
        combat.Handle(new EndTurn());

        var regroup = combat.Play(CardCatalog.Brackets.Id, E);
        var attack = combat.Handle(new EndTurn());

        Assert.Equal("3 + 2 * 4", Assert.Single(regroup.OfType<IntentRegrouped>()).Expression);
        Assert.Equal(11, Assert.Single(attack.OfType<AttackLaunched>()).Value);
        Assert.Equal("11", combat.Moments.Single(m => m.Key == CodexCatalog.OperatorPrecedence).Values["value"]);
    }

    [Fact]
    public void Zonder_haakjes_en_zonder_blok_deelt_de_Splitter_door_nul_en_crasht_hij()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Splitter, CardCatalog.Brackets), CardCatalog.Brackets.Id);
        combat.Play(CardCatalog.Brackets.Id, E);
        Assert.Null(combat.Enemy().Intent!.Value);

        var events = combat.Handle(new EndTurn());

        var crash = Assert.Single(events.OfType<AttackCrashed>());
        Assert.Equal(("DivideByZeroException", "30 / 0 + 1"), (crash.Exception, crash.Expression));
        Assert.DoesNotContain(events, e => e is AttackLaunched);
        Assert.Equal(50, combat.Player().Hp);
        Assert.Contains(XRegister.DivideByZero, XRegister.Earned(events, Bestiary.Splitter));
    }

    [Fact]
    public void Met_blok_deelt_de_Splitter_zonder_haakjes_wel_gewoon()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Splitter, CardCatalog.Brackets), CardCatalog.Brackets.Id, CardCatalog.Shield.Id);
        combat.Play(CardCatalog.Shield.Id, P);
        combat.Play(CardCatalog.Brackets.Id, E);

        var attack = Assert.Single(combat.Handle(new EndTurn()).OfType<AttackLaunched>());

        Assert.Equal(("30 / 5 + 1", 7.0), (attack.Expression, attack.Value));   // en niet 30 / 6 = 5
    }

    [Fact]
    public void Remainder_op_een_aanval_die_crasht_weigert()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Splitter, CardCatalog.Brackets, CardCatalog.Remainder), CardCatalog.Brackets.Id, CardCatalog.Remainder.Id);
        combat.Play(CardCatalog.Brackets.Id, E);

        var events = combat.Play(CardCatalog.Remainder.Id, E);

        Assert.Equal("reject.attack-crashes", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
    }

    // ---------- De Tally Counter opent ++ ----------

    [Fact]
    public void De_Tally_Counter_opent_de_pagina_over_plusplus()
    {
        var run = OnPath(new RunSetup(Relics: [RelicCatalog.TallyCounter]), (NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));

        var events = run.Handle(new DebugWin());

        var page = Assert.Single(events.OfType<CodexUnlocked>(), u => u.Key == CodexCatalog.Increment);
        Assert.Equal("++count", page.Values["expression"]);
    }

    [Fact]
    public void De_nieuwe_vijanden_zitten_in_act_1()
    {
        Assert.Contains(Bestiary.TwinShooters, Acts.VatValley.NormalPool);
        Assert.Contains(Bestiary.Nameless, Acts.VatValley.ElitePool);
        Assert.Contains(CardCatalog.Brackets, Acts.VatValley.NewCards);
    }
}
