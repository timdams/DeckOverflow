using DeckOverflow.Engine.Text;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

/// <summary>Alleen-lezen beeld van een kaart buiten een gevecht: deck, beloning, winkel.</summary>
/// <param name="Upgrade">De verbeterde versie, als die bestaat. Zo toont het rustvuur wat je krijgt.</param>
public sealed record CardInfo(
    string Id,
    int Cost,
    TextRef Text,
    TargetMode Target,
    ValueKind? Kind,
    ValueKind? CastTo,
    Rarity Rarity,
    CardInfo? Upgrade)
{
    public static CardInfo From(CardDefinition card) => new(
        card.Id, card.Cost, CardText.Of(card), card.Target, card.Kind, card.CastTo, card.Rarity,
        CardCatalog.Upgrade(card) is { } up ? From(up) : null);
}
