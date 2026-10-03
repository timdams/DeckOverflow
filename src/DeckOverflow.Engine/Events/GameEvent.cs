using System.Text.Json.Serialization;
using DeckOverflow.Engine.Maps;
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
[JsonDerivedType(typeof(ValueRounded), nameof(ValueRounded))]
[JsonDerivedType(typeof(ValueGrew), nameof(ValueGrew))]
[JsonDerivedType(typeof(ConversionCrashed), nameof(ConversionCrashed))]
[JsonDerivedType(typeof(AttackSkipped), nameof(AttackSkipped))]
[JsonDerivedType(typeof(IntentAssigned), nameof(IntentAssigned))]
[JsonDerivedType(typeof(ModifierQueued), nameof(ModifierQueued))]
[JsonDerivedType(typeof(ModifiersApplied), nameof(ModifiersApplied))]
[JsonDerivedType(typeof(RelicTriggered), nameof(RelicTriggered))]
[JsonDerivedType(typeof(NodeEntered), nameof(NodeEntered))]
[JsonDerivedType(typeof(GoldChanged), nameof(GoldChanged))]
[JsonDerivedType(typeof(RunHpChanged), nameof(RunHpChanged))]
[JsonDerivedType(typeof(CardAdded), nameof(CardAdded))]
[JsonDerivedType(typeof(CardRemoved), nameof(CardRemoved))]
[JsonDerivedType(typeof(CardTransformed), nameof(CardTransformed))]
[JsonDerivedType(typeof(RelicGained), nameof(RelicGained))]
[JsonDerivedType(typeof(RunRejected), nameof(RunRejected))]
[JsonDerivedType(typeof(RunEnded), nameof(RunEnded))]
[JsonDerivedType(typeof(ActCompleted), nameof(ActCompleted))]
[JsonDerivedType(typeof(ActStarted), nameof(ActStarted))]
public abstract record GameEvent
{
    public int Seq { get; init; }
}

/// <summary>Welke waarde een decimaal verloor. Zo toont de stage afgekapte schade anders dan een afgekapt schild.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ValueSubject>))]
public enum ValueSubject { Damage, Block, Hp }

// Gevecht: kaarten, schade, blok en types. HP en blok zijn double, omdat een double-doelwit decimalen houdt.
public sealed record CardPlayed(string CardId, int SourceId, int TargetId) : GameEvent;
public sealed record DamageDealt(int TargetId, double Amount, double HpBefore, double HpAfter) : GameEvent;
public sealed record ValueTruncated(int TargetId, double Before, int After, double Lost, ValueSubject Subject) : GameEvent;
public sealed record ValueOverflowed(int TargetId, int Before, int Added, int After, int Max) : GameEvent;
public sealed record BlockGained(int TargetId, double Amount, double Total) : GameEvent;
public sealed record Healed(int TargetId, double Amount, double HpAfter) : GameEvent;
/// <param name="Value">Leeg als het totaal verborgen blijft (Rekenmeester).</param>
public sealed record IntentRevealed(int EnemyId, string Expression, double? Value) : GameEvent;
public sealed record CombatantDied(int TargetId) : GameEvent;
public sealed record TurnEnded(int Turn) : GameEvent;
public sealed record BlockAbsorbed(int TargetId, double Absorbed, double Remaining) : GameEvent;
public sealed record BlockExpired(int TargetId, double Amount) : GameEvent;
public sealed record AttackLaunched(int SourceId, int TargetId, string Expression, double Value) : GameEvent;
public sealed record TurnStarted(int Turn, int Energy) : GameEvent;
/// <param name="Reason">Sleutel in <c>en.json</c>, bv. <c>reject.no-energy</c>.</param>
public sealed record PlayRejected(int HandIndex, string Reason) : GameEvent;
public sealed record CombatEnded(bool Won) : GameEvent;

// Omgieten
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

/// <summary><c>Math.Round</c> of <c>Convert</c> rondde af: 2.5 wordt 2, 3.5 wordt 4.</summary>
public sealed record ValueRounded(int TargetId, double Before, double After, ValueSubject Subject) : GameEvent;

/// <summary>Groei: <paramref name="Raw"/> is <c>HP * factor</c>, <paramref name="After"/> wat het type ervan overhoudt.</summary>
public sealed record ValueGrew(int TargetId, double Before, double Factor, double Raw, double After) : GameEvent;

/// <summary><c>Convert</c> paste niet: een <c>OverflowException</c>. Het doelwit slaat zijn volgende aanval over.</summary>
public sealed record ConversionCrashed(int TargetId, ValueKind To, double Value) : GameEvent;

/// <summary>De vijand slaat zijn aanval over, bv. na een crash.</summary>
public sealed record AttackSkipped(int EnemyId) : GameEvent;

// Gevecht: verloop en relics

/// <summary>Toekenning: de aanval van de vijand is overschreven, wat er ook stond.</summary>
public sealed record IntentAssigned(int EnemyId, string ExpressionBefore, double Value) : GameEvent;

/// <summary>Een modifier wacht op je volgende kaart. <paramref name="Pending"/> is alles wat wacht, bv. "+3 ×2".</summary>
public sealed record ModifierQueued(string Label, string Pending) : GameEvent;

/// <summary>De wachtende modifiers zijn op een kaart toegepast, in volgorde: "(6 + 3) × 2" wordt 18.</summary>
public sealed record ModifiersApplied(string CardId, double Before, double After, string Expression) : GameEvent;

/// <summary>Een relic deed iets. Wat precies, volgt als gewone events (schade, kost).</summary>
public sealed record RelicTriggered(string RelicId) : GameEvent;

// Run. De stage negeert deze; de shell toont ze.

public sealed record NodeEntered(int NodeId, NodeKind Kind) : GameEvent;
public sealed record GoldChanged(int Amount, int Total) : GameEvent;
public sealed record RunHpChanged(int Amount, int Hp, int MaxHp) : GameEvent;
public sealed record CardAdded(string CardId) : GameEvent;
public sealed record CardRemoved(string CardId) : GameEvent;

/// <summary>Een kaart werd verbeterd of omgegoten.</summary>
public sealed record CardTransformed(string FromId, string ToId) : GameEvent;
public sealed record RelicGained(string RelicId) : GameEvent;

/// <summary>Een keuze buiten het gevecht die niet kan. <paramref name="Reason"/> is een sleutel in <c>en.json</c>.</summary>
public sealed record RunRejected(string Reason) : GameEvent;
public sealed record RunEnded(bool Won) : GameEvent;
/// <summary>De baas van deze act is verslagen; de volgende act wacht.</summary>
public sealed record ActCompleted(int Act) : GameEvent;
/// <summary>Een nieuwe act met een nieuwe map. Wordt ook een startpunt voor volgende runs.</summary>
public sealed record ActStarted(int Act) : GameEvent;
