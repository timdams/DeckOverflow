using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

/// <summary>Alleen-lezen beeld van een kaart buiten een gevecht: deck, beloning, winkel.</summary>
/// <param name="Upgrade">De verbeterde versie, als die bestaat. Zo toont het rustvuur wat je krijgt.</param>
public sealed record CardInfo(
    string Id,
    string Name,
    int Cost,
    string Text,
    TargetMode Target,
    ValueKind? Kind,
    ValueKind? CastTo,
    Rarity Rarity,
    CardInfo? Upgrade)
{
    public static CardInfo From(CardDefinition card) => new(
        card.Id, card.Name, card.Cost, card.Text, card.Target, card.Kind, card.CastTo, card.Rarity,
        CardCatalog.Upgrade(card) is { } up ? From(up) : null);
}
