using System.Text.Json.Serialization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Text;
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
    string Id, int Cost, TextRef Text, TargetMode Target, ValueKind? Kind, ValueKind? CastTo, bool Playable);

/// <param name="Text">Alleen voor een <c>string</c>: de tekst die zijn HP is.</param>
/// <param name="TextLimit">Alleen als zijn tekst kan crashen: de lengte waarop dat gebeurt.</param>
public sealed record CombatantView(
    int Id,
    string Key,
    ValueKind Kind,
    double Hp,
    double MaxHp,
    double Block,
    bool IsEnemy,
    IntentView? Intent,
    string? Text = null,
    int? TextLimit = null,
    string? Rule = null);

/// <param name="Value">Leeg als het totaal verborgen blijft. Bij een bewuste intent: wat het nu zou zijn.</param>
/// <param name="Filled">Alleen bij een bewuste intent: de expressie met jouw getallen ingevuld, bv. <c>30 / (5 + 1)</c>.</param>
public sealed record IntentView(string Expression, double? Value, string? Filled = null);

/// <param name="Counter">Voortgang voor relics die tellen, bv. "1.5/3". Leeg voor de rest.</param>
public sealed record RelicView(string Id, string? Counter);
