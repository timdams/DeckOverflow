using System.Text.Json.Serialization;
using DeckOverflow.Core.Values;

namespace DeckOverflow.ControlRoom.Gambits;

[JsonConverter(typeof(JsonStringEnumConverter<Side>))]
public enum Side { Player, Enemy }

[JsonConverter(typeof(JsonStringEnumConverter<Outcome>))]
public enum Outcome { PlayerWon, EnemyWon, ShiftOver }

/// <summary>
/// Wat er in een duel gebeurt: het eventcontract met de stage van de Controlekamer.
/// Klein en plat, zoals de events van de Card Hall; de stage negeert wat ze niet kent.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TurnStarted), nameof(TurnStarted))]
[JsonDerivedType(typeof(RuleFired), nameof(RuleFired))]
[JsonDerivedType(typeof(NoRuleMatched), nameof(NoRuleMatched))]
[JsonDerivedType(typeof(BlockExpired), nameof(BlockExpired))]
[JsonDerivedType(typeof(Whacked), nameof(Whacked))]
[JsonDerivedType(typeof(Braced), nameof(Braced))]
[JsonDerivedType(typeof(Repaired), nameof(Repaired))]
[JsonDerivedType(typeof(RepairEmpty), nameof(RepairEmpty))]
[JsonDerivedType(typeof(WoundUp), nameof(WoundUp))]
[JsonDerivedType(typeof(ValueTruncated), nameof(ValueTruncated))]
[JsonDerivedType(typeof(ValueOverflowed), nameof(ValueOverflowed))]
[JsonDerivedType(typeof(ValueRounded), nameof(ValueRounded))]
[JsonDerivedType(typeof(TextAppended), nameof(TextAppended))]
[JsonDerivedType(typeof(TextCrashed), nameof(TextCrashed))]
[JsonDerivedType(typeof(CounterOverflowed), nameof(CounterOverflowed))]
[JsonDerivedType(typeof(TypeChanged), nameof(TypeChanged))]
[JsonDerivedType(typeof(TypeUnchanged), nameof(TypeUnchanged))]
[JsonDerivedType(typeof(ConversionCrashed), nameof(ConversionCrashed))]
[JsonDerivedType(typeof(MoveSkipped), nameof(MoveSkipped))]
[JsonDerivedType(typeof(TextParsed), nameof(TextParsed))]
[JsonDerivedType(typeof(ParseCrashed), nameof(ParseCrashed))]
[JsonDerivedType(typeof(DuelEnded), nameof(DuelEnded))]
public abstract record DuelEvent;

public sealed record TurnStarted(int Turn) : DuelEvent;
/// <summary>De regel met deze index klopte als eerste. De regels erboven werden bekeken, die eronder niet.</summary>
public sealed record RuleFired(Side Side, int RuleIndex, Move Move) : DuelEvent;
/// <summary>Geen enkele regel klopte: de automaat doet niets.</summary>
public sealed record NoRuleMatched(Side Side) : DuelEvent;
public sealed record BlockExpired(Side Side, int Amount) : DuelEvent;
/// <param name="Damage">De klap zoals de automaat ze gaf, eventueel een kommagetal.</param>
/// <param name="Dealt">Wat er echt van de HP af ging: na het blok, afgekapt tot een geheel getal.</param>
public sealed record Whacked(Side Side, double Damage, int Absorbed, int Dealt, int HpAfter, bool WasCharged) : DuelEvent;
/// <summary>Schade met een kommagetal op een geheel type: alles na de komma valt weg, het rondt nooit af.</summary>
public sealed record ValueTruncated(Side Target, double Before, int After) : DuelEvent;
/// <summary>Herstel op een <c>byte</c> voorbij 255: de waarde loopt over en begint opnieuw bij 0.</summary>
public sealed record ValueOverflowed(Side Side, int Before, int Added, int After, int RepairsLeft) : DuelEvent;
public sealed record Braced(Side Side, int Amount, int Total) : DuelEvent;
public sealed record Repaired(Side Side, int Amount, int HpAfter, int RepairsLeft) : DuelEvent;
/// <summary>De regel klopte, maar er zijn geen herstellingen meer: de beurt is weg.</summary>
public sealed record RepairEmpty(Side Side) : DuelEvent;
/// <summary><paramref name="WasCharged"/>: al opgeladen, dus deze beurt deed niets extra.</summary>
public sealed record WoundUp(Side Side, bool WasCharged) : DuelEvent;
/// <summary>Schade met een kommagetal op een automaat die afrondt: <c>Math.Round</c>, bij .5 naar het dichtste even getal.</summary>
public sealed record ValueRounded(Side Target, double Before, int After) : DuelEvent;
/// <summary>Een klap op tekst trekt niets af: het getal wordt erachter geplakt.</summary>
public sealed record TextAppended(Side Target, string Before, string Added, string After) : DuelEvent;
/// <summary>De tekst werd te lang om te tonen: de automaat crasht.</summary>
public sealed record TextCrashed(Side Target, int Length, int Limit) : DuelEvent;
/// <summary>Een teller van het type <c>byte</c> liep over: van 255 naar 0.</summary>
public sealed record CounterOverflowed(Side Side, int Before, int After) : DuelEvent;
/// <summary>Omgegoten of geconverteerd naar een ander type. <paramref name="Method"/>: <c>cast</c> of <c>convert</c>.</summary>
public sealed record TypeChanged(Side Target, ValueKind From, ValueKind To, int Before, int After, bool Wrapped, string Method) : DuelEvent;
/// <summary>Hij was al dat type: de regel klopte, maar er verandert niets.</summary>
public sealed record TypeUnchanged(Side Target, ValueKind Kind) : DuelEvent;
/// <summary><c>Convert.ToByte</c> op een te groot getal: een <c>OverflowException</c>. Zijn volgende zet valt weg.</summary>
public sealed record ConversionCrashed(Side Target, int Value) : DuelEvent;
/// <summary>Deze zet viel weg, omdat een conversie crashte.</summary>
public sealed record MoveSkipped(Side Side) : DuelEvent;
/// <summary><c>int.Parse</c> lukte: de tekst van het doelwit is nu het getal <paramref name="Value"/>, een <c>int</c>.</summary>
public sealed record TextParsed(Side Target, string Text, int Value) : DuelEvent;
/// <summary>
/// <c>int.Parse</c> op tekst die geen geheel getal is (<c>"605.5"</c>): de lezer crasht, en zijn volgende zet valt weg.
/// In beeld heet dat crashen, niet <c>FormatException</c> (dat woord hoort bij H10).
/// </summary>
public sealed record ParseCrashed(Side Side, string Text) : DuelEvent;
public sealed record DuelEnded(Outcome Outcome, int Turn) : DuelEvent;
