namespace DeckOverflow.Engine.Belts;

/// <summary>Hoe een kist van de band ging.</summary>
public enum Ending
{
    /// <summary>Aangekomen met precies de juiste inhoud.</summary>
    Delivered,
    /// <summary>Aangekomen, maar met iets anders dan gevraagd.</summary>
    Wrong,
    /// <summary>Van de band gevallen: een vakje zonder band, of buiten het rooster.</summary>
    FellOff,
    /// <summary>Dezelfde toestand kwam terug: deze kist komt nooit meer van de band. Een oneindige lus.</summary>
    Forever,
}

/// <summary>Eén tik: waar de kist staat, wat erin zit, wat er gebeurde, en hoe ver elke teller staat.</summary>
/// <param name="Gate">Bij een poort: wat de voorwaarde gaf.</param>
public sealed record Frame(int Tick, Cell At, Value Value, Note Note, bool? Gate, IReadOnlyDictionary<Cell, int> Counters);

/// <summary>Eén testgeval van begin tot einde, tik per tik. De shell speelt het af en spoelt erin terug.</summary>
public sealed record CaseRun(TestCase Case, IReadOnlyList<Frame> Frames, Ending Ending, Value Result)
{
    public bool Passed => Ending == Ending.Delivered;
    public int Ticks => Frames[^1].Tick;
}

/// <summary>Alle testgevallen van een puzzel. Gelukt als elke kist juist aankwam.</summary>
public sealed record LevelRun(IReadOnlyList<CaseRun> Cases, int Machines)
{
    public bool Solved => Cases.All(c => c.Passed);

    /// <summary>Alle tikken samen: de cycli in de score, zoals op een echte band.</summary>
    public int Cycles => Cases.Sum(c => c.Ticks);
}

/// <summary>
/// De band laten draaien. Deterministisch: hetzelfde bord geeft altijd hetzelfde resultaat. Komt dezelfde toestand
/// terug (vakje, richting, inhoud en tellers), dan is het bewezen een oneindige lus, en stopt de band.
/// </summary>
public static class Simulator
{
    /// <summary>Een vangnet dat nooit zou mogen afgaan: elke lus herhaalt vroeg of laat een toestand.</summary>
    public const int MaxTicks = 5000;

    public static LevelRun Run(Level level, IReadOnlyDictionary<Cell, Piece> board) =>
        new([.. level.Cases.Select(c => Run(level, board, c))], board.Count(p => level.Free(p.Key) && p.Value.IsMachine));

    public static CaseRun Run(Level level, IReadOnlyDictionary<Cell, Piece> board, TestCase test)
    {
        var layout = level.Layout(board);
        var counters = new Dictionary<Cell, int>();
        var seen = new HashSet<string>();
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
                return new CaseRun(test, frames, Ending.FellOff, value);
            }

            at = next;
            Note note = Note.None;
            bool? gate = null;
            switch (piece)
            {
                case Output:
                    frames.Add(new Frame(tick, at, value, Note.None, null, Snapshot(counters)));
                    return new CaseRun(test, frames, value == test.Expected ? Ending.Delivered : Ending.Wrong, value);
                case Belt belt:
                    heading = belt.Out;
                    break;
                case Source source:
                    heading = source.Out;
                    break;
                case Machine machine:
                    (value, note) = machine.Op.Apply(value);
                    heading = machine.Out;
                    break;
                case Gate g:
                    gate = g.When.Holds(value);
                    heading = gate.Value ? g.IfTrue : g.IfFalse;
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
                    }
                    break;
            }

            frames.Add(new Frame(tick, at, value, note, gate, Snapshot(counters)));
            string state = $"{at}|{heading}|{value}|{string.Join(";", counters.OrderBy(c => c.Key.X).ThenBy(c => c.Key.Y))}";
            if (!seen.Add(state)) return new CaseRun(test, frames, Ending.Forever, value);
        }
        return new CaseRun(test, frames, Ending.Forever, value);
    }

    private static Dictionary<Cell, int> Snapshot(Dictionary<Cell, int> counters) => new(counters);
}
