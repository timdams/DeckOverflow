using System.Text.Json.Serialization;
using DeckOverflow.Core.Values;

namespace DeckOverflow.ControlRoom.Gambits;

/// <summary>Hoe een automaat met een geheel type een kommagetal aan schade ontvangt.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DamageRule>))]
public enum DamageRule
{
    /// <summary>Een gewone conversie, zoals <c>(int)</c>: alles na de komma valt weg.</summary>
    Truncate,
    /// <summary><c>Math.Round</c>: afronden, en bij precies .5 naar het dichtste even getal.</summary>
    Round,
}

/// <summary>De vaste getallen van een automaat.</summary>
/// <param name="WhackDamage">Een kommagetal mag: op een geheel type wordt het afgekapt of afgerond, zoals in de Card Hall.</param>
/// <param name="Repairs">Hoe vaak Patch Up werkt. Daarna doet de zet niets meer, maar de regel klopt nog wel.</param>
/// <param name="Kind">
/// Het type van zijn HP: <c>int</c> (herstel stopt bij het maximum), <c>byte</c> (herstel loopt over voorbij 255),
/// <c>char</c> (een getal dat als letter getoond wordt: 'z' is 122) of <c>string</c> (een klap plakt het getal erachter;
/// te lang, en hij crasht).
/// </param>
/// <param name="Damage">Hoe hij een kommagetal aan schade ontvangt: afkappen of afronden.</param>
/// <param name="StartText">De tekst van een automaat van het type <c>string</c>.</param>
/// <param name="CrashLength">Bij zoveel tekens crasht een automaat van het type <c>string</c>.</param>
/// <param name="CounterStart">Een teller van het type <c>byte</c> die elke eigen zet met 1 oploopt, of leeg.</param>
public sealed record BotSpec(
    string Key,
    int MaxHp,
    double WhackDamage,
    int BlockAmount,
    int RepairAmount,
    int Repairs,
    ValueKind Kind = ValueKind.Int,
    DamageRule Damage = DamageRule.Truncate,
    string? StartText = null,
    int CrashLength = 0,
    int? CounterStart = null);

/// <summary>Een automaat tijdens een duel.</summary>
public sealed class Bot
{
    public Bot(BotSpec spec)
    {
        Spec = spec;
        Hp = spec.MaxHp;
        RepairsLeft = spec.Repairs;
        Text = spec.Kind == ValueKind.String ? spec.StartText ?? "" : null;
        Counter = spec.CounterStart;
        Kind = spec.Kind;
        MaxHp = spec.MaxHp;
    }

    /// <summary>Het maximum van zijn HP. Tekst die gelezen wordt, krijgt het getal dat erin stond als maximum.</summary>
    public int MaxHp { get; internal set; }

    /// <summary>Het type van zijn HP nu: omgieten of converteren kan het veranderen.</summary>
    public ValueKind Kind { get; internal set; }

    /// <summary>Een conversie crashte: zijn volgende zet valt weg.</summary>
    internal bool Stunned { get; set; }

    public BotSpec Spec { get; }
    public int Hp { get; internal set; }
    public int Block { get; internal set; }
    public bool Charged { get; internal set; }
    public int RepairsLeft { get; internal set; }

    /// <summary>De tekst van een automaat van het type <c>string</c>.</summary>
    public string? Text { get; internal set; }

    /// <summary>De teller, als de automaat er een heeft: een <c>byte</c>.</summary>
    public int? Counter { get; internal set; }

    internal bool Crashed { get; set; }

    /// <summary>Een tekst valt om als ze crasht; een getal als het op 0 staat.</summary>
    public bool Dead => Kind == ValueKind.String ? Crashed : Hp <= 0;

    public BotState State() => new(Spec.Key, Hp, MaxHp, Block, Charged, RepairsLeft, Spec.Repairs, Kind,
        Text, Spec.CrashLength, Counter, Dead);
}

/// <summary>Een automaat zoals de shell en de stage hem zien. Klein en plat.</summary>
public sealed record BotState(
    string Key,
    int Hp,
    int MaxHp,
    int Block,
    bool Charged,
    int RepairsLeft,
    int Repairs,
    ValueKind Kind,
    string? Text = null,
    int CrashLength = 0,
    int? Counter = null,
    bool Dead = false);
