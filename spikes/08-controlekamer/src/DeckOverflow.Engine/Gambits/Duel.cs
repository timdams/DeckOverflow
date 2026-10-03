namespace DeckOverflow.Engine.Gambits;

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

    public Bot Bot(Side side) => _bots[(int)side];
    public RuleStats Stats(Side side) => _stats[(int)side];

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

        int fired = Pick(side, me, foe);
        if (fired < 0)
        {
            events.Add(new NoRuleMatched(side));
        }
        else
        {
            Move move = _rules[(int)side][fired].Then;
            events.Add(new RuleFired(side, fired, move));
            Apply(side, move, me, foe, events);
        }

        if (foe.Dead)
            End(side == Side.Player ? Gambits.Outcome.PlayerWon : Gambits.Outcome.EnemyWon, events);
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

    private static void Apply(Side side, Move move, Bot me, Bot foe, List<DuelEvent> events)
    {
        switch (move)
        {
            case Move.Whack:
            {
                bool charged = me.Charged;
                int damage = me.Spec.WhackDamage * (charged ? 2 : 1);
                me.Charged = false;
                int absorbed = Math.Min(foe.Block, damage);
                foe.Block -= absorbed;
                foe.Hp = Math.Max(0, foe.Hp - (damage - absorbed));
                events.Add(new Whacked(side, damage, absorbed, foe.Hp, charged));
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
                int healed = Math.Min(me.Spec.RepairAmount, me.Spec.MaxHp - me.Hp);
                me.Hp += healed;
                events.Add(new Repaired(side, healed, me.Hp, me.RepairsLeft));
                break;
            case Move.WindUp:
                events.Add(new WoundUp(side, me.Charged));
                me.Charged = true;
                break;
        }
    }

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
