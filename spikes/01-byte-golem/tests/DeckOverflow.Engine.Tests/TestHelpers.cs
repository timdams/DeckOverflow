using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;

namespace DeckOverflow.Tests;

internal static class TestHelpers
{
    public static Combat StartDefault() =>
        Combat.Start(ByteGolemScenario.Create(), ByteGolemScenario.DefaultSeed);

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

    /// <summary>Events zonder volgnummer, zodat tests op inhoud vergelijken.</summary>
    public static List<GameEvent> WithoutSeq(this IEnumerable<GameEvent> events) =>
        [.. events.Select(e => e with { Seq = 0 })];
}
