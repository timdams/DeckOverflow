using System.Text.Json.Serialization;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

[JsonConverter(typeof(JsonStringEnumConverter<Rarity>))]
public enum Rarity { Starter, Common, Uncommon, Rare }

/// <param name="Kind">Het type van de aanval. Leeg voor kaarten die geen waarde afvuren.</param>
public sealed record CardDefinition(
    string Id,
    string Name,
    int Cost,
    TargetMode Target,
    Effect Effect,
    string Text,
    ValueKind? Kind = null,
    Rarity Rarity = Rarity.Starter)
{
    public bool IsUpgraded => Id.EndsWith('+');

    /// <summary>Het type waarnaar deze kaart giet, als ze dat doet.</summary>
    public ValueKind? CastTo => Effect switch
    {
        CastEffect c => c.To,
        ComboEffect { First: CastEffect c } => c.To,
        _ => null
    };
}
