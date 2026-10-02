using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>Status van een speler of vijand. De regels zitten in <see cref="Combat"/>.</summary>
public sealed class Combatant(int id, string name, ValueKind kind, int hp, int maxHp, bool isEnemy)
{
    public int Id { get; } = id;
    public string Name { get; } = name;

    /// <summary>Het C#-type van de HP. Bepaalt welke regels gelden.</summary>
    public ValueKind Kind { get; } = kind;

    public int Hp { get; internal set; } = hp;
    public int MaxHp { get; } = maxHp;
    public int Block { get; internal set; }
    public bool IsEnemy { get; } = isEnemy;
    public bool IsDead => Hp <= 0;
}
