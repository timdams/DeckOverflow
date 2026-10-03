using System.Text;
using DeckOverflow.Engine.Gambits;

namespace DeckOverflow.Web.Text;

/// <summary>Regels als zin voor het bord, en als C# voor "onder de motorkap".</summary>
public static class RuleText
{
    public static string Condition(Strings s, Condition c) =>
        s.T($"check.{c.Check}", ("n", c.Value));

    public static string Move(Strings s, Move m) => s.T($"move.{m}");

    public static string Sentence(Strings s, Rule r)
    {
        string when = r.When.Check == Check.Always && r.And is null
            ? s.T("rule.otherwise")
            : s.T("rule.if", ("cond", Condition(s, r.When))) + (r.And is null ? "" : " " + s.T("rule.and", ("cond", Condition(s, r.And))));
        return $"{when} → {Move(s, r.Then)}";
    }

    /// <summary>
    /// De regels als <c>if</c> / <c>else if</c>-keten. Een "altijd" onderaan wordt <c>else</c>;
    /// een "altijd" ergens anders wordt <c>else if (true)</c>, zodat je ziet waarom alles eronder nooit loopt.
    /// </summary>
    public static string CSharp(IReadOnlyList<Rule> rules)
    {
        if (rules.Count == 0) return "// no rules: stand still";
        var sb = new StringBuilder();
        for (int i = 0; i < rules.Count; i++)
        {
            Rule r = rules[i];
            bool always = r.When.Check == Check.Always && r.And is null;
            bool last = i == rules.Count - 1;
            string call = $"{r.Then}();";

            if (always && i == 0 && last) { sb.AppendLine(call); break; }

            string head = always && last ? "else"
                : (i == 0 ? "if" : "else if") + $" ({Expr(r)})";
            sb.AppendLine(head);
            sb.AppendLine("    " + call);
        }
        return sb.ToString().TrimEnd();
    }

    private static string Expr(Rule r) =>
        r.And is null ? Expr(r.When) : $"{Expr(r.When)} && {Expr(r.And)}";

    private static string Expr(Condition c) => c.Check switch
    {
        Check.Always => "true",
        Check.MyHpBelow => $"me.Hp < {c.Value}",
        Check.FoeHpBelow => $"foe.Hp < {c.Value}",
        Check.IAmCharged => "me.IsCharged",
        Check.FoeCharged => "foe.IsCharged",
        Check.FoeBlocking => "foe.Block > 0",
        Check.EveryNthTurn => $"turn % {c.Value} == 0",
        _ => "?"
    };
}
