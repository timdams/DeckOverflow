using DeckOverflow.CardHall.Cards;
using DeckOverflow.Core.Values;

namespace DeckOverflow.CardHall.Combat;

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
/// <param name="ResetTextEvery">Om de zoveel beurten wordt hij opnieuw tekst, met <paramref name="ResetTemplate"/> (The Typesetter).</param>
/// <param name="ResetTemplate">String interpolatie: <c>{0}</c> wordt de schade die hij die beurt kreeg.</param>
/// <param name="YearPrefix">Een jaartal als tekst (Y2K): na elke beurt wordt zijn tekst <c>YearPrefix + jaar</c>, echte concatenatie.
/// Na <c>"19" + 99</c> komt <c>"19" + 100</c>, en dat is <c>"19100"</c>.</param>
/// <param name="Toggles">Een <c>bool</c> <c>isSolid</c> die elke treffer omdraait. Alleen als hij solid is, raakt de treffer (de Bool Ghost).</param>
/// <param name="OpenEvery">Zijn schild is alleen open bij elke zoveelste kaart van jouw beurt: <c>cards % OpenEvery == 0</c> (de Rhythm Turtle).</param>
/// <param name="StartYear">Het jaar zonder eeuw bij de start. Na beurt N is het <c>StartYear + N</c>.</param>
/// <param name="Shots">Een teller <c>shots</c> voor zijn intent, met deze beginwaarde (de Twin Shooters). Leeg: geen teller.</param>
/// <param name="ShotsPerAttack">Hoeveel <c>++</c> er in zijn aanval zitten: na de aanval staat <c>shots</c> zoveel hoger.</param>
/// <param name="Names">De naam waarmee hij elke beurt aangesproken wordt, één per beurt, in een kring (The Nameless).
/// Alleen als die naam geldig is en naar <paramref name="RealName"/> wijst, kan een kaart hem raken.</param>
/// <param name="RealName">De naam van zijn variabele. Hoofdletters tellen.</param>
/// <param name="ConstAttack">Een revisie maakte zijn aanval <c>const</c>: toekennen (Wrong Label, Remainder) compileert niet meer.</param>
public sealed record EnemySetup(
    CombatantSetup Stats,
    IReadOnlyList<Intent> Pattern,
    int HealAfterAttack = 0,
    double BlockAfterAttack = 0,
    bool RoundsIncoming = false,
    double GrowthAfterAttack = 0,
    IReadOnlyList<ValueKind>? TypeCycle = null,
    int CrashLength = 0,
    bool StopsHealingOnOverflow = false,
    int ResetTextEvery = 0,
    string? ResetTemplate = null,
    string? YearPrefix = null,
    int StartYear = 0,
    bool Toggles = false,
    int OpenEvery = 0,
    int? Shots = null,
    int ShotsPerAttack = 0,
    IReadOnlyList<string>? Names = null,
    string? RealName = null,
    bool ConstAttack = false);

/// <param name="Relics">Ids uit <see cref="Relics.RelicCatalog"/> die in dit gevecht meespelen.</param>
public sealed record CombatSetup(
    CombatantSetup Player,
    EnemySetup Enemy,
    IReadOnlyList<CardDefinition> Deck,
    int MaxEnergy = 3,
    int HandSize = 5,
    IReadOnlyList<string>? Relics = null);
