namespace DeckOverflow.ControlRoom.Levels;

/// <summary>
/// Hoe alle winnende borden van een gevecht scoren, zoals de histogrammen van Opus Magnum: per aantal beurten
/// en per aantal regels, hoeveel borden er zo winnen. De speler ziet waar zijn eigen bord staat.
/// Vooraf uitgerekend met de <see cref="Solver"/> (tot 3 regels, zonder EN, OF of NIET), want in de browser duurt dat te lang.
/// </summary>
public sealed record Histogram(int Total, SortedDictionary<int, int> Turns, SortedDictionary<int, int> Rules)
{
    public const int MaxRules = 3;

    public static Histogram Of(Level level)
    {
        var wins = Solver.Wins(level, MaxRules);
        return new Histogram(
            wins.Count,
            new SortedDictionary<int, int>(wins.GroupBy(w => w.Turns).ToDictionary(g => g.Key, g => g.Count())),
            new SortedDictionary<int, int>(wins.GroupBy(w => w.Rules.Count).ToDictionary(g => g.Key, g => g.Count())));
    }
}
