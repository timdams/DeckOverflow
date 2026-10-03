using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>
/// De vijanden van alle acts. Elke waarde in een intent rekent C# zelf uit.
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
    public const string Splitter = "splitter";

    // Act 2: de Gieterij
    public const string Ingot = "ingot";
    public const string Rounder = "rounder";
    public const string Index = "index";
    public const string Caster = "caster";
    public const string Label = "label";

    /// <summary>Alle elites, over de acts heen. Welke elite in welke act zit, staat in <see cref="Runs.Acts"/>.</summary>
    public static readonly IReadOnlyList<string> Elites = [Colossus, Golem, Index];

    public static readonly IReadOnlyList<string> Bosses = [Reckoner, Caster];

    public static readonly IReadOnlyList<string> All = [Slime, Knight, Ghost, Dripper, Jug, Colossus, Golem, Reckoner, Splitter, Ingot, Rounder, Index, Caster, Label];

    public static bool Exists(string? key) => key is not null && All.Contains(key);

    public static EnemySetup Create(string key) => key switch
    {
        Slime => new(
            new CombatantSetup(Slime, ValueKind.Int, Hp: 20, MaxHp: 20),
            [new("2 * 3", 2 * 3), new("4 + 4", 4 + 4)]),

        // Een int met een int-schild: Vlottende kaarten verliezen hier hun decimalen.
        // Zijn tweede aanval wordt kleiner naarmate je meer kaarten speelt.
        Knight => new(
            new CombatantSetup(Knight, ValueKind.Int, Hp: 30, MaxHp: 30),
            [new("5 + 3", 5 + 3), Intent.Live("24 / (cards + 1)", c => 24 / (c.Cards + 1))],
            BlockAfterAttack: 5.5),

        // Een double met een decimaal schild. Naar int omgegoten verliest hij de restjes.
        Ghost => new(
            new CombatantSetup(Ghost, ValueKind.Double, Hp: 24.5, MaxHp: 24.5, Block: 12.5),
            [new("9 / 2.0", 9 / 2.0), new("13 / 2.0", 13 / 2.0)],
            BlockAfterAttack: 12.5),

        // Een double zonder schild die halve schade uitdeelt: jouw int-HP kapt af.
        // Zijn tweede aanval straft energie die je overhoudt.
        Dripper => new(
            new CombatantSetup(Dripper, ValueKind.Double, Hp: 19.5, MaxHp: 19.5),
            [new("5 * 1.5", 5 * 1.5), Intent.Live("energy * 4 + 2.5", c => c.Energy * 4 + 2.5)]),

        // Deling van gehele getallen als verdediging: zonder blok 30, met 5 blok nog 5
        Splitter => new(
            new CombatantSetup(Splitter, ValueKind.Int, Hp: 26, MaxHp: 26),
            [Intent.Live("30 / (block + 1)", c => 30 / (c.Block + 1)), new("4 * 3", 4 * 3)]),

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

        // ---------- Act 2: de Gieterij ----------

        // Een eenvoudig gevecht, een adempauze tussen de puzzels
        Ingot => new(
            new CombatantSetup(Ingot, ValueKind.Int, Hp: 34, MaxHp: 34),
            [new("6 + 6", 6 + 6), new("3 * 3", 3 * 3)],
            BlockAfterAttack: 6),

        // Zijn HP is tekst: elke treffer plakt eraan vast. Eerst parsen, dan pas raken.
        Label => new(
            new CombatantSetup(Label, ValueKind.String, Hp: 40, MaxHp: 40, Text: "40"),
            [new("4 + 4", 4 + 4), new("13 / 2", 13 / 2)]),

        // Rondt elke treffer af in plaats van af te kappen: 1.5 wordt 2, maar 2.5 ook.
        // Naar int omgegoten kapt hij gewoon af.
        Rounder => new(
            new CombatantSetup(Rounder, ValueKind.Double, Hp: 40, MaxHp: 40),
            [new("5 * 1.5", 5 * 1.5), new("21 / 2", 21 / 2)],
            RoundsIncoming: true),

        // The Index (Vancouver, 1982): groeit elke beurt, maar kapt af in plaats van af te ronden.
        // Onder 20 HP eet het afkappen de groei op: (int)(19 * 1.05) is 19.
        Index => new(
            new CombatantSetup(Index, ValueKind.Int, Hp: 90, MaxHp: 200),
            [new("4 + 5", 4 + 5), new("23 / 2", 23 / 2), new("7 * 1", 7 * 1)],
            GrowthAfterAttack: 1.05),

        // Giet zichzelf elke beurt om. Boven 255 HP klapt hij om als hij een byte wordt.
        Caster => new(
            new CombatantSetup(Caster, ValueKind.Int, Hp: 300, MaxHp: 300),
            [new("12 + 3 * 2", 12 + 3 * 2), new("50 / 4", 50 / 4), new("7.5 * 2", 7.5 * 2)],
            TypeCycle: [ValueKind.Int, ValueKind.Double, ValueKind.Byte]),

        _ => throw new ArgumentException($"Onbekende vijand: {key}", nameof(key))
    };
}
