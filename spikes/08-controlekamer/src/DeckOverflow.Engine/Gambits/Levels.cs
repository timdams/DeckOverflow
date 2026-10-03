namespace DeckOverflow.Engine.Gambits;

/// <summary>
/// Eén gevecht in de Controlekamer: de vijand met zijn regels, en wat de speler mag gebruiken.
/// De woordenschat groeit per gevecht, zodat elke nieuwe check of zet een eigen moment krijgt.
/// </summary>
/// <param name="Slots">Hoeveel regels de speler mag opstellen.</param>
/// <param name="AllowAnd">Mag een regel een tweede voorwaarde krijgen (EN)?</param>
/// <param name="StartRules">Waarmee het regelbord begint.</param>
public sealed record Level(
    string Key,
    BotSpec Enemy,
    IReadOnlyList<Rule> EnemyRules,
    int Slots,
    IReadOnlyList<Check> Checks,
    IReadOnlyList<Move> Moves,
    bool AllowAnd,
    IReadOnlyList<Rule> StartRules);

public static class Levels
{
    public static BotSpec Player { get; } = new("hero", MaxHp: 40, WhackDamage: 6, BlockAmount: 8, RepairAmount: 14, Repairs: 2);

    /// <summary>De getallen die een speler in een check kan kiezen.</summary>
    public static IReadOnlyList<int> ValuesFor(Check check) => check switch
    {
        Check.MyHpBelow or Check.FoeHpBelow => [10, 15, 20, 25, 30],
        Check.EveryNthTurn => [2, 3, 4],
        _ => [0]
    };

    private static Condition C(Check check, int value = 0) => new(check, value);

    public static IReadOnlyList<Level> All { get; } =
    [
        // 1. Hij slaat altijd. Wie alleen terugslaat, verliest nipt: herstel op het juiste moment.
        new("stamper",
            new BotSpec("stamper", MaxHp: 40, WhackDamage: 7, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [Rule.Otherwise(Move.Whack)],
            Slots: 2,
            [Check.Always, Check.MyHpBelow],
            [Move.Whack, Move.PatchUp],
            AllowAnd: false,
            [Rule.Otherwise(Move.Whack)]),

        // 2. Hij laadt op en slaat dan dubbel. Lees zijn regels: schild als hij opgeladen is, en zet die regel bovenaan.
        new("press",
            new BotSpec("press", MaxHp: 40, WhackDamage: 7, BlockAmount: 0, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.IAmCharged), Move.Whack), Rule.Otherwise(Move.WindUp)],
            Slots: 3,
            [Check.Always, Check.MyHpBelow, Check.FoeCharged],
            [Move.Whack, Move.PatchUp, Move.HoldFirmly],
            AllowAnd: false,
            [Rule.Otherwise(Move.Whack)]),

        // 3. Elke derde beurt zet hij een dik schild. Slaan op een schild is verspild: laad dan op.
        new("metronome",
            new BotSpec("metronome", MaxHp: 45, WhackDamage: 6, BlockAmount: 20, RepairAmount: 0, Repairs: 0),
            [new Rule(C(Check.EveryNthTurn, 3), Move.HoldFirmly), Rule.Otherwise(Move.Whack)],
            Slots: 3,
            [Check.Always, Check.MyHpBelow, Check.FoeCharged, Check.FoeBlocking, Check.EveryNthTurn],
            [Move.Whack, Move.PatchUp, Move.HoldFirmly, Move.WindUp],
            AllowAnd: false,
            [Rule.Otherwise(Move.Whack)]),

        // 4. Hij herstelt onder 20. Zijn herstelregel staat bovenaan, ook als hij niets meer heeft om te herstellen.
        new("mender",
            new BotSpec("mender", MaxHp: 50, WhackDamage: 8, BlockAmount: 12, RepairAmount: 10, Repairs: 2),
            [new Rule(C(Check.MyHpBelow, 20), Move.PatchUp), new Rule(C(Check.FoeCharged), Move.HoldFirmly), Rule.Otherwise(Move.Whack)],
            Slots: 4,
            [Check.Always, Check.MyHpBelow, Check.FoeHpBelow, Check.IAmCharged, Check.FoeCharged, Check.FoeBlocking, Check.EveryNthTurn],
            [Move.Whack, Move.PatchUp, Move.HoldFirmly, Move.WindUp],
            AllowAnd: true,
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
            [Check.Always, Check.MyHpBelow, Check.FoeHpBelow, Check.IAmCharged, Check.FoeCharged, Check.FoeBlocking, Check.EveryNthTurn],
            [Move.Whack, Move.PatchUp, Move.HoldFirmly, Move.WindUp],
            AllowAnd: true,
            [Rule.Otherwise(Move.Whack)]),
    ];

    public static Level Get(string key) => All.First(l => l.Key == key);

    public static Duel Start(Level level, IReadOnlyList<Rule> playerRules) =>
        new(Player, playerRules, level.Enemy, level.EnemyRules);
}
