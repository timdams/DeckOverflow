using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Web.Backend;
using DeckOverflow.Web.Progress;

namespace DeckOverflow.Web.Tests;

/// <summary>De Prikklok in de shell: wanneer een dagelijkse run prikt, en hoe het histogram zijn emmers legt.</summary>
public class PunchClockTests
{
    private static readonly DateOnly Day = new(2026, 10, 7);
    private static readonly ICommand[] Played = [new ChooseNode(0), new EndTurn()];

    [Fact]
    public void De_eerste_uitgespeelde_run_van_de_dag_prikt_met_haar_commands()
    {
        var progress = new PlayerProgress();

        Assert.Equal(PunchOutcome.Counted, progress.TryPunch(Day, 900, Played));

        Assert.Equal(Day, progress.Punch!.Date);
        Assert.Equal(900, progress.Punch.Score);
        Assert.False(progress.Punch.Sent);
        Assert.Equal(Played, DailyRun.CommandsFromJson(progress.Punch.Commands!));
    }

    [Fact]
    public void Een_tweede_run_op_dezelfde_dag_telt_niet_en_laat_de_eerste_staan()
    {
        var progress = new PlayerProgress();
        progress.TryPunch(Day, 900, Played);

        Assert.Equal(PunchOutcome.Again, progress.TryPunch(Day, 2400, Played));
        Assert.Equal(900, progress.Punch!.Score);
    }

    [Fact]
    public void Een_nieuwe_dag_prikt_opnieuw()
    {
        var progress = new PlayerProgress();
        progress.TryPunch(Day, 900, Played);

        Assert.Equal(PunchOutcome.Counted, progress.TryPunch(Day.AddDays(1), 300, Played));
        Assert.Equal(300, progress.Punch!.Score);
    }

    [Fact]
    public void Een_run_met_de_sneltoets_W_prikt_nooit()
    {
        var progress = new PlayerProgress();

        Assert.Equal(PunchOutcome.DebugWin, progress.TryPunch(Day, 5000, [new ChooseNode(0), new DebugWin()]));
        Assert.Null(progress.Punch);
    }

    [Fact]
    public void Het_histogram_vult_de_lege_emmers_ertussen()
    {
        var bars = PunchClock.Bars([new PunchBucket(300, 2), new PunchBucket(600, 1)]);

        Assert.Equal([300, 400, 500, 600], bars.Select(b => b.From));
        Assert.Equal([2, 0, 0, 1], bars.Select(b => b.Players));
    }

    [Fact]
    public void Een_absurde_score_maakt_het_histogram_niet_eindeloos()
    {
        var bars = PunchClock.Bars([new PunchBucket(200, 3), new PunchBucket(int.MaxValue / 100 * 100, 1), new PunchBucket(-100, 1)]);

        Assert.Equal([200], bars.Select(b => b.From));
    }

    [Fact]
    public void Zonder_spelers_geen_emmers() => Assert.Empty(PunchClock.Bars([]));

    [Theory]
    [InlineData(0, 0)]
    [InlineData(99, 0)]
    [InlineData(100, 100)]
    [InlineData(2349, 2300)]
    public void Een_score_valt_in_de_emmer_van_haar_verdieping(int score, int bucket) =>
        Assert.Equal(bucket, PunchClock.BucketOf(score));
}
