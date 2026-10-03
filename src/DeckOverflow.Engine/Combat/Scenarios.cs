using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>Losse gevechten buiten een run, voor tests en om één vijand snel te proberen.</summary>
public static class Scenarios
{
    public const ulong DefaultSeed = 255;
    public const string PlayerKey = "player";

    public static CombatantSetup Player(int hp = 50, int maxHp = 50, double block = 0) =>
        new(PlayerKey, ValueKind.Int, hp, maxHp, block);

    public static CombatSetup Create(string enemy) =>
        new(Player(), Bestiary.Create(enemy), CardCatalog.StarterDeck());
}
