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

public sealed record CardView(string Id, string Name, int Cost, string Text, TargetMode Target, bool Playable);

public sealed record CombatantView(
    int Id,
    string Name,
    ValueKind Kind,
    int Hp,
    int MaxHp,
    int Block,
    bool IsEnemy,
    IntentView? Intent);

public sealed record IntentView(string Expression, double Value);
