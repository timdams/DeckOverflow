using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;

namespace DeckOverflow.Tests;

/// <summary>Wachtende modifiers wegvegen: Ink tegen een getal mag je nooit vastzetten.</summary>
public class ScrapTests
{
    private const int E = Combat.EnemyId;

    private static Combat InkedAgainstKnight()
    {
        var setup = new CombatSetup(Scenarios.Player(), Bestiary.Create(Bestiary.Knight), [.. TestHelpers.TestDeck, CardCatalog.Ink]);
        var combat = TestHelpers.StartWithHand(setup, CardCatalog.Ink.Id, CardCatalog.Strike.Id);
        combat.Play(CardCatalog.Ink.Id, Combat.PlayerId);
        return combat;
    }

    [Fact]
    public void Na_Ink_raakt_een_Whack_geen_getal_meer()
    {
        var combat = InkedAgainstKnight();

        var events = combat.Play(CardCatalog.Strike.Id, E);

        Assert.Equal("reject.text-not-number", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
    }

    [Fact]
    public void Wegvegen_kost_een_energie_en_dan_slaat_de_Whack_gewoon()
    {
        var combat = InkedAgainstKnight();
        int energy = combat.Snapshot().Energy;

        var events = combat.Handle(new ScrapModifiers()).WithoutSeq();

        Assert.Contains(new ModifiersScrapped("+\"1\"", Combat.ScrapCost), events);
        Assert.Equal(energy - Combat.ScrapCost, combat.Snapshot().Energy);
        Assert.Empty(combat.Snapshot().Modifiers);
        Assert.Contains(combat.Play(CardCatalog.Strike.Id, E), e => e is DamageDealt);
    }

    [Fact]
    public void Niets_om_weg_te_vegen_weigert_en_kost_niets()
    {
        var combat = Combat.Start(new CombatSetup(Scenarios.Player(), Bestiary.Create(Bestiary.Knight), TestHelpers.TestDeck), 1);
        int energy = combat.Snapshot().Energy;

        var events = combat.Handle(new ScrapModifiers());

        Assert.Equal("reject.nothing-to-scrap", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
        Assert.Equal(energy, combat.Snapshot().Energy);
    }

    [Fact]
    public void Zonder_energie_wacht_het_wegvegen_tot_je_volgende_beurt()
    {
        var combat = InkedAgainstKnight();
        while (combat.Snapshot().Energy > 0)
        {
            var hand = combat.Snapshot().Hand;
            int i = hand.ToList().FindIndex(c => c.Playable && c.Target == TargetMode.Self && c.Id != CardCatalog.Ink.Id);
            if (i < 0) break;
            combat.Handle(new PlayCard(i, Combat.PlayerId));
        }
        if (combat.Snapshot().Energy == 0)
            Assert.Equal("reject.no-energy", Assert.IsType<PlayRejected>(Assert.Single(combat.Handle(new ScrapModifiers()))).Reason);

        combat.Handle(new EndTurn());

        Assert.Contains(combat.Handle(new ScrapModifiers()), e => e is ModifiersScrapped);
    }
}
