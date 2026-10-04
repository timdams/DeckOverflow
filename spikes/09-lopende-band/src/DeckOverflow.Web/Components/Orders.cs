namespace DeckOverflow.Web.Components;

/// <summary>
/// Wat elke puzzel aflevert: een product voor het front, een vijand uit de Card Hall. Alleen beeld en verhaal:
/// de bestelbon zelf (wie bestelt, wat) staat in <c>nl.json</c> onder <c>order.&lt;key&gt;</c>.
/// </summary>
public static class Orders
{
    private static readonly Dictionary<string, string> Products = new()
    {
        ["first-belt"] = "knight",
        ["double-up"] = "slime",
        ["three-times"] = "jug",
        ["at-least-once"] = "dripper",
        ["nested"] = "ghost",
        ["round-up"] = "rhythm-turtle",
        ["byte-loop"] = "level-256",
        ["halving"] = "splitter",
        ["padding"] = "label",
        ["zune"] = "zune",
    };

    /// <summary>De tekening van het product, in <c>wwwroot/art/</c>.</summary>
    public static string Product(string level) => Products.GetValueOrDefault(level, "knight");
}
