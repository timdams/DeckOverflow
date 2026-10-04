using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.Web.Text;

namespace DeckOverflow.Web.Features.ControlRoom;

/// <summary>Regels als zin voor het bord, en als C# voor "onder de motorkap". Alleen tekst, geen regels.</summary>
public static class RuleText
{
    public static string Condition(Strings s, Condition c)
    {
        string text = s.T($"room.check.{c.Check}", ("n", c.Literal));
        return c.Not ? s.T("room.rule.not", ("cond", text)) : text;
    }

    public static string Move(Strings s, Move m) => s.T($"room.move.{m}");

    public static string Sentence(Strings s, Rule r)
    {
        if (r.IsAlways) return $"{s.T("room.rule.otherwise")} → {Move(s, r.Then)}";
        string when = s.T("room.rule.if", ("cond", Condition(s, r.When)));
        if (r.Second is { } second)
            when += " " + s.T(r.Join == Join.And ? "room.rule.and" : "room.rule.or", ("cond", Condition(s, second)));
        return $"{when} → {Move(s, r.Then)}";
    }

    /// <summary>
    /// De regels als <c>if</c> / <c>else if</c>-keten. Een "altijd" onderaan wordt <c>else</c>;
    /// een "altijd" ergens anders wordt <c>else if (true)</c>, zodat je ziet waarom alles eronder nooit loopt.
    /// Een leeg bord is een lege methode: de automaat staat stil.
    /// </summary>
    public static string CSharp(Strings s, IReadOnlyList<Rule> rules) =>
        string.Join("\n", CSharpLines(s, rules).Select(l => l.Text));

    /// <summary>
    /// Dezelfde keten, lijn per lijn, met bij elke lijn de regel waar ze bij hoort (-1 als geen enkele): zo kan het luik
    /// de regel laten oplichten die nu vuurt.
    /// </summary>
    public static IReadOnlyList<(string Text, int Rule)> CSharpLines(Strings s, IReadOnlyList<Rule> rules)
    {
        if (rules.Count == 0) return [(s.T("room.code.empty"), -1)];
        var lines = new List<(string Text, int Rule)>();
        for (int i = 0; i < rules.Count; i++)
        {
            Rule r = rules[i];
            bool last = i == rules.Count - 1;
            string call = $"{r.Then}();";

            if (r.IsAlways && i == 0 && last) { lines.Add((call, i)); break; }

            string head = r.IsAlways && last ? "else" : (i == 0 ? "if" : "else if") + $" ({r.Expression()})";
            lines.Add((head, i));
            lines.Add(("    " + call, i));
        }
        return lines;
    }
}
