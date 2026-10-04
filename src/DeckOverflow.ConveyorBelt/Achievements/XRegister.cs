using DeckOverflow.ConveyorBelt.Belts;
using DeckOverflow.Core.Achievements;

namespace DeckOverflow.ConveyorBelt.Achievements;

/// <summary>
/// De ✗-panelen van de Lopende Band, en hoe de motor ze herkent aan een band die draaide.
/// Het register zelf hoort bij de wereld: die krijgt de panelen via <see cref="Source"/>.
/// </summary>
public static class XRegister
{
    /// <summary>Een kist die nooit van de band komt: een oneindige loop.</summary>
    public const string BeltForever = "belt-forever";
    /// <summary>Een kist die van de band valt.</summary>
    public const string DroppedCrate = "dropped-crate";
    /// <summary>De laatste bestelling van de Lopende Band afleveren.</summary>
    public const string ConveyorCleared = "conveyor-cleared";

    public static readonly IReadOnlyList<XPanel> All =
    [
        new(BeltForever),
        new(ConveyorCleared),
        new(DroppedCrate, Hidden: true),
    ];

    public static IXPanelSource Source { get; } = new PanelSource();

    /// <summary>De panelen die deze band verdiende. De wereld bewaart ze; een paneel dat je al had, telt niet opnieuw.</summary>
    public static IReadOnlyList<string> Earned(Level level, LevelRun run)
    {
        var earned = new List<string>();
        if (run.Cases.Any(c => c.Ending == Ending.Forever)) earned.Add(BeltForever);
        if (run.Cases.Any(c => c.Ending == Ending.FellOff)) earned.Add(DroppedCrate);
        if (run.Solved && level.Key == LevelCatalog.All[^1].Key) earned.Add(ConveyorCleared);
        return earned;
    }

    private sealed class PanelSource : IXPanelSource
    {
        public string Department => "conveyor-belt";
        public IReadOnlyList<XPanel> Panels => All;
    }
}
