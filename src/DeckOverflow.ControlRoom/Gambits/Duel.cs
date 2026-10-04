using System.Globalization;
using DeckOverflow.Core.Codex;
using DeckOverflow.Core.Values;

namespace DeckOverflow.ControlRoom.Gambits;

/// <summary>
/// Twee automaten die om beurten hun regels volgen. Elke beurt handelt eerst de speler, dan de vijand.
/// Blok blijft staan tot de eigen volgende zet, dus het vangt precies één zet van de ander op.
/// Volledig deterministisch: dezelfde regels geven altijd hetzelfde duel.
/// </summary>
public sealed class Duel
{
    /// <summary>Na zoveel beurten is de shift voorbij en heeft niemand gewonnen.</summary>
    public const int MaxTurns = 40;

    private readonly Bot[] _bots;
    private readonly IReadOnlyList<Rule>[] _rules;
    private readonly RuleStats[] _stats;
    private readonly Dictionary<string, CodexMoment> _moments = [];
    private Side _next = Side.Player;

    public Duel(BotSpec player, IReadOnlyList<Rule> playerRules, BotSpec enemy, IReadOnlyList<Rule> enemyRules)
    {
        _bots = [new Bot(player), new Bot(enemy)];
        _rules = [playerRules, enemyRules];
        _stats = [new RuleStats(playerRules.Count), new RuleStats(enemyRules.Count)];
    }

    public int Turn { get; private set; }
    public Outcome? Outcome { get; private set; }
    public bool IsOver => Outcome is not null;

    /// <summary>Hoe vaak de automaat van de speler stilstond omdat geen regel klopte.</summary>
    public int PlayerStoodStill { get; private set; }

    public Bot Bot(Side side) => _bots[(int)side];
    public RuleStats Stats(Side side) => _stats[(int)side];
    public IReadOnlyList<Rule> Rules(Side side) => _rules[(int)side];

    /// <summary>
    /// De regels die de speler in dit duel voelde, elk met de getallen van het eerste moment,
    /// in de volgorde waarin ze gebeurden. Alleen van de speler: de vijand leert niets bij.
    /// </summary>
    public IReadOnlyList<CodexMoment> Moments => [.. _moments.Values];

    /// <summary>Speel één zet: die van de speler of die van de vijand.</summary>
    public IReadOnlyList<DuelEvent> Step()
    {
        if (IsOver) return [];
        var events = new List<DuelEvent>();
        Side side = _next;

        if (side == Side.Player)
        {
            Turn++;
            events.Add(new TurnStarted(Turn));
        }

        Bot me = Bot(side);
        Bot foe = Bot(Other(side));

        if (me.Block > 0)
        {
            events.Add(new BlockExpired(side, me.Block));
            me.Block = 0;
        }

        if (me.Counter is { } counter)
        {
            // De teller loopt elke eigen zet op, als een byte: na 255 komt 0
            (byte next, bool overflowed) = ByteRules.Add((byte)counter, 1);
            me.Counter = next;
            if (overflowed)
            {
                events.Add(new CounterOverflowed(side, counter, next));
                Remember(CodexCatalog.Overflow, ("target", me.Spec.Key), ("type", "byte"), ("max", Num(byte.MaxValue)),
                    ("before", Num(counter)), ("added", "1"), ("after", Num(next)));
            }
        }

        if (me.Stunned)
        {
            // De conversie crashte: deze zet valt weg, zonder dat een regel bekeken wordt
            me.Stunned = false;
            events.Add(new MoveSkipped(side));
            _next = Other(side);
            if (side == Side.Enemy && Turn >= MaxTurns) End(Gambits.Outcome.ShiftOver, events);
            return events;
        }

        int fired = Pick(side, me, foe);
        if (fired < 0)
        {
            events.Add(new NoRuleMatched(side));
            if (side == Side.Player) PlayerStoodStill++;
        }
        else
        {
            if (side == Side.Player) Notice(fired, me, foe);
            Move move = _rules[(int)side][fired].Then;
            events.Add(new RuleFired(side, fired, move));
            Apply(side, move, me, foe, events);
        }

        if (foe.Dead)
            End(side == Side.Player ? Gambits.Outcome.PlayerWon : Gambits.Outcome.EnemyWon, events);
        // Een byte die zichzelf precies op 0 oplapt, valt door zijn eigen zet om
        else if (me.Dead)
            End(side == Side.Player ? Gambits.Outcome.EnemyWon : Gambits.Outcome.PlayerWon, events);
        else if (side == Side.Enemy && Turn >= MaxTurns)
            End(Gambits.Outcome.ShiftOver, events);

        _next = Other(side);
        return events;
    }

    /// <summary>Speel tot het einde. Handig voor tests en om oplossingen te zoeken.</summary>
    public Outcome RunToEnd()
    {
        while (!IsOver) Step();
        return Outcome!.Value;
    }

    /// <summary>Alles wat de shell en de stage nodig hebben om dit moment te tonen, ook bij terugspoelen.</summary>
    public DuelSnapshot Snapshot() => new(
        Turn,
        Bot(Side.Player).State(),
        Bot(Side.Enemy).State(),
        [.. _stats[0].Checked], [.. _stats[0].Fired],
        [.. _stats[1].Checked], [.. _stats[1].Fired],
        Outcome);

    private int Pick(Side side, Bot me, Bot foe)
    {
        var rules = _rules[(int)side];
        var stats = _stats[(int)side];
        for (int i = 0; i < rules.Count; i++)
        {
            stats.Checked[i]++;
            if (rules[i].Matches(me, foe, Turn))
            {
                stats.Fired[i]++;
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Herkent de momenten voor de Codex aan de regel die net vuurde. Kijkt ook naar de regels eronder,
    /// zonder ze mee te tellen: klopte er nog een, dan won de bovenste, en dat is precies wat <c>else if</c> doet.
    /// </summary>
    private void Notice(int fired, Bot me, Bot foe)
    {
        var rules = _rules[(int)Side.Player];
        Rule rule = rules[fired];

        if (!_moments.ContainsKey(CodexCatalog.IfElse))
        {
            for (int j = fired + 1; j < rules.Count; j++)
            {
                if (!rules[j].Matches(me, foe, Turn)) continue;
                Remember(CodexCatalog.IfElse,
                    ("first", rule.Expression()), ("firstMove", rule.Then.ToString()), ("firstNumber", (fired + 1).ToString()),
                    ("second", rules[j].Expression()), ("secondMove", rules[j].Then.ToString()), ("secondNumber", (j + 1).ToString()),
                    ("turn", Turn.ToString()));
                break;
            }
        }

        if (rule.HasComparison)
        {
            Condition compare = rule.When.IsComparison ? rule.When : rule.Second!;
            Remember(CodexCatalog.RelationalOperators,
                ("expression", compare.CSharp()), ("filled", compare.Filled(me, foe, Turn)),
                ("value", Condition.Bool(compare.Holds(me, foe, Turn))));
        }

        if (rule.IsLogical)
        {
            Remember(CodexCatalog.LogicalOperators,
                ("expression", rule.Expression()), ("truths", rule.Truths(me, foe, Turn)), ("move", rule.Then.ToString()));
        }
    }

    private void Remember(string key, params (string Name, string Value)[] values)
    {
        if (_moments.ContainsKey(key)) return;
        _moments[key] = new CodexMoment(key, values.ToDictionary(v => v.Name, v => v.Value));
    }

    private void Apply(Side side, Move move, Bot me, Bot foe, List<DuelEvent> events)
    {
        switch (move)
        {
            case Move.Whack:
            {
                bool charged = me.Charged;
                double raw = me.Spec.WhackDamage * (charged ? 2 : 1);
                me.Charged = false;

                if (foe.Kind == ValueKind.String)
                {
                    events.Add(new Whacked(side, raw, 0, 0, foe.Hp, charged));
                    AppendText(foe, raw, events);
                    break;
                }

                // Zoals in de Card Hall: een blok van een geheel type vangt een halve schade op als een hele
                double remaining = raw;
                int absorbed = 0;
                if (foe.Block > 0)
                {
                    absorbed = (int)Math.Min(foe.Block, Math.Ceiling(remaining));
                    remaining = Math.Max(0, remaining - foe.Block);
                    foe.Block -= absorbed;
                }

                // Een getal van een geheel type: wat na de komma staat, valt weg, of wordt afgerond
                int dealt;
                if (foe.Spec.Damage == DamageRule.Round)
                {
                    dealt = (int)Math.Round(remaining);
                    if (remaining % 1 != 0)
                    {
                        events.Add(new ValueRounded(Other(side), remaining, dealt));
                        Remember(CodexCatalog.Rounding, ("before", Num(remaining)), ("enemy", foe.Spec.Key), ("after", Num(dealt)));
                    }
                }
                else
                {
                    (dealt, double lost) = IntRules.Truncate(remaining);
                    if (lost > 0)
                    {
                        events.Add(new ValueTruncated(Other(side), remaining, dealt));
                        Remember(CodexCatalog.IntTruncation, ("target", foe.Spec.Key), ("before", Num(remaining)), ("after", Num(dealt)));
                    }
                }

                // Spelregel, zoals in de Card Hall: schade stopt op 0
                foe.Hp = Math.Max(0, foe.Hp - dealt);
                events.Add(new Whacked(side, raw, absorbed, dealt, foe.Hp, charged));
                break;
            }
            case Move.HoldFirmly:
                me.Block += me.Spec.BlockAmount;
                events.Add(new Braced(side, me.Spec.BlockAmount, me.Block));
                break;
            case Move.PatchUp:
                if (me.RepairsLeft == 0)
                {
                    events.Add(new RepairEmpty(side));
                    break;
                }
                me.RepairsLeft--;
                if (me.Kind == ValueKind.Byte)
                {
                    // Een byte kent geen maximum van de automaat, alleen dat van het type: voorbij 255 loopt hij over
                    int before = me.Hp;
                    (byte result, bool overflowed) = ByteRules.Add((byte)before, me.Spec.RepairAmount);
                    me.Hp = result;
                    if (overflowed)
                    {
                        events.Add(new ValueOverflowed(side, before, me.Spec.RepairAmount, result, me.RepairsLeft));
                        Remember(CodexCatalog.Overflow, ("target", me.Spec.Key), ("type", "byte"), ("max", Num(byte.MaxValue)),
                            ("before", Num(before)), ("added", Num(me.Spec.RepairAmount)), ("after", Num(result)));
                    }
                    else
                    {
                        events.Add(new Repaired(side, me.Spec.RepairAmount, me.Hp, me.RepairsLeft));
                    }
                    break;
                }
                int healed = Math.Min(me.Spec.RepairAmount, me.Spec.MaxHp - me.Hp);
                me.Hp += healed;
                events.Add(new Repaired(side, healed, me.Hp, me.RepairsLeft));
                break;
            case Move.WindUp:
                events.Add(new WoundUp(side, me.Charged));
                me.Charged = true;
                break;
            case Move.CastToByte:
                CastToByte(foe, events);
                break;
            case Move.ConvertToByte:
                ConvertToByte(foe, events);
                break;
        }
    }

    private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Zoals in de Card Hall: een getal op tekst trekt niets af, het plakt. <c>"40" + 5.5</c> is <c>"405.5"</c>.
    /// Wordt de tekst te lang, dan crasht de automaat.
    /// </summary>
    private void AppendText(Bot target, double amount, List<DuelEvent> events)
    {
        string before = target.Text ?? "";
        string added = TypedValue.OfCard(amount, amount % 1 == 0 ? ValueKind.Int : ValueKind.Double).Plus(TypedValue.String("")).Text;
        target.Text = before + added;
        Side side = SideOf(target);
        events.Add(new TextAppended(side, before, added, target.Text));
        Remember(CodexCatalog.StringConcat, ("expression", $"\"{before}\" + {added}"), ("value", $"\"{target.Text}\""));

        if (target.Spec.CrashLength > 0 && target.Text.Length >= target.Spec.CrashLength)
        {
            target.Crashed = true;
            events.Add(new TextCrashed(side, target.Text.Length, target.Spec.CrashLength));
            Remember(CodexCatalog.StringLength, ("text", target.Text), ("length", Num(target.Text.Length)));
        }
    }

    /// <summary>
    /// Omgieten, zoals in de Card Hall: <c>(byte)hp</c>. Een cast controleert nooit: te groot loopt over.
    /// Wie al een byte is, verandert niet; de regel klopte toch, dus de zet is weg.
    /// </summary>
    private void CastToByte(Bot target, List<DuelEvent> events)
    {
        Side side = SideOf(target);
        if (target.Kind != ValueKind.Int)
        {
            events.Add(new TypeUnchanged(side, target.Kind));
            return;
        }
        int before = target.Hp;
        var cast = CastRules.Convert(before, ValueKind.Byte);
        target.Hp = (int)cast.Result;
        target.Kind = ValueKind.Byte;
        events.Add(new TypeChanged(side, ValueKind.Int, ValueKind.Byte, before, target.Hp, cast.Wrapped, "cast"));
        Remember(CodexCatalog.Casting, ("target", target.Spec.Key), ("from", "int"), ("to", "byte"),
            ("before", Num(before)), ("after", Num(target.Hp)));
    }

    /// <summary>
    /// <c>Convert.ToByte(hp)</c>, zoals in de Card Hall: Convert controleert. Past de waarde niet, dan crasht de
    /// automaat: zijn volgende zet valt weg. Past ze wel, dan is hij een byte.
    /// Een crash opent geen Codex-pagina: het woord exception hoort bij een later hoofdstuk (H10), in H4 heet het crashen.
    /// </summary>
    private void ConvertToByte(Bot target, List<DuelEvent> events)
    {
        Side side = SideOf(target);
        if (target.Kind != ValueKind.Int)
        {
            events.Add(new TypeUnchanged(side, target.Kind));
            return;
        }
        int before = target.Hp;
        double? converted = CastRules.ConvertChecked(before, ValueKind.Byte);
        if (converted is not { } value)
        {
            target.Stunned = true;
            events.Add(new ConversionCrashed(side, before));
            return;
        }
        target.Hp = (int)value;
        target.Kind = ValueKind.Byte;
        events.Add(new TypeChanged(side, ValueKind.Int, ValueKind.Byte, before, target.Hp, false, "convert"));
        Remember(CodexCatalog.Convert, ("expression", $"Convert.ToByte({Num(before)})"), ("value", Num(target.Hp)));
    }

    private Side SideOf(Bot bot) => bot == Bot(Side.Player) ? Side.Player : Side.Enemy;

    private void End(Outcome outcome, List<DuelEvent> events)
    {
        Outcome = outcome;
        events.Add(new DuelEnded(outcome, Turn));
    }

    private static Side Other(Side side) => side == Side.Player ? Side.Enemy : Side.Player;
}

/// <summary>
/// Per regel: hoe vaak werd ze bekeken, en hoe vaak klopte ze als eerste.
/// Nooit bekeken betekent dat een regel erboven altijd eerst klopte.
/// </summary>
public sealed class RuleStats(int count)
{
    public int[] Checked { get; } = new int[count];
    public int[] Fired { get; } = new int[count];
}

/// <summary>
/// Een duel op één moment: beide automaten en de tellers per regel. De stage tekent hiermee
/// (een <c>sync</c> na elke zet), en de tijdlijn springt ermee naar elke beurt.
/// </summary>
public sealed record DuelSnapshot(
    int Turn,
    BotState Player,
    BotState Enemy,
    int[] PlayerChecked,
    int[] PlayerFired,
    int[] EnemyChecked,
    int[] EnemyFired,
    Outcome? Outcome);
