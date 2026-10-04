using System.Text.Json.Serialization;

namespace DeckOverflow.ControlRoom.Gambits;

/// <summary>Wat een automaat kan doen. Elke automaat heeft dezelfde vier zetten, met eigen getallen.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Move>))]
public enum Move
{
    Whack, HoldFirmly, PatchUp, WindUp,
    /// <summary>Omgieten: <c>(byte)</c> op de HP van de vijand. Een cast controleert nooit.</summary>
    CastToByte,
    /// <summary><c>Convert.ToByte</c> op de HP van de vijand: te groot, en hij crasht.</summary>
    ConvertToByte,
}

/// <summary>Waar een voorwaarde naar kijkt. "Ik" is de automaat die de regel heeft, "vijand" de andere.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Check>))]
public enum Check
{
    Always, MyHpBelow, FoeHpBelow, IAmCharged, FoeCharged, FoeBlocking, EveryNthTurn,
    /// <summary>Alleen voor vijanden met een teller (<see cref="Bot.Counter"/>): <c>me.Uptime &lt; n</c>.</summary>
    MyCounterBelow,
}

/// <summary>Hoe een tweede voorwaarde meedoet: EN (<c>&amp;&amp;</c>) of OF (<c>||</c>).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<Join>))]
public enum Join { And, Or }

/// <summary>
/// Eén voorwaarde, eventueel omgedraaid met NIET (<c>!</c>). Altijd kan niet omgedraaid worden:
/// <c>!true</c> is nooit, en een regel die nooit klopt, heeft geen zin.
/// </summary>
public sealed record Condition(Check Check, int Value = 0, bool Not = false)
{
    public static Condition Always { get; } = new(Check.Always);

    public bool Holds(Bot me, Bot foe, int turn) => Raw(me, foe, turn) != Not;

    private bool Raw(Bot me, Bot foe, int turn) => Check switch
    {
        Check.Always => true,
        Check.MyHpBelow => me.Hp < Value,
        Check.FoeHpBelow => foe.Hp < Value,
        Check.IAmCharged => me.Charged,
        Check.FoeCharged => foe.Charged,
        Check.FoeBlocking => foe.Block > 0,
        Check.EveryNthTurn => Value > 0 && turn % Value == 0,
        Check.MyCounterBelow => me.Counter < Value,
        _ => throw new ArgumentOutOfRangeException(nameof(Check))
    };

    /// <summary>Heeft deze check een getal nodig?</summary>
    public static bool TakesValue(Check check) =>
        check is Check.MyHpBelow or Check.FoeHpBelow or Check.EveryNthTurn or Check.MyCounterBelow;

    /// <summary>Vergelijkt deze check twee waarden (<c>&lt;</c>, <c>==</c>)? Dan is ze een relationele operator.</summary>
    public bool IsComparison => TakesValue(Check);

    /// <summary>De voorwaarde als C#, zoals onder de motorkap: <c>me.Hp &lt; 20</c>, <c>!foe.IsCharged</c>.</summary>
    public string CSharp()
    {
        string raw = Check switch
        {
            Check.Always => "true",
            Check.MyHpBelow => $"me.Hp < {Value}",
            Check.FoeHpBelow => $"foe.Hp < {Value}",
            Check.IAmCharged => "me.IsCharged",
            Check.FoeCharged => "foe.IsCharged",
            Check.FoeBlocking => "foe.Block > 0",
            Check.EveryNthTurn => $"turn % {Value} == 0",
            Check.MyCounterBelow => $"me.Uptime < {Value}",
            _ => "?"
        };
        if (!Not) return raw;
        return IsComparison || Check == Check.FoeBlocking ? $"!({raw})" : $"!{raw}";
    }

    /// <summary>De voorwaarde met de getallen van nu ingevuld: <c>14 &lt; 20</c>. Een bool blijft zijn waarde.</summary>
    public string Filled(Bot me, Bot foe, int turn)
    {
        string raw = Check switch
        {
            Check.MyHpBelow => $"{me.Hp} < {Value}",
            Check.FoeHpBelow => $"{foe.Hp} < {Value}",
            Check.EveryNthTurn => $"{turn} % {Value} == 0",
            Check.FoeBlocking => $"{foe.Block} > 0",
            Check.MyCounterBelow => $"{me.Counter} < {Value}",
            _ => Bool(Raw(me, foe, turn))
        };
        if (!Not) return raw;
        return IsComparison || Check == Check.FoeBlocking ? $"!({raw})" : $"!{raw}";
    }

    internal static string Bool(bool value) => value ? "true" : "false";
}

/// <summary>
/// Eén regel: als <see cref="When"/> klopt (eventueel samen met <see cref="Second"/>, via EN of OF), doe <see cref="Then"/>.
/// Een lijst regels werkt als een <c>if</c> / <c>else if</c>-keten: de eerste die klopt, wint.
/// </summary>
public sealed record Rule(Condition When, Move Then, Condition? Second = null, Join Join = Join.And)
{
    public bool Matches(Bot me, Bot foe, int turn)
    {
        bool first = When.Holds(me, foe, turn);
        if (Second is null) return first;
        return Join == Join.And
            ? first && Second.Holds(me, foe, turn)
            : first || Second.Holds(me, foe, turn);
    }

    /// <summary>Altijd, zonder iets erbij: in C# een <c>else</c> onderaan.</summary>
    public bool IsAlways => When.Check == Check.Always && !When.Not && Second is null;

    /// <summary>Gebruikt de regel EN, OF of NIET?</summary>
    public bool IsLogical => Second is not null || When.Not;

    /// <summary>Vergelijkt de regel ergens een getal?</summary>
    public bool HasComparison => When.IsComparison || Second?.IsComparison == true;

    /// <summary>De voorwaarde als C#-expressie: <c>me.Hp &lt; 20 &amp;&amp; !foe.IsCharged</c>.</summary>
    public string Expression() =>
        Second is null ? When.CSharp() : $"{When.CSharp()} {Op} {Second.CSharp()}";

    /// <summary>Zoals <see cref="Expression"/>, met elk deel ingevuld tot <c>true</c> of <c>false</c>.</summary>
    public string Truths(Bot me, Bot foe, int turn) =>
        Second is null
            ? Condition.Bool(When.Holds(me, foe, turn))
            : $"{Condition.Bool(When.Holds(me, foe, turn))} {Op} {Condition.Bool(Second.Holds(me, foe, turn))}";

    private string Op => Join == Join.And ? "&&" : "||";

    public static Rule Otherwise(Move then) => new(Condition.Always, then);
}
