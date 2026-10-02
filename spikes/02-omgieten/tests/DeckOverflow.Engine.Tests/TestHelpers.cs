using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;

namespace DeckOverflow.Tests;

internal static class TestHelpers
{
    /// <summary>
    /// Start een gevecht met de eerste seed waarbij deze kaarten in de openingshand zitten.
    /// Zo hangt een test niet af van één toevallig gekozen seed.
    /// </summary>
    public static Combat StartWithHand(CombatSetup setup, params string[] cardIds)
    {
        for (ulong seed = 1; seed < 10_000; seed++)
        {
            var combat = Combat.Start(setup, seed);
            var hand = combat.Snapshot().Hand.Select(c => c.Id).ToList();
            if (cardIds.All(id => hand.Remove(id))) return combat;
        }
        throw new InvalidOperationException($"Geen seed met {string.Join(", ", cardIds)} in de openingshand.");
    }

    public static Combat StartWithHand(string scenario, params string[] cardIds) =>
        StartWithHand(Scenarios.Create(scenario), cardIds);

    public static int HandIndexOf(this Combat combat, string cardId)
    {
        var hand = combat.Snapshot().Hand;
        for (int i = 0; i < hand.Count; i++)
        {
            if (hand[i].Id == cardId) return i;
        }
        throw new InvalidOperationException($"Kaart '{cardId}' zit niet in de hand.");
    }

    public static IReadOnlyList<GameEvent> Play(this Combat combat, string cardId, int targetId) =>
        combat.Handle(new PlayCard(combat.HandIndexOf(cardId), targetId));

    public static CombatantView Enemy(this Combat combat) =>
        combat.Snapshot().Combatants.Single(c => c.IsEnemy);

    public static CombatantView Player(this Combat combat) =>
        combat.Snapshot().Combatants.Single(c => !c.IsEnemy);

    /// <summary>Events zonder volgnummer, zodat tests op inhoud vergelijken.</summary>
    public static List<GameEvent> WithoutSeq(this IEnumerable<GameEvent> events) =>
        [.. events.Select(e => e with { Seq = 0 })];
}
