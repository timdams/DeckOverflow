using DeckOverflow.Engine.Maps;

namespace DeckOverflow.Web.Components;

/// <summary>Placeholder-iconen voor de knopen op de map. De namen staan in <c>en.json</c> onder <c>node.&lt;soort&gt;</c>.</summary>
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

    public static string Key(NodeKind kind) => $"node.{kind.ToString().ToLowerInvariant()}";

    public static readonly NodeKind[] Legend =
        [NodeKind.Fight, NodeKind.Elite, NodeKind.Event, NodeKind.Shop, NodeKind.Rest, NodeKind.Treasure, NodeKind.Boss];
}
