using DeckOverflow.CardHall.Cards;
using DeckOverflow.Core.Codex;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.Tests;

/// <summary>
/// De Codex: een pagina gaat pas open als een regel in een gevecht iets deed, met de getallen van dat moment.
/// Eerst ervaren, dan benoemen: "Casting" opent pas in act 3.
/// </summary>
public class CodexTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CodexMoment MomentOf(Combat combat, string key) => combat.Moments.Single(m => m.Key == key);

    [Fact]
    public void Een_overflow_wordt_een_moment_met_de_getallen_erbij()
    {
        var combat = TestHelpers.StartWithHand(Bestiary.Golem, CardCatalog.Mend.Id);

        combat.Play(CardCatalog.Mend.Id, E);

        var moment = MomentOf(combat, CodexCatalog.Overflow);
        Assert.Equal("250", moment.Values["before"]);
        Assert.Equal("6", moment.Values["added"]);
        Assert.Equal("0", moment.Values["after"]);
    }

    [Fact]
    public void Afkappen_wordt_een_moment()
    {
        var combat = TestHelpers.StartWithHand(Bestiary.Knight, CardCatalog.FloatingStrike.Id);

        combat.Play(CardCatalog.FloatingStrike.Id, E);

        Assert.Equal("2.5", MomentOf(combat, CodexCatalog.IntTruncation).Values["before"]);
    }

    [Fact]
    public void Per_regel_telt_alleen_het_eerste_moment()
    {
        var combat = TestHelpers.StartWithHand(Bestiary.Knight, CardCatalog.FloatingStrike.Id, CardCatalog.FloatingStrike.Id);
        combat.Play(CardCatalog.FloatingStrike.Id, E);
        combat.Play(CardCatalog.FloatingStrike.Id, E);

        Assert.Single(combat.Moments, m => m.Key == CodexCatalog.IntTruncation);
    }

    [Fact]
    public void Een_bewuste_intent_opent_variabelen_en_deling_van_gehele_getallen()
    {
        var combat = Combat.Start(TestHelpers.Scenario(Bestiary.Splitter), 1);

        combat.Handle(new EndTurn());

        Assert.Equal("30 / (block + 1)", MomentOf(combat, CodexCatalog.Variables).Values["expression"]);
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.IntegerDivision);
    }

    [Fact]
    public void De_Rekenmeester_verslaan_opent_operatorvoorrang_maar_omgieten_in_act_1_nog_geen_casting()
    {
        var run = Run.Start(255, new RunSetup(Hp: 300, Map: ActMap.Path((NodeKind.Boss, Bestiary.Reckoner)), Opening: false));
        run.Handle(new ChooseNode(0));

        var events = run.WinCombat();

        var unlocked = events.OfType<CodexUnlocked>().Select(u => u.Key).ToList();
        Assert.Contains(CodexCatalog.OperatorPrecedence, unlocked);
        Assert.DoesNotContain(CodexCatalog.Casting, unlocked);
    }

    [Fact]
    public void In_de_Gieterij_opent_omgieten_de_pagina_casting()
    {
        var run = Run.Start(255, new RunSetup(Hp: 400, Map: ActMap.Path((NodeKind.Boss, Bestiary.Caster)), StartAct: Acts.MoldWorks.Number));
        for (int i = 0; i < Run.DraftRounds; i++) run.Handle(new SkipReward());
        run.Handle(new ChooseRelic(0));
        run.Handle(new ChooseNode(0));

        var events = run.WinCombat();

        // The Caster giet zichzelf om: in act 3 mag die regel zijn naam krijgen
        var casting = events.OfType<CodexUnlocked>().Single(u => u.Key == CodexCatalog.Casting);
        Assert.Equal("caster", casting.Values["target"]);
    }

    [Fact]
    public void Een_pagina_gaat_maar_een_keer_per_run_open()
    {
        var run = Run.Start(255, new RunSetup(Deck: TestHelpers.TestDeck, Opening: false,
            Map: ActMap.Path((NodeKind.Fight, Bestiary.Knight), (NodeKind.Fight, Bestiary.Knight), (NodeKind.Rest, null))));
        var all = new List<GameEvent>();

        run.Handle(new ChooseNode(0));
        all.AddRange(run.WinCombat());
        run.Handle(new SkipReward());
        run.Handle(new ChooseNode(1));
        all.AddRange(run.WinCombat());

        var keys = all.OfType<CodexUnlocked>().Select(u => u.Key).ToList();
        Assert.Equal(keys.Distinct().Count(), keys.Count);
    }

    [Fact]
    public void Elke_pagina_staat_in_de_catalogus_in_de_volgorde_van_het_boek()
    {
        var chapters = CodexCatalog.All.Select(e => e.Chapter ?? int.MaxValue).ToList();

        Assert.Equal(chapters.Order(), chapters);
        Assert.Equal(CodexCatalog.All.Count, CodexCatalog.All.Select(e => e.Key).Distinct().Count());
    }
}
