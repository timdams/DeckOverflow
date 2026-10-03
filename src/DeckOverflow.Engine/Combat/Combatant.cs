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

    /// <summary>Max HP zoals de vijand ontworpen is, los van zijn huidige type. Groeit als een tekst groter blijkt bij het parsen.</summary>
    public double BaseMaxHp { get; internal set; } = setup.MaxHp;
    public double MaxHp => CastRules.MaxFor(Kind, BaseMaxHp);

    /// <summary>
    /// Alleen voor een <c>string</c>: zijn HP is tekst. Schade plakt eraan vast, en tekst sterft niet:
    /// eerst moet ze een getal worden.
    /// </summary>
    public string? Text { get; internal set; } = setup.Kind == ValueKind.String ? setup.Text ?? setup.Hp.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;

    public double Block { get; internal set; } = setup.Block;
    public bool IsEnemy { get; } = isEnemy;
    public bool IsDead => Kind != ValueKind.String && Hp <= 0;
}
