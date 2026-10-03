namespace DeckOverflow.Engine.Gambits;

/// <summary>
/// Rekent elke regelset zonder EN door, tot een gegeven lengte. Een ontwerpgereedschap: zo zien we of een
/// gevecht meerdere oplossingen heeft en of de naïeve aanpak echt verliest. Geen onderdeel van het spel.
/// </summary>
public static class Solver
{
    public sealed record Solution(IReadOnlyList<Rule> Rules, int Turns, int HpLeft);

    public static IReadOnlyList<Solution> Wins(Level level, int maxRules)
    {
        var vocabulary = Vocabulary(level).ToList();
        var wins = new List<Solution>();
        var current = new List<Rule>();
        Search(level, vocabulary, current, Math.Min(maxRules, level.Slots), wins);
        return wins;
    }

    public static IEnumerable<Rule> Vocabulary(Level level) =>
        from check in level.Checks
        from value in Levels.ValuesFor(check)
        from move in level.Moves
        select new Rule(new Condition(check, value), move);

    private static void Search(Level level, List<Rule> vocabulary, List<Rule> current, int maxRules, List<Solution> wins)
    {
        foreach (var rule in vocabulary)
        {
            current.Add(rule);
            var duel = Levels.Start(level, current.ToList());
            if (duel.RunToEnd() == Outcome.PlayerWon)
                wins.Add(new Solution(current.ToList(), duel.Turn, duel.Bot(Side.Player).Hp));
            // Na een regel die altijd klopt, wordt niets meer bekeken: verder zoeken levert alleen kopieën op.
            if (current.Count < maxRules && rule.When.Check != Check.Always)
                Search(level, vocabulary, current, maxRules, wins);
            current.RemoveAt(current.Count - 1);
        }
    }
}
