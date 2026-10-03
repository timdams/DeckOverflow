using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;

namespace DeckOverflow.Tests;

/// <summary>
/// Bewuste intents: een aanval met variabelen uit jouw toestand. De vraag is niet "hoeveel is dit?"
/// maar "welke variabele verander ik het goedkoopst?"
/// </summary>
public class LiveIntentTests
{
    private const int P = Combat.PlayerId;

    [Fact]
    public void Een_bewuste_intent_rekent_met_een_deling_van_gehele_getallen()
    {
        var intent = Intent.Live("30 / (block + 1)", c => 30 / (c.Block + 1));

        Assert.Equal(30, intent.ValueIn(new IntentContext(0, 0, 0, 50)));
        Assert.Equal(5, intent.ValueIn(new IntentContext(5, 0, 0, 50)));
        // 30 / 4 is 7, niet 7.5
        Assert.Equal(7, intent.ValueIn(new IntentContext(3, 0, 0, 50)));
        Assert.Equal("30 / (3 + 1)", intent.FilledIn(new IntentContext(3, 0, 0, 50)));
    }

    [Fact]
    public void De_Splitter_slaat_minder_hard_naarmate_je_meer_blokt()
    {
        var combat = TestHelpers.StartWithHand(TestHelpers.Scenario(Bestiary.Splitter), CardCatalog.Shield.Id);
        Assert.Equal(30, combat.Enemy().Intent!.Value);

        combat.Play(CardCatalog.Shield.Id, P);

        var intent = combat.Enemy().Intent!;
        Assert.Equal(5, intent.Value);
        Assert.Equal("30 / (5 + 1)", intent.Filled);
        Assert.Equal("30 / (block + 1)", intent.Expression);
    }

    [Fact]
    public void Bij_de_aanval_telt_wat_je_aan_het_eind_van_je_beurt_hebt()
    {
        var combat = TestHelpers.StartWithHand(TestHelpers.Scenario(Bestiary.Splitter), CardCatalog.Shield.Id);
        combat.Play(CardCatalog.Shield.Id, P);
        double hp = combat.Player().Hp;

        var events = combat.Handle(new EndTurn()).WithoutSeq();

        Assert.Contains(new AttackLaunched(Combat.EnemyId, P, "30 / (5 + 1)", 5), events);
        // 5 schade op 5 blok: niets komt door
        Assert.Equal(hp, combat.Player().Hp);
    }

    [Fact]
    public void De_Tin_Knight_wordt_zwakker_naarmate_je_meer_kaarten_speelt()
    {
        var shields = new CombatSetup(Scenarios.Player(), Bestiary.Create(Bestiary.Knight), [.. Enumerable.Repeat(CardCatalog.Shield, 10)]);
        var combat = Combat.Start(shields, 1);
        combat.Handle(new EndTurn());
        Assert.Equal(24, combat.Enemy().Intent!.Value);

        combat.Handle(new PlayCard(0, P));
        combat.Handle(new PlayCard(0, P));

        // Elke gespeelde kaart verkleint de aanval: 24 / (2 + 1) is 8
        Assert.Equal(8, combat.Enemy().Intent!.Value);
    }

    [Fact]
    public void De_Dripper_straft_energie_die_je_overhoudt()
    {
        var combat = Combat.Start(TestHelpers.Scenario(Bestiary.Dripper), 1);
        combat.Handle(new EndTurn());

        // Begin van de beurt: 3 energie over, dus 3 * 4 + 2.5
        Assert.Equal(14.5, combat.Enemy().Intent!.Value);
        Assert.Equal("3 * 4 + 2.5", combat.Enemy().Intent!.Filled);
    }

    [Fact]
    public void Een_vaste_intent_heeft_geen_ingevulde_expressie()
    {
        var combat = Combat.Start(TestHelpers.Scenario(Bestiary.Slime), 1);

        Assert.Null(combat.Enemy().Intent!.Filled);
    }
}
