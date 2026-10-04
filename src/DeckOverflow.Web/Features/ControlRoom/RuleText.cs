using System.Text;
using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.Web.Text;

namespace DeckOverflow.Web.Features.ControlRoom;

/// <summary>Regels als zin voor het bord, en als C# voor "onder de motorkap". Alleen tekst, geen regels.</summary>
public static class RuleText
{
    public static string Condition(Strings s, Condition c)
    {
        string text = s.T($"room.check.{c.Check}", ("n", c.Value));
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
    public static string CSharp(Strings s, IReadOnlyList<Rule> rules)
    {
        if (rules.Count == 0) return s.T("room.code.empty");
        var sb = new StringBuilder();
        for (int i = 0; i < rules.Count; i++)
        {
            Rule r = rules[i];
            bool last = i == rules.Count - 1;
            string call = $"{r.Then}();";

            if (r.IsAlways && i == 0 && last) { sb.AppendLine(call); break; }

            string head = r.IsAlways && last ? "else" : (i == 0 ? "if" : "else if") + $" ({r.Expression()})";
            sb.AppendLine(head);
            sb.AppendLine("    " + call);
        }
        return sb.ToString().TrimEnd();
    }
}
