using DeckOverflow.Engine.Gambits;

namespace DeckOverflow.Tests;

/// <summary>
/// De ontwerpbedoeling per gevecht, vastgepind. Faalt er een na het bijstellen van getallen,
/// dan is het gevecht veranderd: kijk eerst met de Solver of het nog doet wat het moet doen.
/// </summary>
public class LevelTests
{
    private static Condition C(Check check, int value = 0) => new(check, value);

    private static Duel Play(string key, params Rule[] rules)
    {
        var duel = Levels.Start(Levels.Get(key), rules);
        duel.RunToEnd();
        return duel;
    }

    public static TheoryData<string> Keys => new(Levels.All.Select(l => l.Key));

    [Theory, MemberData(nameof(Keys))]
    public void Alleen_slaan_verliest_overal(string key)
    {
        var level = Levels.Get(key);
        Assert.Equal(Outcome.EnemyWon, Play(key, [.. level.StartRules]).Outcome);
    }

    [Theory, MemberData(nameof(Keys))]
    public void Elk_gevecht_is_op_meerdere_manieren_te_winnen_met_drie_regels(string key)
    {
        var level = Levels.Get(key);
        var wins = Solver.Wins(level, Math.Min(3, level.Slots));
        Assert.True(wins.Count >= (level.Slots == 2 ? 1 : 2), $"{key}: {wins.Count} oplossingen");
    }

    [Fact]
    public void Stamper_herstellen_op_tijd_wint_maar_de_volgorde_telt()
    {
        var heal = new Rule(C(Check.MyHpBelow, 10), Move.PatchUp);
        Assert.Equal(Outcome.PlayerWon, Play("stamper", heal, Rule.Otherwise(Move.Whack)).Outcome);

        var wrongOrder = Play("stamper", Rule.Otherwise(Move.Whack), heal);
        Assert.Equal(Outcome.EnemyWon, wrongOrder.Outcome);
        Assert.Equal(0, wrongOrder.Stats(Side.Player).Checked[1]);
    }

    [Fact]
    public void Press_blokken_als_hij_opgeladen_is_wint()
    {
        var brace = new Rule(C(Check.FoeCharged), Move.HoldFirmly);
        Assert.Equal(Outcome.PlayerWon, Play("press", brace, Rule.Otherwise(Move.Whack)).Outcome);
        Assert.Equal(Outcome.EnemyWon, Play("press", Rule.Otherwise(Move.Whack), brace).Outcome);
    }

    [Fact]
    public void Metronome_opladen_tegen_zijn_schild_wint()
    {
        var duel = Play("metronome", new Rule(C(Check.FoeBlocking), Move.WindUp), Rule.Otherwise(Move.Whack));
        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
    }

    [Fact]
    public void Mender_blijft_na_zijn_laatste_herstelling_hangen_in_zijn_bovenste_regel()
    {
        var duel = Levels.Start(Levels.Get("mender"),
            [new Rule(C(Check.EveryNthTurn, 4), Move.PatchUp), Rule.Otherwise(Move.Whack)]);
        var events = new List<DuelEvent>();
        while (!duel.IsOver) events.AddRange(duel.Step());

        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
        Assert.Contains(new RepairEmpty(Side.Enemy), events);
    }

    [Fact]
    public void Goto_fail_bekijkt_zijn_regels_onder_de_dubbele_regel_nooit()
    {
        var duel = Play("goto-fail", new Rule(C(Check.FoeCharged), Move.HoldFirmly), Rule.Otherwise(Move.Whack));

        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
        var stats = duel.Stats(Side.Enemy);
        Assert.True(stats.Fired[2] > 0);
        Assert.Equal([0, 0], stats.Checked[3..]);
    }

    [Fact]
    public void Hetzelfde_regelbord_geeft_altijd_hetzelfde_duel()
    {
        Rule[] rules = [new(C(Check.FoeBlocking), Move.WindUp), Rule.Otherwise(Move.Whack)];
        var a = Play("metronome", rules);
        var b = Play("metronome", rules);
        Assert.Equal((a.Turn, a.Bot(Side.Player).Hp), (b.Turn, b.Bot(Side.Player).Hp));
    }
}
