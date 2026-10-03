namespace DeckOverflow.Core.Achievements;

/// <summary>
/// Een ✗-paneel uit de handleiding: iets wat verboden is en wat jij toch deed. Alleen spelprestaties,
/// nooit leerprestaties. Naam en uitleg staan in <c>en.json</c> onder <c>xpanel.&lt;key&gt;</c>.
/// De sleutel is uniek over de hele fabriek, zodat de wereld alle panelen in één verzameling bewaart.
/// </summary>
/// <param name="Hidden">Verborgen tot je hem vindt, zodat geruchten zich op de speelplaats verspreiden.</param>
public sealed record XPanel(string Key, bool Hidden = false);

/// <summary>
/// Wat een afdeling aan het ✗-register levert: haar panelen, voor haar eigen pagina. Hoe ze een paneel
/// herkent, beslist de afdeling zelf in haar motor; ze meldt het met een event. De wereld bewaart en toont.
/// </summary>
public interface IXPanelSource
{
    /// <summary>De sleutel van de afdeling, zoals op de plattegrond (bv. <c>card-hall</c>), of <c>world</c>.</summary>
    string Department { get; }

    IReadOnlyList<XPanel> Panels { get; }
}
