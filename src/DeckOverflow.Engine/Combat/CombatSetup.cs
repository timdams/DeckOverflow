using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

public sealed record CombatantSetup(string Name, ValueKind Kind, int Hp, int MaxHp);

public sealed record EnemySetup(CombatantSetup Stats, Intent Attack, int HealAfterAttack);

public sealed record CombatSetup(
    CombatantSetup Player,
    EnemySetup Enemy,
    IReadOnlyList<CardDefinition> Deck,
    int MaxEnergy = 3,
    int HandSize = 5);
