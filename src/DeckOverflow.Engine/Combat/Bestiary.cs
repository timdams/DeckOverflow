using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>
/// De vijanden van de act. Elke waarde in een intent rekent C# zelf uit.
/// Namen staan in <c>en.json</c> onder <c>enemy.&lt;key&gt;</c>. De getallen zijn eerste gokken.
/// </summary>
public static class Bestiary
{
    public const string Slime = "slime";
    public const string Knight = "knight";
    public const string Ghost = "ghost";
    public const string Dripper = "dripper";
    public const string Jug = "jug";
    public const string Colossus = "colossus";
    public const string Golem = "golem";
    public const string Reckoner = "reckoner";

    /// <summary>Gewone gevechten in de eerste twee rijen: om in te komen.</summary>
    public static readonly IReadOnlyList<string> EasyPool = [Slime, Knight];

    /// <summary>Gewone gevechten verderop.</summary>
    public static readonly IReadOnlyList<string> NormalPool = [Knight, Ghost, Dripper];

    public static readonly IReadOnlyList<string> ElitePool = [Colossus, Golem];

    public static readonly IReadOnlyList<string> All = [Slime, Knight, Ghost, Dripper, Jug, Colossus, Golem, Reckoner];

    public static bool Exists(string? key) => key is not null && All.Contains(key);

    public static EnemySetup Create(string key) => key switch
    {
        Slime => new(
            new CombatantSetup(Slime, ValueKind.Int, Hp: 20, MaxHp: 20),
            [new("2 * 3", 2 * 3), new("4 + 4", 4 + 4)]),

        // Een int met een int-schild: Vlottende kaarten verliezen hier hun decimalen
        Knight => new(
            new CombatantSetup(Knight, ValueKind.Int, Hp: 30, MaxHp: 30),
            [new("5 + 3", 5 + 3), new("10 / 3", 10 / 3)],
            BlockAfterAttack: 5.5),

        // Een double met een decimaal schild. Naar int omgegoten verliest hij de restjes.
        Ghost => new(
            new CombatantSetup(Ghost, ValueKind.Double, Hp: 24.5, MaxHp: 24.5, Block: 12.5),
            [new("9 / 2.0", 9 / 2.0), new("13 / 2.0", 13 / 2.0)],
            BlockAfterAttack: 12.5),

        // Een double zonder schild die halve schade uitdeelt: jouw int-HP kapt af
        Dripper => new(
            new CombatantSetup(Dripper, ValueKind.Double, Hp: 19.5, MaxHp: 19.5),
            [new("5 * 1.5", 5 * 1.5), new("2.5 + 2.5", 2.5 + 2.5)]),

        // Het wondermoment: een byte die zoveel drinkt dat hij omklapt. Hij heelt meer dan
        // een starterdeck per beurt kan slaan, dus hij klapt altijd om, wat je ook doet.
        Jug => new(
            new CombatantSetup(Jug, ValueKind.Byte, Hp: 200, MaxHp: 255),
            [new("7 / 2", 7 / 2), new("2 * 2", 2 * 2)],
            HealAfterAttack: 40),

        // Te groot om plat te slaan. (byte)506 is 250, en dan werkt Herstel als bij de golem.
        Colossus => new(
            new CombatantSetup(Colossus, ValueKind.Int, Hp: 506, MaxHp: 506),
            [new("12 + 3 * 2", 12 + 3 * 2)]),

        // Level 256 (vroeger de Byte-Golem): heelt zichzelf tot hij omklapt
        Golem => new(
            new CombatantSetup(Golem, ValueKind.Byte, Hp: 250, MaxHp: 255),
            [new("15 / 2.0", 15 / 2.0)],
            HealAfterAttack: 2),

        // De baas toont zijn totaal niet: hier is rekenen bewust de kern
        Reckoner => new(
            new CombatantSetup(Reckoner, ValueKind.Int, Hp: 90, MaxHp: 90),
            [
                new("3 + 2 * 4", 3 + 2 * 4, Hidden: true),
                new("(3 + 2) * 4", (3 + 2) * 4, Hidden: true),
                new("17 / 5 + 17 % 5", 17 / 5 + 17 % 5, Hidden: true),
                new("2 * 3 + 4 * 2", 2 * 3 + 4 * 2, Hidden: true),
            ]),

        _ => throw new ArgumentException($"Onbekende vijand: {key}", nameof(key))
    };
}
