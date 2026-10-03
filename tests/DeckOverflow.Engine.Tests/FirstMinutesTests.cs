using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Runs;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Tests;

/// <summary>Wat de eerste minuten van een run moet laten grijpen.</summary>
public class FirstMinutesTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static Run OnPath(RunSetup setup, params (NodeKind, string?)[] steps) =>
        Run.Start(255, setup with { Map = ActMap.Path(steps), Opening = false });

    // ---------- Starterdeck ----------

    public static TheoryData<string> StarterCards()
    {
        var data = new TheoryData<string>();
        foreach (var card in CardCatalog.StarterDeck().DistinctBy(c => c.Id)) data.Add(card.Id);
        return data;
    }

    [Theory]
    [MemberData(nameof(StarterCards))]
    public void Elke_starterkaart_doet_iets_in_het_eerste_gevecht(string cardId)
    {
        var card = CardCatalog.StarterDeck().First(c => c.Id == cardId);
        foreach (string enemy in Acts.VatValley.EasyPool)
        {
            var setup = new CombatSetup(Scenarios.Player(), Bestiary.Create(enemy), [.. Enumerable.Repeat(card, 10)]);
            var combat = Combat.Start(setup, Scenarios.DefaultSeed);
            int target = card.Target == TargetMode.Enemy ? E : P;

            var events = combat.Handle(new PlayCard(0, target));

            Assert.DoesNotContain(events, e => e is PlayRejected);
            Assert.Contains(events, e => e is not CardPlayed);
        }
    }

    [Fact]
    public void Het_starterdeck_heeft_geen_Omgieten_of_Herstel()
    {
        var deck = CardCatalog.StarterDeck();

        Assert.Equal(10, deck.Count);
        Assert.DoesNotContain(deck, c => c.CastTo is not null);
        Assert.DoesNotContain(deck, c => c.Effect is HealEffect);
        // Ze bestaan nog wel, als beloning
        Assert.Contains(CardCatalog.RemoldByte, CardCatalog.RewardPool);
        Assert.Contains(CardCatalog.Mend, CardCatalog.RewardPool);
    }

    // ---------- De Gieterij ----------

    [Fact]
    public void Een_run_begint_bij_de_Gieterij_met_drie_keuzes()
    {
        var run = Run.Start(255);
        var s = run.Snapshot();

        Assert.Equal(RunPhase.Event, s.Phase);
        Assert.Equal(Adventures.Foundry, s.Event!.Key);
        Assert.Equal(3, s.Event.Options.Count);
        Assert.All(s.Event.Options, o => Assert.True(o.Enabled));
        Assert.IsType<RunRejected>(Assert.Single(run.Handle(new Leave())));
        Assert.IsType<RunRejected>(Assert.Single(run.Handle(new ChooseNode(run.Map.StartNodes.First().Id))));
    }

    [Fact]
    public void Een_kaart_uit_de_Gieterij_is_nooit_gewoon()
    {
        var run = Run.Start(255);

        run.Handle(new ChooseEventOption(0));
        var reward = run.Snapshot().Reward!;

        Assert.Equal(RunPhase.Reward, run.Phase);
        Assert.Equal(0, reward.Gold);
        Assert.Equal(3, reward.Cards.Select(c => c.Id).Distinct().Count());
        Assert.DoesNotContain(reward.Cards, c => c.Rarity == Rarity.Common);

        run.Handle(new TakeRewardCard(0));
        Assert.Equal(RunPhase.Map, run.Phase);
        Assert.Equal(11, run.Deck.Count);
    }

    [Fact]
    public void De_relic_van_de_Gieterij_zie_je_voor_je_kiest()
    {
        var run = Run.Start(255);
        var label = run.Snapshot().Event!.Options[1].Label;
        string shown = label.Args!["relic"];

        var events = run.Handle(new ChooseEventOption(1));
        run.Handle(new Leave());

        Assert.Contains(new RelicGained(shown), events.WithoutSeq());
        Assert.Equal([shown], run.Relics);
        Assert.Equal(RunPhase.Map, run.Phase);
    }

    [Fact]
    public void Goud_uit_de_Gieterij()
    {
        var run = Run.Start(255);

        run.Handle(new ChooseEventOption(2));

        Assert.Equal(60 + Run.FoundryGold, run.Gold);
    }

    [Fact]
    public void Zelfde_seed_biedt_dezelfde_Gieterij()
    {
        var a = Run.Start(77).Snapshot().Event!.Options;
        var b = Run.Start(77).Snapshot().Event!.Options;

        Assert.Equal(a.Select(o => o.Label), b.Select(o => o.Label));
    }

    // ---------- De Bottomless Jug ----------

    [Fact]
    public void De_kruik_drinkt_tot_hij_omklapt()
    {
        var setup = new CombatSetup(Scenarios.Player(), Bestiary.Create(Bestiary.Jug), CardCatalog.StarterDeck());
        var combat = Combat.Start(setup, Scenarios.DefaultSeed);

        var first = combat.Handle(new EndTurn()).WithoutSeq();
        var second = combat.Handle(new EndTurn()).WithoutSeq();

        Assert.Contains(new Healed(E, Amount: 40, HpAfter: 240), first);
        // 240 + 40 = 280 past niet in een byte: 280 - 256 = 24
        Assert.Contains(new ValueOverflowed(E, Before: 240, Added: 40, After: 24, Max: 255), second);
        Assert.Equal(24, combat.Enemy().Hp);
        Assert.Equal(ValueKind.Byte, combat.Enemy().Kind);
    }

    [Fact]
    public void De_kruik_klapt_ook_om_als_je_elke_beurt_slaat()
    {
        var run = OnPath(new RunSetup(), (NodeKind.Event, Bestiary.Jug), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        Assert.Equal(RunPhase.Combat, run.Phase);

        var events = run.WinCombat();

        Assert.Contains(events, e => e is ValueOverflowed);
        Assert.Equal(RunPhase.Reward, run.Phase);
        Assert.Equal(3, run.Snapshot().Reward!.Cards.Count);
    }

    [Fact]
    public void Een_tweede_kruik_wordt_een_gewoon_event()
    {
        var run = OnPath(new RunSetup(), (NodeKind.Event, Bestiary.Jug), (NodeKind.Event, Bestiary.Jug));
        run.Handle(new ChooseNode(0));
        run.WinCombat();
        run.Handle(new SkipReward());

        run.Handle(new ChooseNode(1));

        Assert.Equal(RunPhase.Event, run.Phase);
        Assert.Contains(run.Snapshot().Event!.Key, Adventures.All);
    }

    // ---------- Elk gevecht te winnen ----------

    [Fact]
    public void Zonder_kaart_die_naar_byte_giet_krijg_je_de_Golem_in_plaats_van_de_Kolos()
    {
        var run = OnPath(new RunSetup(), (NodeKind.Elite, Bestiary.Colossus));

        run.Handle(new ChooseNode(0));

        Assert.Equal(Bestiary.Golem, run.CombatSnapshot()!.Combatants.Single(c => c.IsEnemy).Key);
    }

    [Fact]
    public void Met_een_kaart_die_naar_byte_giet_wacht_de_Kolos()
    {
        var deck = CardCatalog.StarterDeck().Append(CardCatalog.ByteTrap).ToList();
        var run = OnPath(new RunSetup(Deck: deck), (NodeKind.Elite, Bestiary.Colossus));

        run.Handle(new ChooseNode(0));

        Assert.Equal(Bestiary.Colossus, run.CombatSnapshot()!.Combatants.Single(c => c.IsEnemy).Key);
    }
}
