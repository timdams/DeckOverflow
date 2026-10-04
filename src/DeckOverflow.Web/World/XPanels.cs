using CardHallPanels = DeckOverflow.CardHall.Achievements.XRegister;
using ControlRoomPanels = DeckOverflow.ControlRoom.Achievements.XRegister;
using DeckOverflow.Core.Achievements;

namespace DeckOverflow.Web.World;

/// <summary>
/// Het ✗-register van de hele fabriek: de panelen van elke afdeling, plus die van de wereld zelf.
/// Een nieuwe afdeling voegt hier haar <see cref="IXPanelSource"/> toe.
/// </summary>
public static class XPanels
{
    /// <summary>Een Codex-pagina tot het einde lezen. Hoort bij geen afdeling: de shell herkent het zelf.</summary>
    public const string ReadTheManual = "read-the-manual";

    private sealed class WorldPanels : IXPanelSource
    {
        public string Department => "world";
        public IReadOnlyList<XPanel> Panels { get; } = [new(ReadTheManual, Hidden: true)];
    }

    /// <summary>In de volgorde van de plattegrond; de wereld als laatste.</summary>
    public static readonly IReadOnlyList<IXPanelSource> Sources = [CardHallPanels.Source, ControlRoomPanels.Source, new WorldPanels()];

    public static IEnumerable<XPanel> All => Sources.SelectMany(s => s.Panels);
}
