using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.Tests;

/// <summary>Teases uit de teaseladder: onderdelen die nergens voor dienen, en een vijand met een regel als intent.</summary>
public class TeaseTests
{
    private const int P = Combat.PlayerId;

    /// <summary>Een kist openen op de eerste seed waarbij er een onderdeel in zit.</summary>
    private static (Run Run, IReadOnlyList<GameEvent> Events) OpenChestWithTrinket()
    {
        for (ulong seed = 1; seed < 200; seed++)
        {
            var run = Run.Start(seed, new RunSetup(Opening: false, Map: ActMap.Path((NodeKind.Treasure, null), (NodeKind.Rest, null))));
            run.Handle(new ChooseNode(0));
            var events = run.Handle(new OpenChest());
            if (events.OfType<TrinketFound>().Any()) return (run, events);
        }
        throw new InvalidOperationException("Geen seed met een onderdeel in de kist.");
    }

    [Fact]
    public void Een_kist_bevat_soms_een_onderdeel_met_een_labeltje()
    {
        var (run, events) = OpenChestWithTrinket();

        var found = Assert.Single(events.OfType<TrinketFound>());
        Assert.Contains(Trinkets.All, t => t.Key == found.Key && t.Step == found.Step);
        Assert.Equal([found.Key], run.Snapshot().Trinkets);
        Assert.Equal(found.Key, run.Snapshot().Treasure!.Trinket);
    }

    [Fact]
    public void Niet_elke_kist_bevat_een_onderdeel()
    {
        int with = 0;
        for (ulong seed = 1; seed <= 100; seed++)
        {
            var run = Run.Start(seed, new RunSetup(Opening: false, Map: ActMap.Path((NodeKind.Treasure, null), (NodeKind.Rest, null))));
            run.Handle(new ChooseNode(0));
            if (run.Handle(new OpenChest()).OfType<TrinketFound>().Any()) with++;
        }

        Assert.InRange(with, 30, 70);
    }

    [Fact]
    public void Elk_onderdeel_hoort_bij_een_afdeling_na_de_Card_Hall()
    {
        Assert.All(Trinkets.All, t => Assert.InRange(t.Step, 5, 9));
        Assert.Equal(Trinkets.All.Count, Trinkets.All.Select(t => t.Key).Distinct().Count());
    }

    [Fact]
    public void De_Stray_Automaton_slaat_dubbel_als_je_blokt()
    {
        var combat = TestHelpers.StartWithHand(TestHelpers.Scenario(Bestiary.Stray), CardCatalog.Shield.Id);
        Assert.Equal(8, combat.Enemy().Intent!.Value);
        Assert.Equal("block > 0 ? 16 : 8", combat.Enemy().Intent!.Expression);

        combat.Play(CardCatalog.Shield.Id, P);

        Assert.Equal(16, combat.Enemy().Intent!.Value);
    }

    [Fact]
    public void De_Stray_Automaton_dwaalt_rond_in_act_1_en_2_maar_niet_in_de_Gieterij()
    {
        Assert.Contains(Bestiary.Stray, Acts.VatValley.NormalPool);
        Assert.Contains(Bestiary.Stray, Acts.PrintShop.NormalPool);
        Assert.DoesNotContain(Bestiary.Stray, Acts.MoldWorks.NormalPool);
    }
}
