using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

/// <param name="Kind">Het type van de aanval. Leeg voor kaarten die geen waarde afvuren.</param>
public sealed record CardDefinition(
    string Id,
    string Name,
    int Cost,
    TargetMode Target,
    Effect Effect,
    string Text,
    ValueKind? Kind = null);
