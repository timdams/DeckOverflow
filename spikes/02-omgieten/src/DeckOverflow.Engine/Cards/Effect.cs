using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

public abstract record Effect;

/// <summary>Schade in <paramref name="Hits"/> aparte treffers. Elk doelwit past zijn typeregels per treffer toe.</summary>
public sealed record DamageEffect(double Amount, int Hits = 1) : Effect;
public sealed record BlockEffect(int Amount) : Effect;
public sealed record HealEffect(int Amount) : Effect;

/// <summary>Omgieten: verander het type van het doelwit.</summary>
public sealed record CastEffect(ValueKind To) : Effect;
