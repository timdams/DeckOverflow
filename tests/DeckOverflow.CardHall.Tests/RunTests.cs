using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Relics;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.Tests;

public class RunTests
{
    private const int E = Combat.EnemyId;

    private static Run OnPath(params (NodeKind, string?)[] steps) => OnPath(new RunSetup(), steps);

    private static Run OnPath(RunSetup setup, params (NodeKind, string?)[] steps) =>
        Run.Start(255, setup with { Map = ActMap.Path(steps), Opening = false });

    private static Run OnMap(ulong seed = 255) => Run.Start(seed, TestHelpers.NoOpening);

    // ---------- Start en map ----------

    [Fact]
    public void Zonder_opening_begint_een_run_op_de_map_met_het_starterdeck()
    {
        var run = OnMap();
        var s = run.Snapshot();

        Assert.Equal(RunPhase.Map, s.Phase);
        Assert.Equal(50, s.Hp);
        Assert.Equal(60, s.Gold);
        Assert.Equal(10, s.Deck.Count);
        Assert.Empty(s.Relics);
        Assert.All(s.Map.Nodes.Where(n => n.Reachable), n => Assert.Equal(0, n.Row));
        Assert.Equal(s.Map.Nodes.Count(n => n.Row == 0), s.Map.Nodes.Count(n => n.Reachable));
    }

    [Fact]
    public void Een_knoop_buiten_bereik_wordt_geweigerd()
    {
        var run = OnMap();
        int far = run.Map.Nodes.First(n => n.Row == 3).Id;

        var events = run.Handle(new ChooseNode(far));

        Assert.IsType<RunRejected>(Assert.Single(events));
        Assert.Equal(RunPhase.Map, run.Phase);
    }

    [Fact]
    public void Een_gevechtscommand_op_de_map_wordt_geweigerd()
    {
        var run = OnMap();

        var events = run.Handle(new EndTurn());

        Assert.IsType<RunRejected>(Assert.Single(events));
    }

    [Fact]
    public void Na_een_knoop_kan_je_alleen_verder_langs_zijn_paden()
    {
        var run = OnMap();
        var start = run.Map.StartNodes.First();
        run.Handle(new ChooseNode(start.Id));
        run.WinCombat();
        run.Handle(new SkipReward());

        var reachable = run.Snapshot().Map.Nodes.Where(n => n.Reachable).Select(n => n.Id);

        Assert.Equal(run.Map.Children(start.Id).Select(n => n.Id).Order(), reachable.Order());
    }

    // ---------- Gevecht en beloning ----------

    [Fact]
    public void Een_gewonnen_gevecht_geeft_goud_en_drie_kaarten_om_uit_te_kiezen()
    {
        var run = OnPath((NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        Assert.Equal(RunPhase.Combat, run.Phase);
        Assert.NotNull(run.CombatSnapshot());

        var events = run.WinCombat();
        var s = run.Snapshot();

        Assert.Equal(RunPhase.Reward, s.Phase);
        var gold = Assert.Single(events.OfType<GoldChanged>());
        Assert.InRange(gold.Amount, 12, 20);
        Assert.Equal(60 + gold.Amount, s.Gold);
        Assert.Equal(3, s.Reward!.Cards.Count);
        Assert.Equal(3, s.Reward.Cards.Select(c => c.Id).Distinct().Count());
        Assert.Null(s.Reward.Relic);
    }

    [Fact]
    public void HP_loopt_door_van_het_gevecht_naar_de_run()
    {
        var run = OnPath((NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        run.Handle(new EndTurn());   // Slijmklodder slaat 2 * 3
        int inCombat = (int)run.CombatSnapshot()!.Combatants.Single(c => !c.IsEnemy).Hp;
        Assert.Equal(44, inCombat);
        Assert.Equal(44, run.Snapshot().Hp);

        run.WinCombat();

        Assert.Equal(run.Snapshot().Hp, run.Hp);
        Assert.True(run.Hp <= 44);
    }

    [Fact]
    public void Een_beloningskaart_kiezen_voegt_ze_toe_en_brengt_je_terug_naar_de_map()
    {
        var run = OnPath((NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        run.WinCombat();
        string picked = run.Snapshot().Reward!.Cards[1].Id;

        var events = run.Handle(new TakeRewardCard(1));

        Assert.Contains(new CardAdded(picked), events.WithoutSeq());
        Assert.Equal(11, run.Deck.Count);
        Assert.Equal(RunPhase.Map, run.Phase);
        Assert.True(run.Snapshot().Map.Nodes.Single(n => n.Id == 1).Reachable);
    }

    [Fact]
    public void Een_elite_geeft_ook_een_relic()
    {
        var run = OnPath((NodeKind.Elite, Bestiary.Golem), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));

        // De golem heelt zichzelf tot hij omklapt, ook als je niets doet
        var events = run.WinCombat(passive: true);

        var relic = Assert.Single(events.OfType<RelicGained>());
        Assert.Contains(relic.RelicId, run.Relics);
        Assert.Equal(relic.RelicId, run.Snapshot().Reward!.Relic!.Id);
        Assert.InRange(events.OfType<GoldChanged>().Single().Amount, 28, 40);
    }

    [Fact]
    public void Verlies_eindigt_de_run_en_toont_hoe_dicht_je_erbij_was()
    {
        var deck = TestHelpers.TestDeck;
        var run = OnPath(new RunSetup(Deck: deck), (NodeKind.Fight, Bestiary.Colossus), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));

        IReadOnlyList<GameEvent> events = [];
        while (run.Phase == RunPhase.Combat) events = run.Handle(new EndTurn());

        var s = run.Snapshot();
        Assert.Equal(RunPhase.Lost, s.Phase);
        Assert.Contains(new RunEnded(Won: false), events.WithoutSeq());
        Assert.Equal(Bestiary.Colossus, s.End!.EnemyKey);
        Assert.Equal(1, s.End.Floor);
        Assert.Equal(0, s.Hp);
    }

    [Fact]
    public void De_baas_van_act_1_verslaan_opent_act_2_in_plaats_van_de_run_te_winnen()
    {
        var run = OnPath(new RunSetup(Hp: 300), (NodeKind.Boss, Bestiary.Reckoner));
        run.Handle(new ChooseNode(0));

        var events = run.WinCombat();

        Assert.Equal(RunPhase.RelicChoice, run.Phase);
        Assert.Contains(new ActCompleted(1), events.WithoutSeq());
        Assert.DoesNotContain(events, e => e is RunEnded);
    }

    [Fact]
    public void Ankervat_geeft_blok_bij_de_start_van_een_gevecht()
    {
        var run = OnPath(new RunSetup(Relics: [RelicCatalog.AnchorBarrel]), (NodeKind.Fight, Bestiary.Slime));
        run.Handle(new ChooseNode(0));

        Assert.Equal(AnchorBarrelRelic.Block, run.CombatSnapshot()!.Combatants.Single(c => !c.IsEnemy).Block);
    }

    // ---------- Rustvuur ----------

    [Fact]
    public void Rusten_heelt_30_procent_maar_niet_boven_max()
    {
        var run = OnPath((NodeKind.Event, Adventures.LeakingBarrel), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        run.Handle(new ChooseEventOption(0));
        run.Handle(new Leave());
        Assert.Equal(43, run.Hp);

        run.Handle(new ChooseNode(1));
        Assert.Equal(15, run.Snapshot().Rest!.HealAmount);
        var events = run.Handle(new RestHeal());

        Assert.Contains(new RunHpChanged(Amount: 7, Hp: 50, MaxHp: 50), events.WithoutSeq());
        Assert.NotNull(run.Snapshot().Rest!.Outcome);
        Assert.IsType<RunRejected>(Assert.Single(run.Handle(new RestHeal())));
    }

    [Fact]
    public void Aan_het_rustvuur_verbeter_je_een_kaart()
    {
        var run = OnPath((NodeKind.Rest, null), (NodeKind.Fight, Bestiary.Slime));
        run.Handle(new ChooseNode(0));

        var events = run.Handle(new RestUpgrade(0));
        run.Handle(new Leave());

        Assert.Contains(new CardTransformed("strike", "strike+"), events.WithoutSeq());
        Assert.Equal("strike+", run.Deck[0].Id);
        Assert.Equal(RunPhase.Map, run.Phase);
    }

    [Fact]
    public void Het_rustvuur_verlaten_zonder_te_kiezen_kan_niet()
    {
        var run = OnPath((NodeKind.Rest, null), (NodeKind.Fight, Bestiary.Slime));
        run.Handle(new ChooseNode(0));

        Assert.IsType<RunRejected>(Assert.Single(run.Handle(new Leave())));
    }

    // ---------- Events ----------

    [Fact]
    public void De_Smeltkroes_giet_een_Vlottende_kaart_om_naar_een_gratis_int_kaart()
    {
        var run = OnPath((NodeKind.Event, Adventures.Crucible), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        var option = run.Snapshot().Event!.Options[0];
        int vlottend = run.Deck.ToList().FindIndex(c => c.Id == "floating-strike");
        Assert.Contains(vlottend, option.EligibleCards);

        var events = run.Handle(new ChooseEventOption(0, vlottend));

        Assert.Contains(new CardTransformed("floating-strike", "molded-strike"), events.WithoutSeq());
        Assert.Equal(0, run.Deck[vlottend].Cost);
        Assert.NotNull(run.Snapshot().Event!.Outcome);
        Assert.Empty(run.Snapshot().Event!.Options);
    }

    [Fact]
    public void De_Smeltkroes_zonder_kaart_kiezen_wordt_geweigerd()
    {
        var run = OnPath((NodeKind.Event, Adventures.Crucible), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));

        var events = run.Handle(new ChooseEventOption(0));

        Assert.IsType<RunRejected>(Assert.Single(events));
        Assert.Null(run.Snapshot().Event!.Outcome);
    }

    [Fact]
    public void De_Smeltkroes_zonder_Vlottende_kaarten_biedt_omgieten_niet_aan()
    {
        var run = OnPath(new RunSetup(Deck: [CardCatalog.Strike, CardCatalog.Shield]), (NodeKind.Event, Adventures.Crucible));
        run.Handle(new ChooseNode(0));

        var option = run.Snapshot().Event!.Options[0];

        Assert.False(option.Enabled);
        Assert.Equal("reject.no-floating-card", option.DisabledReason);
    }

    [Fact]
    public void Een_kaart_wegsmelten_kost_HP()
    {
        var run = OnPath((NodeKind.Event, Adventures.Crucible), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));

        run.Handle(new ChooseEventOption(1, 7));

        Assert.Equal(9, run.Deck.Count);
        Assert.Equal(50 - Adventures.MeltHpCost, run.Hp);
    }

    // ---------- Winkel ----------

    [Fact]
    public void In_de_winkel_koop_je_een_kaart_met_goud()
    {
        var run = OnPath(new RunSetup(Gold: 500), (NodeKind.Shop, null));
        run.Handle(new ChooseNode(0));
        var offer = run.Snapshot().Shop!.Cards[0];

        run.Handle(new BuyCard(0));

        Assert.Equal(500 - offer.Price, run.Gold);
        Assert.Equal(offer.Card.Id, run.Deck[^1].Id);
        Assert.True(run.Snapshot().Shop!.Cards[0].Sold);
        Assert.IsType<RunRejected>(Assert.Single(run.Handle(new BuyCard(0))));
    }

    [Fact]
    public void De_winkel_biedt_twee_gewone_twee_ongewone_en_een_zeldzame_kaart()
    {
        var run = OnPath((NodeKind.Shop, null));
        run.Handle(new ChooseNode(0));

        var rarities = run.Snapshot().Shop!.Cards.Select(c => c.Card.Rarity);

        Assert.Equal([Rarity.Common, Rarity.Common, Rarity.Uncommon, Rarity.Uncommon, Rarity.Rare], rarities);
    }

    [Fact]
    public void Zonder_genoeg_goud_koop_je_niets()
    {
        var run = OnPath(new RunSetup(Gold: 10), (NodeKind.Shop, null));
        run.Handle(new ChooseNode(0));

        var events = run.Handle(new BuyCard(0));

        Assert.Equal(new RunRejected("reject.not-enough-gold"), Assert.Single(events) with { Seq = 0 });
        Assert.Equal(10, run.Gold);
    }

    [Fact]
    public void De_smid_verwijdert_een_kaart_per_bezoek()
    {
        var run = OnPath(new RunSetup(Gold: 500), (NodeKind.Shop, null));
        run.Handle(new ChooseNode(0));

        run.Handle(new BuyRemoval(0));
        var second = run.Handle(new BuyRemoval(0));

        Assert.Equal(9, run.Deck.Count);
        Assert.Equal(500 - Run.RemovalPrice, run.Gold);
        Assert.IsType<RunRejected>(Assert.Single(second));
    }

    // ---------- Schat ----------

    [Fact]
    public void Een_schat_open_je_eerst_en_dan_ga_je_verder()
    {
        var run = OnPath((NodeKind.Treasure, null), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        Assert.Null(run.Snapshot().Treasure!.Relic);
        Assert.IsType<RunRejected>(Assert.Single(run.Handle(new Leave())));

        var events = run.Handle(new OpenChest());

        var relic = Assert.Single(events.OfType<RelicGained>());
        Assert.Equal(relic.RelicId, run.Snapshot().Treasure!.Relic!.Id);
        run.Handle(new Leave());
        Assert.Equal(RunPhase.Map, run.Phase);
    }

    [Fact]
    public void Grote_Pot_verhoogt_max_HP_en_heelt_evenveel()
    {
        var others = RelicCatalog.All.Where(id => id != RelicCatalog.GreatPot).ToList();
        var run = OnPath(new RunSetup(Relics: others), (NodeKind.Treasure, null));
        run.Handle(new ChooseNode(0));

        run.Handle(new OpenChest());

        Assert.Equal(58, run.MaxHp);
        Assert.Equal(58, run.Hp);
    }

    // ---------- Determinisme ----------

    [Fact]
    public void Zelfde_seed_en_keuzes_geven_dezelfde_run()
    {
        (List<GameEvent>, RunSnapshot) Play()
        {
            var run = Run.Start(4242);
            var all = new List<GameEvent>();
            all.AddRange(run.Handle(new ChooseEventOption(2)));
            all.AddRange(run.Handle(new Leave()));
            all.AddRange(run.Handle(new ChooseNode(run.Map.StartNodes.First().Id)));
            all.AddRange(run.WinCombat());
            all.AddRange(run.Handle(new TakeRewardCard(0)));
            return (all, run.Snapshot());
        }

        var (eventsA, snapA) = Play();
        var (eventsB, snapB) = Play();

        Assert.Equal(eventsA, eventsB);
        Assert.Equal(snapA.Deck, snapB.Deck);
        Assert.Equal(snapA.Gold, snapB.Gold);
    }

    [Fact]
    public void Seq_loopt_door_over_map_en_gevecht()
    {
        var run = OnPath((NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        var events = new List<GameEvent>();
        events.AddRange(run.Handle(new ChooseNode(0)));
        events.AddRange(run.Handle(new EndTurn()));
        events.AddRange(run.WinCombat());

        Assert.Equal(Enumerable.Range(1, events.Count), events.Select(e => e.Seq));
    }
}
