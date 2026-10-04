using DeckOverflow.ControlRoom.Gambits;

namespace DeckOverflow.ControlRoom.Levels;

/// <summary>
/// Rekent elke regelset zonder EN of OF door, tot een gegeven lengte. Een ontwerpgereedschap: zo zien we of een
/// gevecht meerdere oplossingen heeft en of de naïeve aanpak echt verliest. Het spel gebruikt de uitkomst
/// alleen als histogram (<c>control-room/solutions.json</c>), nooit om de speler te helpen.
/// </summary>
public static class Solver
{
    public sealed record Solution(IReadOnlyList<Rule> Rules, int Turns, int HpLeft);

    /// <param name="withNot">Ook voorwaarden met NIET proberen, als het gevecht ze toelaat. Maakt het zoeken veel trager.</param>
    public static IReadOnlyList<Solution> Wins(Level level, int maxRules, bool withNot = false)
    {
        var vocabulary = Vocabulary(level, withNot).ToList();
        var wins = new List<Solution>();
        Search(level, vocabulary, [], Math.Min(maxRules, level.Slots), wins);
        return wins;
    }

    public static IEnumerable<Rule> Vocabulary(Level level, bool withNot = false) =>
        from check in level.Checks
        from value in level.ValuesFor(check)
        from not in withNot && level.AllowNot && check != Check.Always ? new[] { false, true } : [false]
        from move in level.Moves
        select new Rule(level.Condition(check, value, not), move);

    private static void Search(Level level, List<Rule> vocabulary, List<Rule> current, int maxRules, List<Solution> wins)
    {
        foreach (var rule in vocabulary)
        {
            current.Add(rule);
            var duel = LevelCatalog.Start(level, [.. current]);
            if (duel.RunToEnd() == Outcome.PlayerWon)
                wins.Add(new Solution([.. current], duel.Turn, duel.Bot(Side.Player).Hp));
            // Na een regel die altijd klopt, wordt niets meer bekeken: verder zoeken levert alleen kopieën op.
            if (current.Count < maxRules && !rule.IsAlways)
                Search(level, vocabulary, current, maxRules, wins);
            current.RemoveAt(current.Count - 1);
        }
    }
}
