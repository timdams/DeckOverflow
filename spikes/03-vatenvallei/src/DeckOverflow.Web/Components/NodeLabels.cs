using DeckOverflow.Engine.Maps;

namespace DeckOverflow.Web.Components;

/// <summary>Placeholder-iconen en namen voor de knopen op de map. Geen spelregels.</summary>
public static class NodeLabels
{
    public static string Glyph(NodeKind kind) => kind switch
    {
        NodeKind.Fight => "⚔",
        NodeKind.Elite => "☠",
        NodeKind.Rest => "🔥",
        NodeKind.Event => "?",
        NodeKind.Shop => "$",
        NodeKind.Treasure => "▣",
        NodeKind.Boss => "♛",
        _ => "·"
    };

    public static string Name(NodeKind kind) => kind switch
    {
        NodeKind.Fight => "Gevecht",
        NodeKind.Elite => "Elite",
        NodeKind.Rest => "Rustvuur",
        NodeKind.Event => "Onbekend",
        NodeKind.Shop => "Winkel",
        NodeKind.Treasure => "Schat",
        NodeKind.Boss => "Baas",
        _ => ""
    };

    public static readonly NodeKind[] Legend =
        [NodeKind.Fight, NodeKind.Elite, NodeKind.Event, NodeKind.Shop, NodeKind.Rest, NodeKind.Treasure, NodeKind.Boss];
}
