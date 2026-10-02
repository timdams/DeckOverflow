using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>Het spike-scenario uit het design doc.</summary>
public static class ByteGolemScenario
{
    /// <summary>Met deze seed zit Herstel in de eerste hand (zie de tests).</summary>
    public const ulong DefaultSeed = 255;

    public static CombatSetup Create() => new(
        Player: new CombatantSetup("Jij", ValueKind.Int, Hp: 50, MaxHp: 50),
        Enemy: new EnemySetup(
            Stats: new CombatantSetup("Byte-Golem", ValueKind.Byte, Hp: 250, MaxHp: byte.MaxValue),
            Attack: new Intent("15 / 2.0", 15 / 2.0),
            HealAfterAttack: 2),
        Deck:
        [
            CardCatalog.Slag, CardCatalog.Slag, CardCatalog.Slag, CardCatalog.Slag, CardCatalog.Slag,
            CardCatalog.Schild, CardCatalog.Schild, CardCatalog.Schild, CardCatalog.Schild,
            CardCatalog.Herstel
        ]);
}
