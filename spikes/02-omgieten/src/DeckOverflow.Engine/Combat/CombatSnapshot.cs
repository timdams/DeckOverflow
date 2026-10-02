using System.Text.Json.Serialization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

[JsonConverter(typeof(JsonStringEnumConverter<CombatOutcome>))]
public enum CombatOutcome { Ongoing, Won, Lost }

/// <summary>Alleen-lezen beeld voor shell en stage. Na elke play is dit de waarheid.</summary>
public sealed record CombatSnapshot(
    int Turn,
    int Energy,
    int MaxEnergy,
    IReadOnlyList<CardView> Hand,
    int DrawPile,
    int DiscardPile,
    IReadOnlyList<CombatantView> Combatants,
    CombatOutcome Outcome);

public sealed record CardView(string Id, string Name, int Cost, string Text, TargetMode Target, ValueKind? Kind, bool Playable);

public sealed record CombatantView(
    int Id,
    string Key,
    string Name,
    ValueKind Kind,
    double Hp,
    double MaxHp,
    double Block,
    bool IsEnemy,
    IntentView? Intent);

public sealed record IntentView(string Expression, double Value);
