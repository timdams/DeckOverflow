using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.ControlRoom.Levels;

namespace DeckOverflow.Web.Features.ControlRoom;

/// <summary>Een voorwaarde terwijl de speler eraan sleutelt.</summary>
public sealed class ConditionDraft
{
    public Check Check { get; set; } = Check.Always;
    public int Value { get; set; }
    public bool Not { get; set; }

    /// <summary>De grens is een char (<c>'a'</c>), zoals tegen een vijand met een char als HP.</summary>
    public bool Char { get; set; }

    public Condition ToCondition() => new(Check, Value, Not, Char);

    public static ConditionDraft From(Condition c) => new() { Check = c.Check, Value = c.Value, Not = c.Not, Char = c.Char };

    /// <summary>Een nieuwe voorwaarde met meteen een geldig getal erbij.</summary>
    public static ConditionDraft Of(Check check, Level level) =>
        new() { Check = check, Value = Condition.TakesValue(check) ? level.ValuesFor(check)[0] : 0, Char = level.IsChar(check) };
}

/// <summary>
/// Een regel terwijl de speler eraan sleutelt. Het bord werkt hierop; de motor krijgt pas een
/// <see cref="Rule"/> als het duel start. Geen spelregels: alleen wat er op het bord ligt.
/// </summary>
public sealed class RuleDraft
{
    public ConditionDraft When { get; set; } = new();
    public ConditionDraft? Second { get; set; }
    public Join Join { get; set; } = Join.And;
    public Move Then { get; set; } = Move.Whack;

    public Rule ToRule() => new(When.ToCondition(), Then, Second?.ToCondition(), Join);

    public static RuleDraft From(Rule rule) => new()
    {
        When = ConditionDraft.From(rule.When),
        Second = rule.Second is { } second ? ConditionDraft.From(second) : null,
        Join = rule.Join,
        Then = rule.Then,
    };
}
