using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <param name="Key">Vaste sleutel voor de stage (sprite, geluid). Geen spelregel.</param>
public sealed record CombatantSetup(string Key, string Name, ValueKind Kind, double Hp, double MaxHp, double Block = 0);

/// <param name="BlockAfterAttack">Blok dat de vijand na zijn aanval opbouwt. Volgt de regels van zijn type.</param>
public sealed record EnemySetup(CombatantSetup Stats, Intent Attack, int HealAfterAttack = 0, double BlockAfterAttack = 0);

public sealed record CombatSetup(
    CombatantSetup Player,
    EnemySetup Enemy,
    IReadOnlyList<CardDefinition> Deck,
    int MaxEnergy = 3,
    int HandSize = 5);
