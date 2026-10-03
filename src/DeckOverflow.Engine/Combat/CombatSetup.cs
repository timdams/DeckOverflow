using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <param name="Key">Vaste sleutel voor naam (<c>enemy.&lt;key&gt;</c> in <c>en.json</c>), sprite en geluid. Geen spelregel.</param>
/// <param name="Text">Alleen voor een <c>string</c>: de tekst die zijn HP is, bv. <c>"40"</c>.</param>
public sealed record CombatantSetup(string Key, ValueKind Kind, double Hp, double MaxHp, double Block = 0, string? Text = null);

/// <param name="Pattern">Aanvallen in volgorde, één per beurt. Na de laatste begint hij opnieuw.</param>
/// <param name="StopsHealingOnOverflow">Na zijn eerste overflow heelt hij niet meer (de Bottomless Jug is leeg).</param>
/// <param name="BlockAfterAttack">Blok dat de vijand na zijn aanval opbouwt. Volgt de regels van zijn type.</param>
/// <param name="RoundsIncoming">Zolang hij een <c>double</c> is, rondt hij elke treffer af met <c>Math.Round</c> (bankiersafronding).</param>
/// <param name="GrowthAfterAttack">Na zijn aanval wordt zijn HP <c>HP * factor</c>, volgens de regels van zijn type. 0 is geen groei.</param>
/// <param name="TypeCycle">Na zijn aanval giet hij zichzelf om naar het volgende type in deze lijst.</param>
/// <param name="CrashLength">Alleen voor een <c>string</c>: vanaf deze lengte crasht zijn tekst, en valt hij om (Effective Power).</param>
public sealed record EnemySetup(
    CombatantSetup Stats,
    IReadOnlyList<Intent> Pattern,
    int HealAfterAttack = 0,
    double BlockAfterAttack = 0,
    bool RoundsIncoming = false,
    double GrowthAfterAttack = 0,
    IReadOnlyList<ValueKind>? TypeCycle = null,
    int CrashLength = 0,
    bool StopsHealingOnOverflow = false);

/// <param name="Relics">Ids uit <see cref="Relics.RelicCatalog"/> die in dit gevecht meespelen.</param>
public sealed record CombatSetup(
    CombatantSetup Player,
    EnemySetup Enemy,
    IReadOnlyList<CardDefinition> Deck,
    int MaxEnergy = 3,
    int HandSize = 5,
    IReadOnlyList<string>? Relics = null);
