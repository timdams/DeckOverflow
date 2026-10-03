namespace DeckOverflow.Engine.Gambits;

/// <summary>Wat een automaat kan doen. Elke automaat heeft dezelfde vier zetten, met eigen getallen.</summary>
public enum Move { Whack, HoldFirmly, PatchUp, WindUp }

/// <summary>Waar een regel naar kijkt. "Ik" is de automaat die de regel heeft, "vijand" de andere.</summary>
public enum Check { Always, MyHpBelow, FoeHpBelow, IAmCharged, FoeCharged, FoeBlocking, EveryNthTurn }

public sealed record Condition(Check Check, int Value = 0)
{
    public static Condition Always { get; } = new(Check.Always);

    public bool Holds(Bot me, Bot foe, int turn) => Check switch
    {
        Check.Always => true,
        Check.MyHpBelow => me.Hp < Value,
        Check.FoeHpBelow => foe.Hp < Value,
        Check.IAmCharged => me.Charged,
        Check.FoeCharged => foe.Charged,
        Check.FoeBlocking => foe.Block > 0,
        Check.EveryNthTurn => Value > 0 && turn % Value == 0,
        _ => throw new ArgumentOutOfRangeException(nameof(Check))
    };

    /// <summary>Heeft deze check een getal nodig?</summary>
    public static bool TakesValue(Check check) =>
        check is Check.MyHpBelow or Check.FoeHpBelow or Check.EveryNthTurn;
}

/// <summary>
/// Eén regel: als <see cref="When"/> (en eventueel <see cref="And"/>) klopt, doe <see cref="Then"/>.
/// Een lijst regels werkt als een <c>if</c> / <c>else if</c>-keten: de eerste die klopt, wint.
/// </summary>
public sealed record Rule(Condition When, Move Then, Condition? And = null)
{
    public bool Matches(Bot me, Bot foe, int turn) =>
        When.Holds(me, foe, turn) && (And?.Holds(me, foe, turn) ?? true);

    public static Rule Otherwise(Move then) => new(Condition.Always, then);
}
