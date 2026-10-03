using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Runs;

namespace DeckOverflow.Tests;

/// <summary>TIJDELIJK: de sneltoets W om snel door gevechten te gaan. Weg voor een playtest.</summary>
public class DebugWinTests
{
    [Theory]
    [InlineData(Bestiary.Label)]
    [InlineData(Bestiary.Colossus)]
    [InlineData(Bestiary.Reckoner)]
    public void DebugWin_wint_elk_gevecht_meteen(string enemy)
    {
        var combat = Combat.Start(TestHelpers.Scenario(enemy), 1);

        combat.Handle(new DebugWin());

        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    [Fact]
    public void Na_DebugWin_gaat_de_run_verder_naar_de_beloning()
    {
        var run = Run.Start(255, new RunSetup(Map: ActMap.Path((NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null)), Opening: false));
        run.Handle(new ChooseNode(0));

        run.Handle(new DebugWin());

        Assert.Equal(RunPhase.Reward, run.Phase);
    }
}
