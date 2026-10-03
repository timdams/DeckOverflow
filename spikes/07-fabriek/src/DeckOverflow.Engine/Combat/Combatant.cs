using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>Status van een speler of vijand. De regels zitten in <see cref="Combat"/>.</summary>
public sealed class Combatant(int id, CombatantSetup setup, bool isEnemy)
{
    public int Id { get; } = id;
    public string Key { get; } = setup.Key;

    /// <summary>Het C#-type van HP en blok. Bepaalt welke regels gelden, en verandert bij Omgieten.</summary>
    public ValueKind Kind { get; internal set; } = setup.Kind;

    /// <summary>
    /// Altijd als double bewaard. Voor <c>int</c> en <c>byte</c> zorgen de regels
    /// dat er nooit iets na de komma staat.
    /// </summary>
    public double Hp { get; internal set; } = setup.Hp;

    /// <summary>Max HP zoals de vijand ontworpen is, los van zijn huidige type.</summary>
    public double BaseMaxHp { get; } = setup.MaxHp;
    public double MaxHp => CastRules.MaxFor(Kind, BaseMaxHp);

    public double Block { get; internal set; } = setup.Block;
    public bool IsEnemy { get; } = isEnemy;
    public bool IsDead => Hp <= 0;
}
