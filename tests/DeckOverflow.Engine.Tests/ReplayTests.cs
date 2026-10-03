using System.Text.Json;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Random;
using DeckOverflow.Engine.Runs;

namespace DeckOverflow.Tests;

/// <summary>Een run is een seed plus een commandolijst: opnieuw afspelen geeft dezelfde run. Basis voor de scorecontrole.</summary>
public class ReplayTests
{
    [Fact]
    public void Commands_overleven_een_rondje_JSON()
    {
        ICommand[] commands =
        [
            new PlayCard(2, Combat.EnemyId), new EndTurn(), new ScrapModifiers(), new DebugWin(), new ChooseNode(4),
            new TakeRewardCard(1), new SkipReward(), new ChooseRelic(0), new RestHeal(), new RestUpgrade(3),
            new ChooseEventOption(1, 5), new BuyCard(2), new BuyRelic(), new BuyRemoval(0), new OpenChest(), new Leave()
        ];

        string json = JsonSerializer.Serialize(commands);
        var back = JsonSerializer.Deserialize<ICommand[]>(json);

        Assert.Equal(commands, back);
    }

    [Fact]
    public void Command_draagt_zijn_type_in_de_JSON()
    {
        string json = JsonSerializer.Serialize<ICommand>(new PlayCard(0, Combat.EnemyId));

        Assert.Contains("\"type\":\"PlayCard\"", json);
    }

    [Theory]
    [InlineData(255UL)]
    [InlineData(4018561319058542423UL)]
    public void Opgenomen_run_speelt_opnieuw_af_tot_dezelfde_run(ulong seed)
    {
        var (run, commands) = PlayAndRecord(seed);
        Assert.True(commands.Count > 20, $"De bot deed maar {commands.Count} stappen.");

        string json = JsonSerializer.Serialize(commands);
        var replayed = Run.Replay(seed, JsonSerializer.Deserialize<List<ICommand>>(json)!);

        Assert.Equal(JsonSerializer.Serialize(run.Snapshot()), JsonSerializer.Serialize(replayed.Snapshot()));
        Assert.Equal(JsonSerializer.Serialize(run.CombatSnapshot()), JsonSerializer.Serialize(replayed.CombatSnapshot()));
    }

    [Theory]
    [InlineData(255UL, false)]
    [InlineData(4018561319058542423UL, false)]
    [InlineData(255UL, true)]
    public void Opnieuw_afspelen_geeft_dezelfde_score(ulong seed, bool debugWin)
    {
        var (run, commands) = PlayAndRecord(seed, debugWin: debugWin);

        string json = JsonSerializer.Serialize(commands);
        var replayed = Run.Replay(seed, JsonSerializer.Deserialize<List<ICommand>>(json)!);

        Assert.Equal(run.Score, replayed.Score);
        Assert.Equal(run.Snapshot().End!.Score, replayed.Score);
    }

    [Fact]
    public void Een_uitgespeelde_run_telt_alle_21_verdiepingen_en_de_resterende_HP()
    {
        var (run, _) = PlayAndRecord(255, debugWin: true);

        Assert.Equal(RunPhase.Won, run.Phase);
        var score = run.Score;
        Assert.Equal(Acts.All.Count * 7, score.Floors);
        Assert.Equal(run.Hp, score.HpLeft);
        Assert.True(score.Turns > 0);
        Assert.Equal(score.Floors * 100 + run.Hp * 10 + Math.Max(0, 150 - score.Turns) * 5, score.Total);
    }

    [Fact]
    public void Een_verloren_run_telt_alleen_de_verdiepingen_die_je_voorbij_bent()
    {
        var (run, _) = PlayAndRecord(255);

        Assert.Equal(RunPhase.Lost, run.Phase);
        var end = run.Snapshot().End!;
        Assert.Equal((end.Act - 1) * 7 + end.Floor - 1, run.Score.Floors);
        Assert.Equal(run.Score.Floors * 100, run.Score.Total);
    }

    [Fact]
    public void Winnen_scoort_altijd_meer_dan_verliezen()
    {
        // De slechtste winst (1 HP, traag) tegenover de beste nederlaag (gestorven bij de laatste baas)
        var worstWin = new RunScore(21, Won: true, HpLeft: 1, Turns: 999);
        var bestLoss = new RunScore(20, Won: false, HpLeft: 0, Turns: 1);

        Assert.True(worstWin.Total > bestLoss.Total);
        Assert.Equal(0, new RunScore(5, Won: false, HpLeft: 40, Turns: 10).HpPoints);
    }

    [Fact]
    public void Andere_commands_geven_een_andere_run()
    {
        var (run, commands) = PlayAndRecord(255);

        var replayed = Run.Replay(255, commands.Take(commands.Count / 2));

        Assert.NotEqual(JsonSerializer.Serialize(run.Snapshot()), JsonSerializer.Serialize(replayed.Snapshot()));
    }

    [Fact]
    public void Dagelijkse_seed_is_vastgepind()
    {
        // Pint de hash vast: faalt dit, dan krijgt elke dag een andere run dan wie al speelde.
        Assert.Equal(4018561319058542423UL, DailySeed.For(new DateOnly(2026, 10, 3)));
        Assert.Equal(4018558020523657790UL, DailySeed.For(new DateOnly(2026, 10, 4)));
    }

    [Fact]
    public void Elke_dag_heeft_een_eigen_seed()
    {
        var start = new DateOnly(2026, 1, 1);
        var seeds = Enumerable.Range(0, 3 * 365).Select(d => DailySeed.For(start.AddDays(d))).ToList();

        Assert.Equal(seeds.Count, seeds.Distinct().Count());
    }

    /// <summary>
    /// Speelt een run met een domme maar volledige bot en neemt elk command op. Ook geweigerde
    /// commands tellen mee: een replay moet die net zo weigeren.
    /// </summary>
    private static (Run Run, List<ICommand> Commands) PlayAndRecord(ulong seed, int maxSteps = 600, bool debugWin = false)
    {
        var run = Run.Start(seed);
        var commands = new List<ICommand>();
        var rejected = new HashSet<int>();
        for (int step = 0; step < maxSteps && run.Phase is not (RunPhase.Won or RunPhase.Lost); step++)
        {
            var command = debugWin && run.Phase == RunPhase.Combat ? new DebugWin() : NextCommand(run, rejected);
            commands.Add(command);
            var events = run.Handle(command);
            // Een geweigerde kaart niet eindeloos opnieuw proberen
            if (command is PlayCard p && events.OfType<PlayRejected>().Any()) rejected.Add(p.HandIndex);
            else rejected.Clear();
        }
        return (run, commands);
    }

    private static ICommand NextCommand(Run run, IReadOnlySet<int> rejected)
    {
        var snap = run.Snapshot();
        switch (snap.Phase)
        {
            case RunPhase.Combat:
                var hand = run.CombatSnapshot()!.Hand;
                for (int i = 0; i < hand.Count; i++)
                {
                    if (!hand[i].Playable || rejected.Contains(i)) continue;
                    return new PlayCard(i, hand[i].Target == Engine.Cards.TargetMode.Self ? Combat.PlayerId : Combat.EnemyId);
                }
                return new EndTurn();
            case RunPhase.Map:
                return new ChooseNode(snap.Map.Nodes.First(n => n.Reachable).Id);
            case RunPhase.Reward or RunPhase.Draft:
                return snap.Reward is { Cards.Count: > 0 } ? new TakeRewardCard(0) : new SkipReward();
            case RunPhase.RelicChoice:
                return new ChooseRelic(0);
            case RunPhase.Rest:
                return snap.Rest!.Outcome is null ? new RestHeal() : new Leave();
            case RunPhase.Event:
                if (snap.Event!.Outcome is not null) return new Leave();
                int option = Math.Max(0, snap.Event.Options.ToList().FindIndex(o => o.Enabled));
                var eligible = snap.Event.Options[option].EligibleCards;
                return new ChooseEventOption(option, eligible.Count > 0 ? eligible[0] : -1);
            case RunPhase.Shop:
                return new Leave();
            case RunPhase.Treasure:
                return snap.Treasure!.Opened ? new Leave() : new OpenChest();
            default:
                throw new InvalidOperationException($"Onverwachte fase {snap.Phase}.");
        }
    }
}
