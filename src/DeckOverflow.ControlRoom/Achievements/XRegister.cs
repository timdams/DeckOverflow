using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.ControlRoom.Levels;
using DeckOverflow.Core.Achievements;

namespace DeckOverflow.ControlRoom.Achievements;

/// <summary>
/// De ✗-panelen van de Controlekamer, en hoe de motor ze herkent aan een afgelopen duel.
/// Het register zelf hoort bij de wereld: die krijgt de panelen via <see cref="Source"/>.
/// </summary>
public static class XRegister
{
    /// <summary>Winnen met een regel die nooit bekeken werd: dode code laten staan.</summary>
    public const string DeadCode = "dead-code";
    /// <summary>Winnen terwijl je eigen automaat minstens één beurt stilstond, omdat geen regel klopte.</summary>
    public const string StoodStill = "stood-still";
    /// <summary>De shift laten aflopen: 40 beurten zonder winnaar.</summary>
    public const string Overtime = "overtime";
    /// <summary>Winnen zonder één HP te verliezen.</summary>
    public const string Flawless = "flawless";
    /// <summary>Het laatste gevecht van de Controlekamer winnen.</summary>
    public const string ControlRoomCleared = "control-room-cleared";

    public static readonly IReadOnlyList<XPanel> All =
    [
        new(DeadCode),
        new(StoodStill),
        new(ControlRoomCleared),
        new(Overtime, Hidden: true),
        new(Flawless, Hidden: true),
    ];

    public static IXPanelSource Source { get; } = new PanelSource();

    /// <summary>De panelen die dit duel verdiende. De wereld bewaart ze; een paneel dat je al had, telt niet opnieuw.</summary>
    public static IReadOnlyList<string> Earned(Level level, Duel duel)
    {
        var earned = new List<string>();
        if (duel.Outcome == Outcome.ShiftOver) earned.Add(Overtime);
        if (duel.Outcome != Outcome.PlayerWon) return earned;

        var stats = duel.Stats(Side.Player);
        if (stats.Checked.Any(c => c == 0)) earned.Add(DeadCode);
        if (duel.PlayerStoodStill > 0) earned.Add(StoodStill);
        if (duel.Bot(Side.Player).Hp == duel.Bot(Side.Player).Spec.MaxHp) earned.Add(Flawless);
        if (level.Key == LevelCatalog.All[^1].Key) earned.Add(ControlRoomCleared);
        return earned;
    }

    private sealed class PanelSource : IXPanelSource
    {
        public string Department => "control-room";
        public IReadOnlyList<XPanel> Panels => All;
    }
}
