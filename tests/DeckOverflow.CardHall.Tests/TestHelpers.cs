using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.Tests;

internal static class TestHelpers
{
    /// <summary>
    /// Een oud starterdeck, met Omgieten en Herstel. Gevechtstests hebben die kaarten nodig;
    /// het echte starterdeck laat ze bewust weg.
    /// </summary>
    public static IReadOnlyList<CardDefinition> TestDeck =>
    [
        CardCatalog.Strike, CardCatalog.Strike, CardCatalog.Strike,
        CardCatalog.FloatingStrike, CardCatalog.FloatingStrike,
        CardCatalog.Shield, CardCatalog.Shield,
        CardCatalog.Mend,
        CardCatalog.RemoldInt, CardCatalog.RemoldByte
    ];

    /// <summary>Een los gevecht tegen deze vijand met het testdeck.</summary>
    public static CombatSetup Scenario(string enemy) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), TestDeck);

    /// <summary>Een run zonder openingskeuze: je staat meteen op de map.</summary>
    public static readonly RunSetup NoOpening = new(Opening: false);

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

    public static Combat StartWithHand(string enemy, params string[] cardIds) =>
        StartWithHand(Scenario(enemy), cardIds);

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

    /// <summary>
    /// Speelt het lopende gevecht van een run uit met een domme maar eerlijke strategie:
    /// eerst aanvallen, dan blokken, dan einde beurt. Met <paramref name="passive"/> alleen einde beurt.
    /// </summary>
    public static List<GameEvent> WinCombat(this Run run, bool passive = false)
    {
        var all = new List<GameEvent>();
        for (int step = 0; step < 500 && run.Phase == RunPhase.Combat; step++)
        {
            var snapshot = run.CombatSnapshot()!;
            int attack = passive ? -1 : IndexWhere(snapshot.Hand, c => c.Playable && c.Kind is not null);
            int block = passive ? -1 : IndexWhere(snapshot.Hand, c => c.Playable && c.Target == CardHall.Cards.TargetMode.Self);

            ICommand command = attack >= 0 ? new PlayCard(attack, Combat.EnemyId)
                : block >= 0 ? new PlayCard(block, Combat.PlayerId)
                : new EndTurn();
            all.AddRange(run.Handle(command));
        }

        if (run.Phase is RunPhase.Combat or RunPhase.Lost)
        {
            throw new InvalidOperationException($"Gevecht niet gewonnen, fase {run.Phase}.");
        }
        return all;
    }

    private static int IndexWhere(IReadOnlyList<CardView> hand, Func<CardView, bool> predicate)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (predicate(hand[i])) return i;
        }
        return -1;
    }
}
