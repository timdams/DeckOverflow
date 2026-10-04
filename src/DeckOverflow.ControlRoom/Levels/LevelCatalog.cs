using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.Core.Values;

namespace DeckOverflow.ControlRoom.Levels;

/// <summary>
/// Eén gevecht in de Controlekamer: de vijand met zijn regels, en wat de speler mag gebruiken.
/// De woordenschat groeit per gevecht, zodat elke nieuwe check, zet of operator een eigen moment krijgt.
/// </summary>
/// <param name="Slots">Hoeveel regels de speler mag opstellen.</param>
/// <param name="Joins">Hoe een regel een tweede voorwaarde mag krijgen: EN, OF, of (leeg) niet.</param>
/// <param name="AllowNot">Mag een voorwaarde omgedraaid worden (NIET)?</param>
/// <param name="StartRules">Waarmee het regelbord begint.</param>
/// <param name="Bug">Een elite is een echte bug: na een overwinning staat haar verhaal onder <c>bug.&lt;key&gt;</c>.</param>
/// <param name="Player">De automaat van de speler: meestal de gewone, soms een met een ander type of andere getallen.</param>
/// <param name="MyHpValues">De grenzen die je bij "mijn HP &lt;" kan kiezen. Leeg: 10 tot 30.</param>
/// <param name="FoeHpValues">De grenzen die je bij "vijand HP &lt;" kan kiezen. Leeg: 10 tot 30.</param>
public sealed record Level(
    string Key,
    BotSpec Enemy,
    IReadOnlyList<Rule> EnemyRules,
    int Slots,
    IReadOnlyList<Check> Checks,
    IReadOnlyList<Move> Moves,
    IReadOnlyList<Join> Joins,
    bool AllowNot,
    IReadOnlyList<Rule> StartRules,
    bool Bug = false,
    BotSpec? Player = null,
    IReadOnlyList<int>? MyHpValues = null,
    IReadOnlyList<int>? FoeHpValues = null)
{
    /// <summary>De automaat van de speler in dit gevecht.</summary>
    public BotSpec PlayerBot => Player ?? LevelCatalog.Player;

    /// <summary>De getallen die een speler in een check kan kiezen.</summary>
    public IReadOnlyList<int> ValuesFor(Check check) => check switch
    {
        Check.MyHpBelow => MyHpValues ?? [10, 15, 20, 25, 30],
        Check.FoeHpBelow => FoeHpValues ?? [10, 15, 20, 25, 30],
        Check.EveryNthTurn => [2, 3, 4],
        _ => [0]
    };
}

public static class LevelCatalog
{
    public static BotSpec Player { get; } = new("player", MaxHp: 40, WhackDamage: 6, BlockAmount: 8, RepairAmount: 14, Repairs: 2);

    private static Condition C(Check check, int value = 0, bool not = false) => new(check, value, not);

    private static readonly Check[] AllChecks =
        [Check.Always, Check.MyHpBelow, Check.FoeHpBelow, Check.IAmCharged, Check.FoeCharged, Check.FoeBlocking, Check.EveryNthTurn];

    private static readonly Move[] AllMoves = [Move.Whack, Move.PatchUp, Move.HoldFirmly, Move.WindUp];

    public static IReadOnlyList<Level> All { get; } =
    [
        // 1. Hij slaat altijd. Wie alleen terugslaat, verliest nipt: herstel op het juiste moment.
        new("stamper",
            new BotSpec("stamper", MaxHp: 40, WhackDamage: 7, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [Rule.Otherwise(Move.Whack)],
            Slots: 2,
            [Check.Always, Check.MyHpBelow],
            [Move.Whack, Move.PatchUp],
            Joins: [], AllowNot: false,
            [Rule.Otherwise(Move.Whack)]),

        // 2. Hij laadt op en slaat dan dubbel. Lees zijn regels: schild als hij opgeladen is, en zet die regel bovenaan.
        new("press",
            new BotSpec("press", MaxHp: 40, WhackDamage: 7, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.IAmCharged), Move.Whack), Rule.Otherwise(Move.WindUp)],
            Slots: 3,
            [Check.Always, Check.MyHpBelow, Check.FoeCharged],
            [Move.Whack, Move.PatchUp, Move.HoldFirmly],
            Joins: [], AllowNot: false,
            [Rule.Otherwise(Move.Whack)]),

        // 3. Elke derde beurt zet hij een dik schild. Slaan op een schild is verspild: laad dan op.
        new("metronome",
            new BotSpec("metronome", MaxHp: 45, WhackDamage: 6, BlockAmount: 20, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.EveryNthTurn, 3), Move.HoldFirmly), Rule.Otherwise(Move.Whack)],
            Slots: 3,
            [Check.Always, Check.MyHpBelow, Check.FoeCharged, Check.FoeBlocking, Check.EveryNthTurn],
            AllMoves,
            Joins: [], AllowNot: false,
            [Rule.Otherwise(Move.Whack)]),

        // 4. Hij herstelt onder 20. Zijn herstelregel staat bovenaan, ook als hij niets meer heeft om te herstellen.
        new("mender",
            new BotSpec("mender", MaxHp: 50, WhackDamage: 8, BlockAmount: 12, RepairAmount: 10, Repairs: 2),
            [new Rule(C(Check.MyHpBelow, 20), Move.PatchUp), new Rule(C(Check.FoeCharged), Move.HoldFirmly), Rule.Otherwise(Move.Whack)],
            Slots: 4,
            AllChecks,
            AllMoves,
            Joins: [Join.And], AllowNot: false,
            [Rule.Otherwise(Move.Whack)]),

        // 5. Elite: goto fail. Regel 3 is een kopie van regel 2 zonder voorwaarde, zoals de dubbele regel in Apples
        // SSL-code (2014). Alles eronder wordt nooit meer bekeken: hij blokt en herstelt nooit, wat zijn regels ook beloven.
        new("goto-fail",
            new BotSpec("goto-fail", MaxHp: 55, WhackDamage: 6, BlockAmount: 25, RepairAmount: 12, Repairs: 3),
            [
                new Rule(C(Check.IAmCharged), Move.Whack),
                new Rule(C(Check.FoeHpBelow, 20), Move.WindUp),
                Rule.Otherwise(Move.WindUp),
                new Rule(C(Check.FoeCharged), Move.HoldFirmly),
                new Rule(C(Check.MyHpBelow, 20), Move.PatchUp),
            ],
            Slots: 4,
            AllChecks,
            AllMoves,
            Joins: [Join.And], AllowNot: false,
            [Rule.Otherwise(Move.Whack)],
            Bug: true),

        // 6. OF: hij laadt op zodra jij oplaadt of een schild zet, en slaat dan dubbel. Twee redenen, één reactie.
        new("sentry",
            new BotSpec("sentry", MaxHp: 40, WhackDamage: 7, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.IAmCharged), Move.Whack), new Rule(C(Check.FoeCharged), Move.WindUp, C(Check.FoeBlocking), Join.Or), Rule.Otherwise(Move.Whack)],
            Slots: 4,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: false,
            [Rule.Otherwise(Move.Whack)]),

        // 7. NIET: hij slaat zolang jij geen schild hebt. Heb je er een, dan laadt hij op voor de volgende klap.
        new("contrarian",
            new BotSpec("contrarian", MaxHp: 40, WhackDamage: 7, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.IAmCharged), Move.Whack), new Rule(C(Check.FoeBlocking, not: true), Move.Whack), Rule.Otherwise(Move.WindUp)],
            Slots: 4,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)]),

        // 8. Elite: Knight Capital (2012). Bovenaan staat een oude regel die nooit meer bereikt werd ("Power Peg").
        // Een hergebruikte vlag maakt ze weer bereikbaar: ben jij opgeladen terwijl hij onder 20 staat, dan laadt
        // hij op, en daarna blijft zijn oude regel vuren. Hij doet niets anders meer.
        new("knight-capital",
            new BotSpec("knight-capital", MaxHp: 40, WhackDamage: 7, BlockAmount: 10, RepairAmount: 0, Repairs: 0),
            [
                new Rule(C(Check.IAmCharged), Move.WindUp),
                new Rule(C(Check.FoeCharged), Move.WindUp, C(Check.MyHpBelow, 20)),
                new Rule(C(Check.FoeCharged), Move.HoldFirmly),
                Rule.Otherwise(Move.Whack),
            ],
            Slots: 4,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Bug: true),

        // Terugblik: wat je in de Card Hall leerde (H2 tot H4), gedraagt zich hier precies zo.

        // 9. Afkappen. Jouw Mep is een kommagetal (2.5) op een int: elke klap wordt 2. Opgeladen is het 5.0,
        // en daar valt niets af. Wie snapt wat (int) doet, laadt op voor hij slaat.
        new("cutter",
            new BotSpec("cutter", MaxHp: 24, WhackDamage: 4, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [Rule.Otherwise(Move.Whack)],
            Slots: 3,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Player: Player with { WhackDamage = 2.5, BlockAmount = 4, RepairAmount = 8 }),

        // 10. Overflow. Deze keer is jouw HP een byte: 250 van de 255. Oplappen geeft +40, zonder maximum.
        // Lap je op boven 215, dan loop je over en sta je bijna op nul. Zonder oplappen haal je het niet.
        new("overload",
            new BotSpec("overload", MaxHp: 220, WhackDamage: 30, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [Rule.Otherwise(Move.Whack)],
            Slots: 3,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Player: new BotSpec("player", MaxHp: 250, WhackDamage: 24, BlockAmount: 12, RepairAmount: 40, Repairs: 4, ValueKind.Byte),
            MyHpValues: [100, 150, 200, 215, 230, 240],
            FoeHpValues: [100, 150, 200, 215, 230, 240]),

        // 11. Tekst (H3). Zijn HP is de tekst "40": een klap trekt niets af, het getal wordt erachter geplakt.
        // Bij 18 tekens crasht hij. Jouw Mep is 5.5, en dat zijn drie tekens; opgeladen is het 11, maar twee.
        // Hier is opwinden dus slecht. "vijand HP <" bestaat niet: tekst vergelijk je niet met een getal.
        new("telex",
            new BotSpec("telex", MaxHp: 0, WhackDamage: 8, BlockAmount: 0, RepairAmount: 0, Repairs: 0,
                ValueKind.String, StartText: "40", CrashLength: 18),
            [Rule.Otherwise(Move.Whack)],
            Slots: 3,
            [Check.Always, Check.MyHpBelow, Check.IAmCharged, Check.FoeCharged, Check.FoeBlocking, Check.EveryNthTurn],
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Player: Player with { WhackDamage = 5.5 }),

        // 12. Afronden (H4). Hij rondt elke klap af met Math.Round: 3.5 wordt 4, meer dan afkappen. Maar elke tweede
        // beurt zet hij een dun schild van 1, en dan blijft er 2.5 over: dat wordt 2, naar het dichtste even getal.
        new("estimator",
            new BotSpec("estimator", MaxHp: 38, WhackDamage: 7, BlockAmount: 1, RepairAmount: 0, Repairs: 0, Damage: DamageRule.Round),
            [new Rule(C(Check.EveryNthTurn, 2), Move.HoldFirmly), Rule.Otherwise(Move.Whack)],
            Slots: 3,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Player: Player with { WhackDamage = 3.5 }),

        // 13. Casting (H4) en modulo (H2). 600 HP, een int: wegmeppen haal je niet. Omgieten naar byte kan: (byte)hp
        // controleert nooit en houdt hp % 256 over. Meteen omgieten maakt 600 tot 88; wacht je tot onder 500, dan wordt
        // het 244. En een regel "altijd → omgieten" blijft vuren als hij al een byte is: zet er een regel boven die dan wint.
        new("giant",
            new BotSpec("giant", MaxHp: 600, WhackDamage: 4, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [Rule.Otherwise(Move.Whack)],
            Slots: 3,
            AllChecks,
            [.. AllMoves, Move.CastToByte],
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Player: Player with { WhackDamage = 10 },
            FoeHpValues: [100, 200, 300, 400, 500]),

        // 14. Convert (H4). Elke derde beurt laadt hij op voor een dubbele klap. Convert.ToByte controleert wel:
        // zolang hij boven 255 staat, crasht de conversie en valt zijn volgende zet weg. Onder 256 lukt ze gewoon,
        // en dan helpt alleen omgieten nog.
        new("titan",
            new BotSpec("titan", MaxHp: 480, WhackDamage: 4, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.EveryNthTurn, 3), Move.WindUp), new Rule(C(Check.IAmCharged), Move.Whack), Rule.Otherwise(Move.Whack)],
            Slots: 4,
            AllChecks,
            [.. AllMoves, Move.CastToByte, Move.ConvertToByte],
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Player: Player with { WhackDamage = 20 },
            FoeHpValues: [260, 280, 300, 400]),

        // 15. Elite: Dag 248 (Boeing 787, 2015). Een teller van het type byte telt elke beurt op, vanaf 240.
        // Zijn enige regel: NIET teller < 240 → Mep. Na 16 beurten loopt de teller over naar 0, en dan klopt er
        // niets meer: hij valt stil, zoals de generatoren van de 787. Wie zijn regel leest, houdt het tot dan uit.
        new("day-248",
            new BotSpec("day-248", MaxHp: 100, WhackDamage: 9, BlockAmount: 0, RepairAmount: 0, Repairs: 0, CounterStart: 240),
            [new Rule(C(Check.MyCounterBelow, 240, not: true), Move.Whack)],
            Slots: 4,
            AllChecks,
            AllMoves,
            Joins: [Join.And, Join.Or], AllowNot: true,
            [Rule.Otherwise(Move.Whack)],
            Bug: true),
    ];

    public static Level Get(string key) => All.First(l => l.Key == key);

    public static int IndexOf(string key) => All.ToList().FindIndex(l => l.Key == key);

    public static Duel Start(Level level, IReadOnlyList<Rule> playerRules) =>
        new(level.PlayerBot, playerRules, level.Enemy, level.EnemyRules);
}
