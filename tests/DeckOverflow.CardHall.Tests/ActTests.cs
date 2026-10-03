using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.Core.Random;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.Tests;

/// <summary>Een run over meerdere acts: de overgang na de baas, en een start in een latere act.</summary>
public class ActTests
{
    private static Run BeatFirstBoss(int hp = 300)
    {
        var run = Run.Start(255, new RunSetup(Hp: hp, Map: ActMap.Path((NodeKind.Boss, Bestiary.Reckoner)), Opening: false));
        run.Handle(new ChooseNode(0));
        run.WinCombat();
        return run;
    }

    /// <summary>Een start in de Gieterij (act 3) met precies dit deck: de draft overgeslagen, de eerste relic gekozen.</summary>
    private static Run StartInMoldWorks(RunSetup? setup = null)
    {
        var run = Run.Start(255, (setup ?? new RunSetup()) with { StartAct = Acts.MoldWorks.Number });
        for (int i = 0; i < Run.DraftRounds; i++) run.Handle(new SkipReward());
        run.Handle(new ChooseRelic(0));
        return run;
    }

    // ---------- Na de baas ----------

    [Fact]
    public void Na_de_baas_heel_je_volledig_en_kies_je_een_relic_uit_drie()
    {
        var run = BeatFirstBoss(hp: 300);
        var s = run.Snapshot();

        Assert.Equal(RunPhase.RelicChoice, s.Phase);
        Assert.Equal("boss", s.RelicChoice!.Reason);
        Assert.Equal(Run.RelicChoices, s.RelicChoice.Relics.Count);
        Assert.Equal(s.RelicChoice.Relics.Count, s.RelicChoice.Relics.Select(r => r.Id).Distinct().Count());
        Assert.Equal(s.MaxHp, s.Hp);
    }

    [Fact]
    public void Een_relic_kiezen_start_act_2_op_een_nieuwe_map()
    {
        var run = BeatFirstBoss();
        string relic = run.Snapshot().RelicChoice!.Relics[1].Id;

        var events = run.Handle(new ChooseRelic(1));
        var s = run.Snapshot();

        Assert.Contains(new RelicGained(relic), events.WithoutSeq());
        Assert.Contains(new ActStarted(2), events.WithoutSeq());
        Assert.Equal(RunPhase.Map, s.Phase);
        Assert.Equal(2, s.Act);
        Assert.Equal(Acts.PrintShop.Key, s.ActKey);
        Assert.Equal(Bestiary.Typesetter, run.Map.Nodes.Single(n => n.Kind == NodeKind.Boss).Encounter);
        Assert.All(s.Map.Nodes.Where(n => n.Reachable), n => Assert.Equal(0, n.Row));
        Assert.DoesNotContain(s.Map.Nodes, n => n.Visited);
    }

    [Fact]
    public void Een_relic_die_niet_aangeboden_wordt_kan_je_niet_nemen()
    {
        var run = BeatFirstBoss();

        var events = run.Handle(new ChooseRelic(7));

        Assert.IsType<RunRejected>(Assert.Single(events));
        Assert.Equal(RunPhase.RelicChoice, run.Phase);
    }

    [Fact]
    public void De_baas_van_de_laatste_act_verslaan_wint_de_run()
    {
        var run = Run.Start(255, new RunSetup(Hp: 400, Map: ActMap.Path((NodeKind.Boss, Bestiary.Caster)), StartAct: Acts.MoldWorks.Number));
        for (int i = 0; i < Run.DraftRounds; i++) run.Handle(new SkipReward());
        run.Handle(new ChooseRelic(0));
        run.Handle(new ChooseNode(0));

        var events = run.WinCombat();

        Assert.Equal(RunPhase.Won, run.Phase);
        Assert.Contains(new RunEnded(Won: true), events.WithoutSeq());
        Assert.Equal(Acts.MoldWorks.Number, run.Snapshot().End!.Act);
    }

    // ---------- Startpunt in een latere act ----------

    [Fact]
    public void Een_start_in_act_2_begint_met_een_draft_uit_de_kaarten_van_act_1()
    {
        var run = Run.Start(255, new RunSetup(StartAct: 2));
        var s = run.Snapshot();

        Assert.Equal(RunPhase.Draft, s.Phase);
        Assert.Equal(2, s.Act);
        Assert.Equal(1, s.Draft!.Round);
        Assert.Equal(Run.DraftRounds, s.Draft.Rounds);
        Assert.Equal(3, s.Draft.Cards.Count);
        Assert.Equal(Run.LaterActGold, s.Gold);
        Assert.All(s.Draft.Cards, c => Assert.Contains(c.Id, Acts.VatValley.NewCards.Select(x => x.Id)));
    }

    [Fact]
    public void Na_vijf_keer_kiezen_volgt_een_relic_en_dan_de_map()
    {
        var run = Run.Start(255, new RunSetup(StartAct: 2));
        int before = run.Deck.Count;

        for (int i = 0; i < Run.DraftRounds; i++)
        {
            Assert.Equal(RunPhase.Draft, run.Phase);
            run.Handle(new TakeRewardCard(i % 3));
        }

        Assert.Equal(before + Run.DraftRounds, run.Deck.Count);
        Assert.Equal("start", run.Snapshot().RelicChoice!.Reason);

        run.Handle(new ChooseRelic(0));

        Assert.Equal(RunPhase.Map, run.Phase);
        Assert.Single(run.Relics);
        Assert.Equal(Acts.PrintShop, run.Act);
    }

    [Fact]
    public void Een_draftronde_mag_je_overslaan()
    {
        var run = Run.Start(255, new RunSetup(StartAct: 2));
        int before = run.Deck.Count;

        run.Handle(new SkipReward());

        Assert.Equal(before, run.Deck.Count);
        Assert.Equal(2, run.Snapshot().Draft!.Round);
    }

    [Fact]
    public void Dezelfde_seed_geeft_dezelfde_draft()
    {
        var a = Run.Start(77, new RunSetup(StartAct: 2)).Snapshot().Draft!.Cards.Select(c => c.Id);
        var b = Run.Start(77, new RunSetup(StartAct: 2)).Snapshot().Draft!.Cards.Select(c => c.Id);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Flight_501_zonder_cast_naar_byte_wordt_in_de_Gieterij_The_Index()
    {
        var run = StartInMoldWorks(new RunSetup(Map: ActMap.Path((NodeKind.Elite, Bestiary.Colossus), (NodeKind.Rest, null)),
            Deck: [CardCatalog.Strike, CardCatalog.MeasureTwice]));

        run.Handle(new ChooseNode(0));

        // Measure Twice telt niet: Convert crasht op 506 in plaats van om te klappen
        Assert.Equal(Bestiary.Index, run.CombatSnapshot()!.Combatants.Single(c => c.IsEnemy).Key);
    }

    // ---------- De map van de Gieterij ----------

    public static TheoryData<ulong> Seeds()
    {
        var data = new TheoryData<ulong>();
        for (ulong seed = 1; seed <= 100; seed++) data.Add(seed);
        return data;
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void De_map_van_de_Gieterij_gebruikt_haar_vijanden_en_haar_baas(ulong seed)
    {
        var map = MapGenerator.Generate(new SeededRng(seed), Acts.MoldWorks);

        Assert.Equal(Bestiary.Caster, map.Nodes.Single(n => n.Kind == NodeKind.Boss).Encounter);
        foreach (var node in map.Nodes)
        {
            switch (node.Kind)
            {
                case NodeKind.Fight:
                    Assert.Contains(node.Encounter!, node.Row <= 1 ? Acts.MoldWorks.EasyPool : Acts.MoldWorks.NormalPool);
                    break;
                case NodeKind.Elite:
                    Assert.Contains(node.Encounter!, Acts.MoldWorks.ElitePool);
                    break;
                case NodeKind.Event:
                    Assert.Contains(node.Encounter!, Acts.MoldWorks.Events);
                    break;
            }
        }
    }

    [Fact]
    public void De_kaartpool_van_een_act_bevat_ook_alle_kaarten_van_de_acts_ervoor()
    {
        var pool = Acts.CardPool(Acts.MoldWorks.Number);

        Assert.All(Acts.VatValley.NewCards, c => Assert.Contains(c, pool));
        Assert.All(Acts.PrintShop.NewCards, c => Assert.Contains(c, pool));
        Assert.Contains(CardCatalog.MeasureTwice, pool);
        Assert.DoesNotContain(CardCatalog.MeasureTwice, Acts.CardPool(Acts.PrintShop.Number));
        Assert.DoesNotContain(CardCatalog.CountLetters, Acts.CardPool(Acts.VatValley.Number));
    }
}
