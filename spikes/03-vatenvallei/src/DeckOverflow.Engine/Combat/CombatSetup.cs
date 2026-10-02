using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <param name="Key">Vaste sleutel voor de stage (sprite, geluid). Geen spelregel.</param>
public sealed record CombatantSetup(string Key, string Name, ValueKind Kind, double Hp, double MaxHp, double Block = 0);

/// <param name="Pattern">Aanvallen in volgorde, één per beurt. Na de laatste begint hij opnieuw.</param>
/// <param name="BlockAfterAttack">Blok dat de vijand na zijn aanval opbouwt. Volgt de regels van zijn type.</param>
public sealed record EnemySetup(CombatantSetup Stats, IReadOnlyList<Intent> Pattern, int HealAfterAttack = 0, double BlockAfterAttack = 0);

/// <param name="Relics">Ids uit <see cref="Relics.RelicCatalog"/> die in dit gevecht meespelen.</param>
public sealed record CombatSetup(
    CombatantSetup Player,
    EnemySetup Enemy,
    IReadOnlyList<CardDefinition> Deck,
    int MaxEnergy = 3,
    int HandSize = 5,
    IReadOnlyList<string>? Relics = null);
