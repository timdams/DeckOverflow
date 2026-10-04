namespace DeckOverflow.Engine.Belts;

/// <summary>Eén invoer en wat er aan het einde van de band moet uitkomen.</summary>
public sealed record TestCase(Value Input, Value Expected);

public enum OfferKind { Machine, Gate, Counter }

/// <summary>
/// Iets uit de gereedschapsbak: een soort machine, poort of teller, met de getallen die je mag kiezen en hoeveel
/// je er mag leggen. Band is altijd onbeperkt.
/// </summary>
public sealed record Offer(
    string Id,
    OfferKind Kind,
    int Max,
    IReadOnlyList<int> Choices,
    OpKind Op = OpKind.Add,
    string Text = "",
    IReadOnlyList<CondKind>? Conditions = null)
{
    public bool Matches(Piece piece) => piece switch
    {
        Machine m => Kind == OfferKind.Machine && m.Op.Kind == Op
            && (Op == OpKind.Append ? m.Op.Text == Text : Choices.Contains(m.Op.Operand)),
        Gate g => Kind == OfferKind.Gate && (Conditions ?? []).Contains(g.When.Kind) && Choices.Contains(g.When.N),
        Counter c => Kind == OfferKind.Counter && Choices.Contains(c.Times),
        _ => false,
    };

    /// <summary>Het stuk zoals het uit de bak komt, voor de richting die de speler eerst kiest.</summary>
    public Piece Create(Dir dir) => Kind switch
    {
        OfferKind.Machine => new Machine(new Op(Op, Choices.Count > 0 ? Choices[0] : 0, Text), dir),
        OfferKind.Gate => new Gate(new Condition((Conditions ?? [CondKind.Less])[0], Choices[0]), dir, Turn(dir)),
        OfferKind.Counter => new Counter(Choices[0], Turn(dir), dir),
        _ => new Belt(dir),
    };

    private static Dir Turn(Dir dir) => (Dir)(((int)dir + 1) % 4);
}

/// <summary>
/// Eén puzzel op de band: het rooster, waar de kisten vandaan komen en naartoe moeten, de testgevallen,
/// wat al vastligt, waarmee het bord begint en wat er in de gereedschapsbak zit.
/// </summary>
/// <param name="Fixed">Stukken die de speler niet kan weghalen of draaien.</param>
/// <param name="Start">Waarmee het bord begint; de speler mag ze aanpassen of weghalen.</param>
/// <param name="Bug">Een elite is een echte bug: na een overwinning staat haar verhaal onder <c>bug.&lt;key&gt;</c>.</param>
public sealed record Level(
    string Key,
    int Width,
    int Height,
    Cell Source,
    Dir SourceOut,
    Cell Output,
    IReadOnlyList<TestCase> Cases,
    IReadOnlyList<Offer> Palette,
    IReadOnlyDictionary<Cell, Piece>? Fixed = null,
    IReadOnlyDictionary<Cell, Piece>? Start = null,
    bool Bug = false)
{
    public IReadOnlyDictionary<Cell, Piece> FixedPieces => Fixed ?? new Dictionary<Cell, Piece>();
    public IReadOnlyDictionary<Cell, Piece> StartPieces => Start ?? new Dictionary<Cell, Piece>();

    public bool Inside(Cell c) => c.X >= 0 && c.Y >= 0 && c.X < Width && c.Y < Height;

    /// <summary>Mag de speler hier iets leggen? Niet op de bron, de uitgang of een vast stuk.</summary>
    public bool Free(Cell c) => Inside(c) && c != Source && c != Output && !FixedPieces.ContainsKey(c);

    /// <summary>Alles op het rooster: de vaste stukken, de bron, de uitgang en het bord van de speler.</summary>
    public Dictionary<Cell, Piece> Layout(IReadOnlyDictionary<Cell, Piece> board)
    {
        var all = new Dictionary<Cell, Piece>(board.Where(p => Free(p.Key)));
        foreach (var (cell, piece) in FixedPieces) all[cell] = piece;
        all[Source] = new Source(SourceOut);
        all[Output] = new Output();
        return all;
    }

    /// <summary>
    /// Wat er mis is met een bord: stukken die niet in de bak zitten, of te veel van één soort. Leeg is in orde.
    /// De shell laat zo'n bord niet eens ontstaan; de tests kijken het na.
    /// </summary>
    public IReadOnlyList<string> Problems(IReadOnlyDictionary<Cell, Piece> board)
    {
        var problems = new List<string>();
        var used = Palette.ToDictionary(o => o.Id, _ => 0);
        foreach (var (cell, piece) in board)
        {
            if (!Free(cell)) { problems.Add($"{cell}: hier mag niets liggen"); continue; }
            if (!piece.IsMachine) continue;
            var offer = Palette.FirstOrDefault(o => o.Matches(piece) && used[o.Id] < o.Max);
            if (offer is null) { problems.Add($"{cell}: {piece} zit niet (meer) in de bak"); continue; }
            used[offer.Id]++;
        }
        return problems;
    }
}
