using DeckOverflow.Engine.Gambits;

namespace DeckOverflow.Web.Components;

/// <summary>Een regel terwijl de speler eraan sleutelt. Het bord werkt hierop; de motor krijgt pas een <see cref="Rule"/> bij Run.</summary>
public sealed class RuleDraft
{
    public Check When { get; set; } = Check.Always;
    public int WhenValue { get; set; }
    public Check? And { get; set; }
    public int AndValue { get; set; }
    public Move Then { get; set; } = Move.Whack;

    public Rule ToRule() => new(
        new Condition(When, WhenValue),
        Then,
        And is { } and ? new Condition(and, AndValue) : null);

    public static RuleDraft From(Rule rule) => new()
    {
        When = rule.When.Check,
        WhenValue = rule.When.Value,
        And = rule.And?.Check,
        AndValue = rule.And?.Value ?? 0,
        Then = rule.Then
    };

    /// <summary>Kies een check en zet er meteen een geldig getal bij.</summary>
    public static int DefaultValue(Check check) => Condition.TakesValue(check) ? Levels.ValuesFor(check)[0] : 0;
}
