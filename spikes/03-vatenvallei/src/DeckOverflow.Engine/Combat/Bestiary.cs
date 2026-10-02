using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>
/// De vijanden van de Vatenvallei. Elke waarde in een intent rekent C# zelf uit.
/// De getallen zijn eerste gokken en worden in de playtest afgesteld.
/// </summary>
public static class Bestiary
{
    public const string Slijm = "slijm";
    public const string Ridder = "ridder";
    public const string Geest = "geest";
    public const string Druppel = "druppel";
    public const string Kolos = "kolos";
    public const string Golem = "golem";
    public const string Rekenmeester = "rekenmeester";

    /// <summary>Gewone gevechten in de eerste twee rijen: om in te komen.</summary>
    public static readonly IReadOnlyList<string> EasyPool = [Slijm, Ridder];

    /// <summary>Gewone gevechten verderop.</summary>
    public static readonly IReadOnlyList<string> NormalPool = [Ridder, Geest, Druppel];

    public static readonly IReadOnlyList<string> ElitePool = [Kolos, Golem];

    public static readonly IReadOnlyList<string> All = [Slijm, Ridder, Geest, Druppel, Kolos, Golem, Rekenmeester];

    public static bool Exists(string? key) => key is not null && All.Contains(key);

    public static EnemySetup Create(string key) => key switch
    {
        Slijm => new(
            new CombatantSetup(Slijm, "Slijmklodder", ValueKind.Int, Hp: 20, MaxHp: 20),
            [new("2 * 3", 2 * 3), new("4 + 4", 4 + 4)]),

        // Een int met een int-schild: Vlottende kaarten verliezen hier hun decimalen
        Ridder => new(
            new CombatantSetup(Ridder, "Tinnen Ridder", ValueKind.Int, Hp: 30, MaxHp: 30),
            [new("5 + 3", 5 + 3), new("10 / 3", 10 / 3)],
            BlockAfterAttack: 5.5),

        // Een double met een decimaal schild. Naar int omgegoten verliest hij de restjes.
        Geest => new(
            new CombatantSetup(Geest, "Vlottende Geest", ValueKind.Double, Hp: 24.5, MaxHp: 24.5, Block: 12.5),
            [new("9 / 2.0", 9 / 2.0), new("13 / 2.0", 13 / 2.0)],
            BlockAfterAttack: 12.5),

        // Een double zonder schild die halve schade uitdeelt: jouw int-HP kapt af
        Druppel => new(
            new CombatantSetup(Druppel, "Druppelaar", ValueKind.Double, Hp: 19.5, MaxHp: 19.5),
            [new("5 * 1.5", 5 * 1.5), new("2.5 + 2.5", 2.5 + 2.5)]),

        // Te groot om plat te slaan. (byte)506 is 250, en dan werkt Herstel als bij de golem.
        Kolos => new(
            new CombatantSetup(Kolos, "Tinnen Kolos", ValueKind.Int, Hp: 506, MaxHp: 506),
            [new("12 + 3 * 2", 12 + 3 * 2)]),

        // De Byte-Golem uit spike 1: heelt zichzelf tot hij omklapt
        Golem => new(
            new CombatantSetup(Golem, "Byte-Golem", ValueKind.Byte, Hp: 250, MaxHp: 255),
            [new("15 / 2.0", 15 / 2.0)],
            HealAfterAttack: 2),

        // De baas toont zijn totaal niet: hier is rekenen bewust de kern
        Rekenmeester => new(
            new CombatantSetup(Rekenmeester, "De Rekenmeester", ValueKind.Int, Hp: 90, MaxHp: 90),
            [
                new("3 + 2 * 4", 3 + 2 * 4, Hidden: true),
                new("(3 + 2) * 4", (3 + 2) * 4, Hidden: true),
                new("17 / 5 + 17 % 5", 17 / 5 + 17 % 5, Hidden: true),
                new("2 * 3 + 4 * 2", 2 * 3 + 4 * 2, Hidden: true),
            ]),

        _ => throw new ArgumentException($"Onbekende vijand: {key}", nameof(key))
    };
}
