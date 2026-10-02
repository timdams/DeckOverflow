namespace DeckOverflow.Engine.Cards;

public abstract record Effect;
public sealed record DamageEffect(int Amount) : Effect;
public sealed record BlockEffect(int Amount) : Effect;
public sealed record HealEffect(int Amount) : Effect;
