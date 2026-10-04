namespace DeckOverflow.Engine.Belts;

/// <summary>
/// De puzzels van de Lopende Band, van de basis van H6 naar puzzels die ook kennis uit vorige hoofdstukken vragen.
/// Elke puzzel heeft meerdere testgevallen, zodat je een echte lus bouwt en geen antwoord vast kan leggen.
/// </summary>
public static class LevelCatalog
{
    private static TestCase I(int input, int expected) => new(Value.Int(input), Value.Int(expected));
    private static TestCase B(byte input, byte expected) => new(Value.Byte(input), Value.Byte(expected));
    private static TestCase S(string input, string expected) => new(Value.String(input), Value.String(expected));

    private static readonly Cell In = new(0, 2);
    private static readonly Cell Out = new(8, 2);

    public static IReadOnlyList<Level> All { get; } =
    [
        // 1. Geen lus: band leggen van de bron naar de uitgang, en één machine onderweg.
        new("first-belt", 9, 5, In, Dir.Right, Out,
            [I(0, 5), I(10, 15)],
            [new Offer("add5", OfferKind.Machine, 1, [5], OpKind.Add)]),

        // 2. while: verdubbelen tot het minstens 30 is. Hoe vaak, hangt af van de invoer: dat kan alleen met een lus.
        new("double-up", 9, 5, In, Dir.Right, Out,
            [I(1, 32), I(3, 48), I(5, 40)],
            [
                new Offer("mul2", OfferKind.Machine, 1, [2], OpKind.Multiply),
                new Offer("gate", OfferKind.Gate, 1, [10, 20, 30, 40, 50], Conditions: [CondKind.Less, CondKind.Greater]),
            ]),

        // 3. for: er moet altijd precies 12 bij. Met +4 drie keer, of met +6 twee keer: een teller telt de rondjes.
        new("three-times", 9, 5, In, Dir.Right, Out,
            [I(0, 12), I(7, 19), I(20, 32)],
            [
                new Offer("add4", OfferKind.Machine, 1, [4], OpKind.Add),
                new Offer("add6", OfferKind.Machine, 1, [6], OpKind.Add),
                new Offer("count", OfferKind.Counter, 1, [2, 3, 4, 5]),
            ]),

        // 4. do while: minstens één keer +3, en dan door tot het minstens 10 is. Wie de poort vóór het werk zet,
        // laat 50 onaangeroerd door: een while kijkt eerst, een do while werkt eerst.
        new("at-least-once", 9, 5, In, Dir.Right, Out,
            [I(2, 11), I(50, 53), I(9, 12)],
            [
                new Offer("add3", OfferKind.Machine, 1, [3], OpKind.Add),
                new Offer("gate", OfferKind.Gate, 1, [5, 10, 15], Conditions: [CondKind.Less, CondKind.Greater]),
            ]),

        // 5. Geneste lussen: +24 met alleen +1 en tellers tot 6. Vier keer zes rondjes: een lus in een lus.
        new("nested", 9, 6, new Cell(0, 1), Dir.Right, new Cell(8, 1),
            [I(0, 24), I(5, 29)],
            [
                new Offer("add1", OfferKind.Machine, 1, [1], OpKind.Add),
                new Offer("count", OfferKind.Counter, 2, [2, 3, 4, 5, 6]),
            ]),

        // Terugblik: wat je in de Card Hall en de Controlekamer leerde, gedraagt zich op de band precies zo.

        // 6. Modulo (H2) en een poort (H5): naar boven afronden tot een veelvoud van 5. while (v % 5 != 0) v++;
        new("round-up", 9, 5, In, Dir.Right, Out,
            [I(3, 5), I(10, 10), I(12, 15), I(21, 25)],
            [
                new Offer("add1", OfferKind.Machine, 1, [1], OpKind.Add),
                new Offer("gate", OfferKind.Gate, 1, [2, 3, 5, 10], Conditions: [CondKind.ModZero]),
            ]),

        // 7. Overflow (H2): de kisten zijn bytes. while (v > 100) v += 20; stopt pas omdat de byte overloopt.
        // En een poort "v < 300" stopt nooit: een byte haalt geen 300.
        new("byte-loop", 9, 5, In, Dir.Right, Out,
            [B(250, 14), B(240, 4), B(200, 4)],
            [
                new Offer("add20", OfferKind.Machine, 1, [20], OpKind.Add),
                new Offer("gate", OfferKind.Gate, 1, [100, 200, 300], Conditions: [CondKind.Less, CondKind.Greater]),
            ]),

        // 8. Deling van gehele getallen (H2): halveren tot het hoogstens 5 is. 25 / 2 is 12, niet 12.5.
        new("halving", 9, 5, In, Dir.Right, Out,
            [I(100, 3), I(37, 4), I(64, 4)],
            [
                new Offer("div2", OfferKind.Machine, 1, [2], OpKind.Divide),
                new Offer("gate", OfferKind.Gate, 1, [3, 5, 8], Conditions: [CondKind.Less, CondKind.Greater]),
            ]),

        // 9. Tekst (H3): aanvullen met streepjes tot de Length 6 is. Wat al lang genoeg is, blijft zoals het is.
        new("padding", 9, 5, In, Dir.Right, Out,
            [S("a", "a-----"), S("abc", "abc---"), S("abcdef", "abcdef")],
            [
                new Offer("dash", OfferKind.Machine, 1, [], OpKind.Append, "-"),
                new Offer("gate", OfferKind.Gate, 1, [4, 5, 6, 7], Conditions: [CondKind.LengthLess]),
            ]),

        // 10. Elite: Zune (2008). Een lus die dagen omrekent naar de dag van het jaar, zoals in de Zune 30.
        // Een schrikkeljaar telt 366 dagen: alleen boven 366 trekt de machine een jaar af. Op dag 366 doet ze niets,
        // en blijft de kist rondjes draaien. Oplossen: een uitweg voor precies 366.
        Zune(),
    ];

    private static Level Zune()
    {
        var start = new Dictionary<Cell, Piece>
        {
            [new(1, 2)] = new Belt(Dir.Right),
            [new(1, 3)] = new Belt(Dir.Up),
            [new(1, 4)] = new Belt(Dir.Up),
        };
        for (int x = 3; x < 8; x++) start[new(x, 2)] = new Belt(Dir.Right);

        var fix = new Dictionary<Cell, Piece>
        {
            [new(2, 2)] = new Gate(new Condition(CondKind.Greater, 365), Dir.Down, Dir.Right),
            [new(2, 3)] = new Gate(new Condition(CondKind.Greater, 366), Dir.Down, Dir.Left),
            [new(2, 4)] = new Machine(new Op(OpKind.Subtract, 366), Dir.Left),
        };

        return new Level("zune", 9, 6, In, Dir.Right, Out,
            [I(400, 34), I(1000, 268), I(366, 366)],
            [new Offer("equal", OfferKind.Gate, 1, [365, 366], Conditions: [CondKind.Equal])],
            Fixed: fix, Start: start, Bug: true);
    }

    public static Level Get(string key) => All.First(l => l.Key == key);
}
