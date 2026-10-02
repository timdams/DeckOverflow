using System.Text.Json.Serialization;

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
public abstract record GameEvent
{
    public int Seq { get; init; }
}

// Uit het design doc
public sealed record CardPlayed(string CardId, int SourceId, int TargetId) : GameEvent;
public sealed record DamageDealt(int TargetId, int Amount, int HpBefore, int HpAfter) : GameEvent;
public sealed record ValueTruncated(int TargetId, double Before, int After, double Lost) : GameEvent;
public sealed record ValueOverflowed(int TargetId, int Before, int Added, int After, int Max) : GameEvent;
public sealed record BlockGained(int TargetId, int Amount, int Total) : GameEvent;
public sealed record Healed(int TargetId, int Amount, int HpAfter) : GameEvent;
public sealed record IntentRevealed(int EnemyId, string Expression, double Value) : GameEvent;
public sealed record CombatantDied(int TargetId) : GameEvent;
public sealed record TurnEnded(int Turn) : GameEvent;

// Toegevoegd tijdens de spike
public sealed record BlockAbsorbed(int TargetId, int Absorbed, int Remaining) : GameEvent;
public sealed record BlockExpired(int TargetId, int Amount) : GameEvent;
public sealed record AttackLaunched(int SourceId, int TargetId, string Expression, double Value) : GameEvent;
public sealed record TurnStarted(int Turn, int Energy) : GameEvent;
public sealed record PlayRejected(int HandIndex, string Reason) : GameEvent;
public sealed record CombatEnded(bool Won) : GameEvent;
