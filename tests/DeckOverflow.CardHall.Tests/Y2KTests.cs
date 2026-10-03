using DeckOverflow.CardHall.Achievements;
using DeckOverflow.CardHall.Cards;
using DeckOverflow.Core.Codex;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Core.Values;

namespace DeckOverflow.Tests;

/// <summary>Y2K, de tweede elite van de Drukkerij: een jaartal als tekst, <c>"19" + 100</c> is <c>"19100"</c>.</summary>
public class Y2KTests
{
    private const int E = Combat.EnemyId;

    private static CombatSetup Against(params CardDefinition[] extra) =>
        new(Scenarios.Player(200, 200), Bestiary.Create(Bestiary.Y2K), [.. TestHelpers.TestDeck, .. extra]);

    [Fact]
    public void Zijn_HP_is_het_jaartal_als_tekst()
    {
        var combat = Combat.Start(Against(), 1);

        Assert.Equal(ValueKind.String, combat.Enemy().Kind);
        Assert.Equal("1997", combat.Enemy().Text);
    }

    [Fact]
    public void Na_drie_beurten_is_het_jaar_19100()
    {
        var combat = Combat.Start(Against(), 1);
        var texts = new List<string>();

        for (int turn = 0; turn < 4; turn++)
            texts.AddRange(combat.Handle(new EndTurn()).OfType<TextReset>().Select(r => r.Text));

        Assert.Equal(["1998", "1999", "19100", "19101"], texts);
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.StringConcat && m.Values["value"] == "\"19100\"");
    }

    [Fact]
    public void Wat_je_eraan_plakte_is_na_zijn_beurt_weg()
    {
        var combat = TestHelpers.StartWithHand(Against(), CardCatalog.Strike.Id);
        combat.Play(CardCatalog.Strike.Id, E);
        Assert.Equal("19976", combat.Enemy().Text);

        combat.Handle(new EndTurn());

        Assert.Equal("1998", combat.Enemy().Text);
    }

    [Fact]
    public void Een_double_is_lange_tekst_en_crasht_hem_in_een_kaart()
    {
        var combat = TestHelpers.StartWithHand(Against(), CardCatalog.FloatingStrike.Id);

        var events = combat.Play(CardCatalog.FloatingStrike.Id, E).WithoutSeq();

        // "1997" + "2.5" + "2.5" + "2.5" is 13 tekens
        Assert.Single(events.OfType<TextCrashed>());
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
        Assert.DoesNotContain(XRegister.AfterMidnight, XRegister.Earned(events, Bestiary.Y2K));
        Assert.DoesNotContain(XRegister.MessageTooLong, XRegister.Earned(events, Bestiary.Y2K));
    }

    [Fact]
    public void Wie_hem_na_middernacht_verslaat_verdient_een_paneel()
    {
        var combat = TestHelpers.StartWithHand(Against(), CardCatalog.FloatingStrike.Id);
        // Drie beurten wachten tot het jaar 19100 is; de hand wisselt, dus zoek daarna een Floating Strike
        for (int turn = 0; turn < 3; turn++) combat.Handle(new EndTurn());
        Assert.Equal("19100", combat.Enemy().Text);

        var events = new List<GameEvent>();
        foreach (int i in Enumerable.Range(0, 20))
        {
            if (combat.Outcome != CombatOutcome.Ongoing) break;
            var hand = combat.Snapshot().Hand;
            int attack = hand.ToList().FindIndex(c => c.Playable && c.Target == TargetMode.Enemy);
            events.AddRange(attack >= 0 ? combat.Handle(new PlayCard(attack, E)) : combat.Handle(new EndTurn()));
        }

        Assert.Equal(CombatOutcome.Won, combat.Outcome);
        Assert.Contains(XRegister.AfterMidnight, XRegister.Earned(events, Bestiary.Y2K));
    }

    [Fact]
    public void Y2K_is_een_elite_van_de_Drukkerij()
    {
        Assert.Contains(Bestiary.Y2K, Acts.PrintShop.ElitePool);
        Assert.Contains(Bestiary.Y2K, Bestiary.Elites);
        Assert.Contains(Bestiary.Y2K, Bestiary.All);
    }
}
