using System.Text.Json.Serialization;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

public abstract record Effect;

/// <summary>Schade in <paramref name="Hits"/> aparte treffers. Elk doelwit past zijn typeregels per treffer toe.</summary>
public sealed record DamageEffect(double Amount, int Hits = 1) : Effect;
public sealed record BlockEffect(int Amount) : Effect;
public sealed record HealEffect(int Amount) : Effect;

/// <summary>Omgieten: verander het type van het doelwit.</summary>
public sealed record CastEffect(ValueKind To) : Effect;

/// <summary>Toekenning: de aanval van de vijand wordt deze beurt <paramref name="Value"/>, wat er ook stond.</summary>
public sealed record SetAttackEffect(int Value) : Effect;

/// <summary>Verandert het getal op je volgende kaart. De volgorde van modifiers telt.</summary>
public sealed record ModifierEffect(ModifierOp Op, int Amount) : Effect;

/// <summary>Twee effecten na elkaar op hetzelfde doelwit.</summary>
public sealed record ComboEffect(Effect First, Effect Then) : Effect;

[JsonConverter(typeof(JsonStringEnumConverter<ModifierOp>))]
public enum ModifierOp { Add, Multiply }
