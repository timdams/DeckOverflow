using System.Text.Json.Serialization;

namespace DeckOverflow.Engine.Maps;

[JsonConverter(typeof(JsonStringEnumConverter<NodeKind>))]
public enum NodeKind { Fight, Elite, Rest, Event, Shop, Treasure, Boss }

/// <param name="Row">0 is de onderste rij; de baas staat op de bovenste.</param>
/// <param name="Encounter">Welke vijand of welk event. Blijft verborgen voor de speler tot hij binnenstapt.</param>
public sealed record MapNode(int Id, int Row, int Column, NodeKind Kind, string? Encounter);

public readonly record struct MapEdge(int From, int To);

/// <summary>De kaart van één act: knopen in rijen, verbonden van onder naar boven.</summary>
public sealed class ActMap
{
    public ActMap(IReadOnlyList<MapNode> nodes, IReadOnlyList<MapEdge> edges)
    {
        Nodes = nodes;
        Edges = edges;
        Rows = nodes.Max(n => n.Row) + 1;
    }

    public IReadOnlyList<MapNode> Nodes { get; }
    public IReadOnlyList<MapEdge> Edges { get; }
    public int Rows { get; }

    public MapNode Node(int id) => Nodes.First(n => n.Id == id);

    public IEnumerable<MapNode> Children(int id) =>
        Edges.Where(e => e.From == id).Select(e => Node(e.To));

    public IEnumerable<MapNode> Parents(int id) =>
        Edges.Where(e => e.To == id).Select(e => Node(e.From));

    public IEnumerable<MapNode> StartNodes => Nodes.Where(n => n.Row == 0);

    /// <summary>
    /// Eén rechte lijn, voor tests en om een knoop snel te proberen.
    /// Elk element is een soort knoop met optioneel een vaste vijand of event.
    /// </summary>
    public static ActMap Path(params (NodeKind Kind, string? Encounter)[] steps)
    {
        var nodes = steps.Select((s, i) => new MapNode(i, i, 0, s.Kind, s.Encounter)).ToList();
        var edges = Enumerable.Range(0, steps.Length - 1).Select(i => new MapEdge(i, i + 1)).ToList();
        return new ActMap(nodes, edges);
    }
}
