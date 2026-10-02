using System.Text.Json.Serialization;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Events;

/// <summary>
/// Het eventcontract met de stage. Klein en plat: ids en getallen.
/// De stage mag onbekende types negeren.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CardPlayed), nameof(CardPlayed))]
[JsonDerivedType(typeof(DamageDealt), nameof(DamageDealt))]
[JsonDerivedType(typeof(ValueTruncated), nameof(ValueTruncated))]
[JsonDerivedType(typeof(ValueOverflowed), nameof(ValueOverflowed))]
[JsonDerivedType(typeof(BlockGained), nameof(BlockGained))]
[JsonDerivedType(typeof(BlockAbsorbed), nameof(BlockAbsorbed))]
[JsonDerivedType(typeof(BlockExpired), nameof(BlockExpired))]
[JsonDerivedType(typeof(Healed), nameof(Healed))]
[JsonDerivedType(typeof(IntentRevealed), nameof(IntentRevealed))]
[JsonDerivedType(typeof(AttackLaunched), nameof(AttackLaunched))]
[JsonDerivedType(typeof(CombatantDied), nameof(CombatantDied))]
[JsonDerivedType(typeof(TurnEnded), nameof(TurnEnded))]
[JsonDerivedType(typeof(TurnStarted), nameof(TurnStarted))]
[JsonDerivedType(typeof(PlayRejected), nameof(PlayRejected))]
[JsonDerivedType(typeof(CombatEnded), nameof(CombatEnded))]
[JsonDerivedType(typeof(TypeChanged), nameof(TypeChanged))]
public abstract record GameEvent
{
    public int Seq { get; init; }
}

/// <summary>Welke waarde een decimaal verloor. Zo toont de stage afgekapte schade anders dan een afgekapt schild.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ValueSubject>))]
public enum ValueSubject { Damage, Block, Hp }

// Uit spike 1. HP en blok zijn nu double, omdat een double-doelwit decimalen houdt.
public sealed record CardPlayed(string CardId, int SourceId, int TargetId) : GameEvent;
public sealed record DamageDealt(int TargetId, double Amount, double HpBefore, double HpAfter) : GameEvent;
public sealed record ValueTruncated(int TargetId, double Before, int After, double Lost, ValueSubject Subject) : GameEvent;
public sealed record ValueOverflowed(int TargetId, int Before, int Added, int After, int Max) : GameEvent;
public sealed record BlockGained(int TargetId, double Amount, double Total) : GameEvent;
public sealed record Healed(int TargetId, double Amount, double HpAfter) : GameEvent;
public sealed record IntentRevealed(int EnemyId, string Expression, double Value) : GameEvent;
public sealed record CombatantDied(int TargetId) : GameEvent;
public sealed record TurnEnded(int Turn) : GameEvent;
public sealed record BlockAbsorbed(int TargetId, double Absorbed, double Remaining) : GameEvent;
public sealed record BlockExpired(int TargetId, double Amount) : GameEvent;
public sealed record AttackLaunched(int SourceId, int TargetId, string Expression, double Value) : GameEvent;
public sealed record TurnStarted(int Turn, int Energy) : GameEvent;
public sealed record PlayRejected(int HandIndex, string Reason) : GameEvent;
public sealed record CombatEnded(bool Won) : GameEvent;

// Nieuw in spike 2
/// <summary>
/// Omgieten. Een verloren decimaal komt eerst als <see cref="ValueTruncated"/>;
/// <paramref name="Wrapped"/> zegt of de HP omklapte omdat ze niet in het nieuwe type paste.
/// </summary>
public sealed record TypeChanged(
    int TargetId,
    ValueKind From,
    ValueKind To,
    double HpBefore,
    double HpAfter,
    double MaxHpAfter,
    double BlockAfter,
    bool Wrapped) : GameEvent;
