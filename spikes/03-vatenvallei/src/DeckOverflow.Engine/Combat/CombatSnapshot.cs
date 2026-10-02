using System.Text.Json.Serialization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

[JsonConverter(typeof(JsonStringEnumConverter<CombatOutcome>))]
public enum CombatOutcome { Ongoing, Won, Lost }

/// <summary>Alleen-lezen beeld voor shell en stage. Na elke play is dit de waarheid.</summary>
/// <param name="Modifiers">Wat op je volgende kaart wacht, in volgorde, bv. "+3", "×2".</param>
public sealed record CombatSnapshot(
    int Turn,
    int Energy,
    int MaxEnergy,
    IReadOnlyList<CardView> Hand,
    int DrawPile,
    int DiscardPile,
    IReadOnlyList<CombatantView> Combatants,
    CombatOutcome Outcome,
    IReadOnlyList<string> Modifiers,
    IReadOnlyList<RelicView> Relics);

/// <param name="Cost">Wat de kaart nu kost, relics inbegrepen.</param>
public sealed record CardView(
    string Id, string Name, int Cost, string Text, TargetMode Target, ValueKind? Kind, ValueKind? CastTo, bool Playable);

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

/// <param name="Value">Leeg als het totaal verborgen blijft.</param>
public sealed record IntentView(string Expression, double? Value);

/// <param name="Counter">Voortgang voor relics die tellen, bv. "1.5/3". Leeg voor de rest.</param>
public sealed record RelicView(string Id, string Name, string? Counter);
