using System.Text.Json.Serialization;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

[JsonConverter(typeof(JsonStringEnumConverter<Rarity>))]
public enum Rarity { Starter, Common, Uncommon, Rare }

/// <summary>
/// Een kaart zoals de motor ze kent: kost, doelwit en effect. Naam en tekst staan niet hier
/// maar in <c>en.json</c>; de naam volgt uit de id, de tekst uit het effect (<see cref="CardText"/>).
/// </summary>
/// <param name="Kind">Het type van de aanval. Leeg voor kaarten die geen waarde afvuren.</param>
public sealed record CardDefinition(
    string Id,
    int Cost,
    TargetMode Target,
    Effect Effect,
    ValueKind? Kind = null,
    Rarity Rarity = Rarity.Starter)
{
    public bool IsUpgraded => Id.EndsWith('+');

    /// <summary>De id zonder "+": daarop hangt de naam in <c>en.json</c>.</summary>
    public string BaseId => Id.TrimEnd('+');

    /// <summary>Het type waarnaar deze kaart giet, als ze dat doet.</summary>
    public ValueKind? CastTo => Effect switch
    {
        CastEffect c => c.To,
        ConvertEffect c => c.To,
        ComboEffect { First: CastEffect c } => c.To,
        _ => null
    };
}
