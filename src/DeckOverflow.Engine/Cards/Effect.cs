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

/// <summary>
/// Omzetten met <c>Convert</c>: rondt af in plaats van af te kappen, en is <i>checked</i>.
/// Past het getal niet in het nieuwe type, dan volgt een <c>OverflowException</c> en crasht het doelwit.
/// </summary>
public sealed record ConvertEffect(ValueKind To) : Effect;

/// <summary>De <c>Length</c> van de tekst van een vijand wordt zijn HP, als <c>int</c>.</summary>
public sealed record LengthEffect : Effect;

/// <summary><c>int.Parse</c> op een vijand: zijn tekst-HP wordt een getal. Ongeldige tekst crasht, en je beurt eindigt.</summary>
public sealed record ParseEffect : Effect;

/// <summary>Toekenning: de aanval van de vijand wordt deze beurt <paramref name="Value"/>, wat er ook stond.</summary>
public sealed record SetAttackEffect(int Value) : Effect;

/// <summary>
/// Verandert de waarde van je volgende kaart, met echte C#-operatoren op een waarde met een type.
/// De volgorde van modifiers telt. <paramref name="DoubleHits"/>: de kaart slaat twee keer zo vaak (Split).
/// </summary>
public sealed record ModifierEffect(ModifierOp Op, TypedValue Operand, bool DoubleHits = false) : Effect
{
    public ModifierEffect(ModifierOp op, int amount) : this(op, TypedValue.Int(amount)) { }
}

/// <summary>Twee effecten na elkaar op hetzelfde doelwit.</summary>
public sealed record ComboEffect(Effect First, Effect Then) : Effect;

[JsonConverter(typeof(JsonStringEnumConverter<ModifierOp>))]
public enum ModifierOp { Add, Multiply, Divide, Parse }
