using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Core.Random;

namespace DeckOverflow.Tests;

/// <summary>De dagelijkse run van de Prikklok, en hoe de nachtelijke scorecontrole een ingestuurde run nakijkt.</summary>
public class DailyRunTests
{
    private static readonly DateOnly Day = new(2026, 10, 7);

    private static (Run Run, List<ICommand> Commands) PlayDay(bool debugWin = false) =>
        ReplayTests.PlayAndRecord(DailySeed.For(Day), debugWin: debugWin);

    [Fact]
    public void De_dagelijkse_run_is_de_gewone_start_met_de_seed_van_de_dag()
    {
        var daily = DailyRun.Start(Day);
        var plain = Run.Start(DailySeed.For(Day));

        Assert.Equal(1, daily.Act.Number);
        Assert.Equal(RunPhase.Event, daily.Phase);
        Assert.Equal(System.Text.Json.JsonSerializer.Serialize(plain.Snapshot()), System.Text.Json.JsonSerializer.Serialize(daily.Snapshot()));
    }

    [Fact]
    public void Een_eerlijke_run_klopt()
    {
        var (run, commands) = PlayDay();

        Assert.Equal(ScoreVerdict.Ok, DailyRun.Check(Day, commands, run.Score.Total));
    }

    [Fact]
    public void Een_eerlijke_run_klopt_ook_na_een_rondje_JSON()
    {
        var (run, commands) = PlayDay();

        Assert.Equal(ScoreVerdict.Ok, DailyRun.Check(Day, DailyRun.CommandsToJson(commands), run.Score.Total));
    }

    [Fact]
    public void Een_andere_score_dan_de_motor_uitrekent_wordt_afgewezen()
    {
        var (run, commands) = PlayDay();

        Assert.Equal(ScoreVerdict.WrongScore, DailyRun.Check(Day, commands, run.Score.Total + 100));
    }

    [Fact]
    public void Dezelfde_commands_op_een_andere_dag_geven_een_andere_run()
    {
        var (run, commands) = PlayDay();

        Assert.NotEqual(ScoreVerdict.Ok, DailyRun.Check(Day.AddDays(1), commands, run.Score.Total));
    }

    [Fact]
    public void Een_run_die_niet_af_is_telt_niet()
    {
        var (run, commands) = PlayDay();
        var half = commands.Take(commands.Count / 2).ToList();

        Assert.Equal(ScoreVerdict.Unfinished, DailyRun.Check(Day, half, Run.Replay(DailySeed.For(Day), half).Score.Total));
    }

    [Fact]
    public void Een_run_met_de_sneltoets_W_telt_nooit()
    {
        var (run, commands) = PlayDay(debugWin: true);

        Assert.Equal(RunPhase.Won, run.Phase);
        Assert.Equal(ScoreVerdict.DebugWin, DailyRun.Check(Day, commands, run.Score.Total));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[{\"type\":\"Bestaat niet\"}]")]
    [InlineData("[null]")]
    public void Onleesbare_commands_worden_afgewezen(string json) =>
        Assert.Equal(ScoreVerdict.Unreadable, DailyRun.Check(Day, json, 0));
}
