using System.Globalization;
using DeckOverflow.ConveyorBelt.Achievements;
using DeckOverflow.Core.Codex;

namespace DeckOverflow.ConveyorBelt.Belts;

/// <summary>Hoe een kist van de band ging.</summary>
public enum Ending
{
    /// <summary>Aangekomen met precies de juiste inhoud.</summary>
    Delivered,
    /// <summary>Aangekomen, maar met iets anders dan gevraagd.</summary>
    Wrong,
    /// <summary>Van de band gevallen: een vakje zonder band, of buiten het rooster.</summary>
    FellOff,
    /// <summary>Dezelfde toestand kwam terug: deze kist komt nooit meer van de band. Een oneindige loop.</summary>
    Forever,
}

/// <summary>Eén tik: waar de kist staat, wat erin zit, wat er gebeurde, en hoe ver elke teller staat.</summary>
/// <param name="Gate">Bij een poort: wat de voorwaarde gaf.</param>
public sealed record Frame(int Tick, Cell At, Value Value, Note Note, bool? Gate, IReadOnlyDictionary<Cell, int> Counters);

/// <summary>Eén testgeval van begin tot einde, tik per tik. De shell speelt het af en spoelt erin terug.</summary>
/// <param name="Moments">Wat de speler in dit testgeval voelde, voor de Codex, met de getallen van het eerste moment.</param>
public sealed record CaseRun(TestCase Case, IReadOnlyList<Frame> Frames, Ending Ending, Value Result, IReadOnlyList<CodexMoment> Moments)
{
    public bool Passed => Ending == Ending.Delivered;
    public int Ticks => Frames[^1].Tick;
}

/// <summary>Alle testgevallen van een puzzel. Gelukt als elke kist juist aankwam.</summary>
/// <param name="Panels">De ✗-panelen die deze band verdiende.</param>
public sealed record LevelRun(IReadOnlyList<CaseRun> Cases, int Machines, IReadOnlyList<string> Panels)
{
    public bool Solved => Cases.All(c => c.Passed);

    /// <summary>Alle tikken samen: de cycli in de score, zoals op een echte band.</summary>
    public int Cycles => Cases.Sum(c => c.Ticks);

    /// <summary>De Codex-momenten van alle testgevallen, elke pagina één keer, in de volgorde waarin ze gebeurden.</summary>
    public IReadOnlyList<CodexMoment> Moments =>
        [.. Cases.SelectMany(c => c.Moments).GroupBy(m => m.Key).Select(g => g.First())];
}

/// <summary>
/// De band laten draaien. Deterministisch: hetzelfde bord geeft altijd hetzelfde resultaat. Komt dezelfde toestand
/// terug (vakje, richting, inhoud en tellers), dan is het bewezen een oneindige loop, en stopt de band.
/// </summary>
public static class Simulator
{
    /// <summary>Een vangnet dat nooit zou mogen afgaan: elke loop herhaalt vroeg of laat een toestand.</summary>
    public const int MaxTicks = 5000;

    public static LevelRun Run(Level level, IReadOnlyDictionary<Cell, Piece> board)
    {
        var cases = level.Cases.Select(c => Run(level, board, c)).ToList();
        int machines = board.Count(p => level.Free(p.Key) && p.Value.IsMachine);
        var run = new LevelRun(cases, machines, []);
        return run with { Panels = XRegister.Earned(level, run) };
    }

    public static CaseRun Run(Level level, IReadOnlyDictionary<Cell, Piece> board, TestCase test)
    {
        var layout = level.Layout(board);
        var counters = new Dictionary<Cell, int>();
        var gateVisits = new Dictionary<Cell, int>();
        var seen = new HashSet<string>();
        var moments = new Moments();
        var frames = new List<Frame> { new(0, level.Source, test.Input, Note.None, null, Snapshot(counters)) };

        Cell at = level.Source;
        Dir heading = level.SourceOut;
        Value value = test.Input;

        for (int tick = 1; tick <= MaxTicks; tick++)
        {
            Cell next = at.Step(heading);
            if (!level.Inside(next) || !layout.TryGetValue(next, out var piece))
            {
                frames.Add(new Frame(tick, next, value, Note.None, null, Snapshot(counters)));
                return new CaseRun(test, frames, Ending.FellOff, value, moments.All);
            }

            at = next;
            Note note = Note.None;
            bool? gate = null;
            switch (piece)
            {
                case Output:
                    frames.Add(new Frame(tick, at, value, Note.None, null, Snapshot(counters)));
                    return new CaseRun(test, frames, value == test.Expected ? Ending.Delivered : Ending.Wrong, value, moments.All);
                case Belt belt:
                    heading = belt.Out;
                    break;
                case Source source:
                    heading = source.Out;
                    break;
                case Machine machine:
                    Value before = value;
                    (value, note) = machine.Op.Apply(value);
                    heading = machine.Out;
                    moments.NoticeOp(level, machine.Op, before, value, note);
                    break;
                case Gate g:
                    gate = g.When.Holds(value);
                    heading = gate.Value ? g.IfTrue : g.IfFalse;
                    int visits = gateVisits[at] = gateVisits.GetValueOrDefault(at) + 1;
                    moments.NoticeGate(g.When, value);
                    // Een poort die de kist een tweede keer ziet: de band kwam terug, het is een loop
                    if (visits >= 2) moments.Add(CodexCatalog.WhileLoop, ("condition", g.When.CSharp()), ("passes", Num(visits)));
                    break;
                case Counter c:
                    int count = counters.GetValueOrDefault(at);
                    if (count < c.Times)
                    {
                        counters[at] = count + 1;
                        heading = c.Loop;
                    }
                    else
                    {
                        // Klaar: naar buiten, en de volgende keer begint hij opnieuw bij 0, zoals een nieuwe for
                        counters[at] = 0;
                        heading = c.Done;
                        moments.Add(CodexCatalog.ForLoop, ("times", Num(c.Times)));
                        // Een teller die klaar is terwijl een andere nog telt: een loop in een loop
                        var outer = counters.FirstOrDefault(o => o.Key != at && o.Value > 0);
                        if (outer.Value > 0 && layout[outer.Key] is Counter outerCounter)
                            moments.Add(CodexCatalog.NestedLoops, ("outer", Num(outerCounter.Times)), ("inner", Num(c.Times)),
                                ("total", Num(outerCounter.Times * c.Times)));
                    }
                    break;
            }

            frames.Add(new Frame(tick, at, value, note, gate, Snapshot(counters)));
            string state = $"{at}|{heading}|{value}|{string.Join(";", counters.OrderBy(c => c.Key.X).ThenBy(c => c.Key.Y))}";
            if (!seen.Add(state))
            {
                moments.Add(CodexCatalog.InfiniteLoop, ("value", value.ToString()), ("ticks", Num(tick)));
                return new CaseRun(test, frames, Ending.Forever, value, moments.All);
            }
        }
        moments.Add(CodexCatalog.InfiniteLoop, ("value", value.ToString()), ("ticks", Num(MaxTicks)));
        return new CaseRun(test, frames, Ending.Forever, value, moments.All);
    }

    private static Dictionary<Cell, int> Snapshot(Dictionary<Cell, int> counters) => new(counters);

    private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// De momenten voor de Codex, elk één keer, met de getallen van het eerste moment. Ook de pagina's van de
    /// Card Hall gaan hier open: wat een byte of een deling op de band doet, is dezelfde natuurwet.
    /// </summary>
    private sealed class Moments
    {
        private readonly Dictionary<string, CodexMoment> _moments = [];

        public IReadOnlyList<CodexMoment> All => [.. _moments.Values];

        public void Add(string key, params (string Name, string Value)[] values)
        {
            if (_moments.ContainsKey(key)) return;
            _moments[key] = new CodexMoment(key, values.ToDictionary(v => v.Name, v => v.Value));
        }

        public void NoticeOp(Level level, Op op, Value before, Value after, Note note)
        {
            if (note == Note.Overflow)
            {
                int added = op.Kind switch { OpKind.Subtract => -op.Operand, _ => op.Operand };
                Add(CodexCatalog.Overflow, ("target", level.Product), ("type", "byte"), ("max", "255"),
                    ("before", before.ToString()), ("added", Num(added)), ("after", after.ToString()));
            }
            if (note == Note.Truncated)
                Add(CodexCatalog.IntegerDivision, ("expression", $"{before} / {op.Operand}"), ("value", after.ToString()));
        }

        public void NoticeGate(Condition when, Value value)
        {
            if (when.Kind == CondKind.ModZero)
                Add(CodexCatalog.Modulo, ("expression", $"{value} % {when.N}"), ("value", Num((long)value.Number % when.N)));
            if (when.Kind == CondKind.LengthLess)
                Add(CodexCatalog.StringLength, ("text", value.Text), ("length", Num(value.Text.Length)));
        }
    }
}
