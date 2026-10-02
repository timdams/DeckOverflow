namespace DeckOverflow.Engine.Cards;

public sealed record CardDefinition(
    string Id,
    string Name,
    int Cost,
    TargetMode Target,
    Effect Effect,
    string Text);
