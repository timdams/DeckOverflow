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
    public const string EffectivePower = "effective-power";
    /// <summary>De millenniumbug: zijn HP is een jaartal als tekst, <c>"19" + jaar</c>.</summary>
    public const string Y2K = "y2k";
    public const string Counter = "counter";
    /// <summary>Een tease: een robotje dat ontsnapte uit de Controlekamer, met een regel als intent.</summary>
    public const string Stray = "stray";

    // Act 2: de Drukkerij
    public const string TypeBlock = "type-block";
    public const string PaperGolem = "paper-golem";
    public const string Typesetter = "typesetter";

    // Act 2: de Gieterij
    public const string Ingot = "ingot";
    public const string Rounder = "rounder";
    public const string Index = "index";
    public const string Caster = "caster";
    public const string Label = "label";

    /// <summary>Alle elites, over de acts heen. Welke elite in welke act zit, staat in <see cref="Runs.Acts"/>.</summary>
    public static readonly IReadOnlyList<string> Elites = [Colossus, Golem, Counter, EffectivePower, Y2K, Index];

    public static readonly IReadOnlyList<string> Bosses = [Reckoner, Typesetter, Caster];

    public static readonly IReadOnlyList<string> All = [Slime, Knight, Ghost, Dripper, Jug, Colossus, Golem, Reckoner, Splitter, Counter, Stray, EffectivePower, Y2K, TypeBlock, PaperGolem, Typesetter, Ingot, Rounder, Index, Caster, Label];

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

        // Effective Power (iPhone, 2015): zijn HP is een bericht. Elke treffer plakt eraan vast,
        // en vanaf 32 tekens crasht het bericht. "2.5" plakt drie tekens, "6" maar één.
        EffectivePower => new(
            new CombatantSetup(EffectivePower, ValueKind.String, Hp: 1, MaxHp: 1, Text: "effective. Power"),
            [new("8 + 4", 8 + 4), new("30 / 2", 30 / 2), new("3 * 3", 3 * 3)],
            CrashLength: 32),

        // Y2K (1999): websites schreven het jaar als "19" + (jaar - 1900). Op 1 januari 2000 stond er 19100.
        // Zijn HP is dat jaartal als tekst, en elke beurt schrijft hij het opnieuw: wat je eraan plakte, is weg.
        // Je krijgt hem alleen in één beurt lang genoeg om te crashen, of je telt zijn letters. Na middernacht
        // is "19100" een teken langer, en dus makkelijker.
        Y2K => new(
            new CombatantSetup(Y2K, ValueKind.String, Hp: 1, MaxHp: 1, Text: "1997"),
            [new("4 * 3", 4 * 3), new("7 + 7", 7 + 7), new("2 * 9", 2 * 9)],
            CrashLength: 12,
            YearPrefix: "19",
            StartYear: 97),

        // The Counter (YouTube, 2014): de weergaventeller van Gangnam Style naderde int.MaxValue.
        // Hij telt elke beurt op; wie hem heelt of lang genoeg overleeft, ziet hem omklappen naar negatief.
        Counter => new(
            new CombatantSetup(Counter, ValueKind.Int, Hp: int.MaxValue - 30, MaxHp: int.MaxValue),
            [new("9 + 9", 9 + 9), new("25 / 2", 25 / 2), new("7 * 2", 7 * 2)],
            HealAfterAttack: 9),

        // De Stray Automaton, ontsnapt uit de Controlekamer (H5): zijn intent is geen getal maar een regel.
        // Wie blokt, krijgt het dubbel. Een eerste smaak van if, zonder het te benoemen.
        Stray => new(
            new CombatantSetup(Stray, ValueKind.Int, Hp: 26, MaxHp: 26),
            [Intent.Live("block > 0 ? 16 : 8", c => c.Block > 0 ? 16 : 8), new("5 + 5", 5 + 5)]),

        // ---------- Act 2: de Drukkerij ----------

        // Zijn HP is het teken '0': er staat een 0, maar het is 48. Een cijferteken is geen cijfer.
        TypeBlock => new(
            new CombatantSetup(TypeBlock, ValueKind.Char, Hp: '0', MaxHp: '0'),
            [new("3 + 3", 3 + 3), new("16 / 3", 16 / 3)]),

        // Valt aan met tekst die hij plakt en dan pas omzet: "1" + 2 is 12, 1 + 2 is 3
        PaperGolem => new(
            new CombatantSetup(PaperGolem, ValueKind.Int, Hp: 32, MaxHp: 32),
            [new("\"1\" + 2", int.Parse("1" + 2)), new("1 + 2", 1 + 2)]),

        // Zijn HP is een zin. Count Letters maakt er de Length van; om de drie beurten zet hij een nieuwe
        // zin met string interpolatie. Wie lang genoeg plakt, ziet de zin crashen (een tweede uitweg).
        Typesetter => new(
            new CombatantSetup(Typesetter, ValueKind.String, Hp: 1, MaxHp: 1, Text: "THE MANUAL IS ALWAYS RIGHT. FOLLOW EVERY STEP."),
            [new("6 + 6", 6 + 6), new("3 * 5", 3 * 5), new("40 / 3", 40 / 3)],
            CrashLength: 120,
            ResetTextEvery: 3,
            ResetTemplate: "YOU HIT ME FOR {0}. I WROTE IT DOWN, WORD FOR WORD."),

        // Deling van gehele getallen als verdediging: zonder blok 30, met 5 blok nog 5
        Splitter => new(
            new CombatantSetup(Splitter, ValueKind.Int, Hp: 26, MaxHp: 26),
            [Intent.Live("30 / (block + 1)", c => 30 / (c.Block + 1)), new("4 * 3", 4 * 3)]),

        // Het wondermoment: een byte die zoveel drinkt dat hij omklapt. Hij heelt meer dan
        // een starterdeck per beurt kan slaan, dus hij klapt altijd om, wat je ook doet.
        // Daarna is hij leeg en heelt hij niet meer, anders kwam hij elke beurt boven je schade uit.
        Jug => new(
            new CombatantSetup(Jug, ValueKind.Byte, Hp: 200, MaxHp: 255),
            [new("7 / 2", 7 / 2), new("2 * 2", 2 * 2)],
            HealAfterAttack: 40,
            StopsHealingOnOverflow: true),

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
