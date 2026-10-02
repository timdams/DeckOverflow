using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Random;
using DeckOverflow.Engine.Runs;

namespace DeckOverflow.Tests;

public class MapTests
{
    public static TheoryData<ulong> Seeds()
    {
        var data = new TheoryData<ulong>();
        for (ulong seed = 1; seed <= 200; seed++) data.Add(seed);
        return data;
    }

    private static ActMap Generate(ulong seed) => MapGenerator.Generate(new SeededRng(seed));

    [Fact]
    public void Zelfde_seed_geeft_dezelfde_map()
    {
        var a = Generate(255);
        var b = Generate(255);

        Assert.Equal(a.Nodes, b.Nodes);
        Assert.Equal(a.Edges, b.Edges);
    }

    [Fact]
    public void Andere_seeds_geven_andere_maps()
    {
        var maps = Enumerable.Range(1, 10).Select(s => Generate((ulong)s)).ToList();

        Assert.True(maps.Select(m => string.Join(",", m.Nodes.Select(n => $"{n.Row}{n.Column}{n.Kind}"))).Distinct().Count() > 5);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Vaste_rijen_geven_ritme(ulong seed)
    {
        var map = Generate(seed);

        Assert.All(map.Nodes.Where(n => n.Row == 0), n => Assert.Equal(NodeKind.Fight, n.Kind));
        Assert.All(map.Nodes.Where(n => n.Row == MapGenerator.TreasureRow), n => Assert.Equal(NodeKind.Treasure, n.Kind));
        Assert.All(map.Nodes.Where(n => n.Row == MapGenerator.RestRow), n => Assert.Equal(NodeKind.Rest, n.Kind));
        var boss = Assert.Single(map.Nodes, n => n.Kind == NodeKind.Boss);
        Assert.Equal(MapGenerator.Rows, boss.Row);
        Assert.Equal(Bestiary.Rekenmeester, boss.Encounter);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Elke_knoop_ligt_op_een_pad_van_onder_naar_de_baas(ulong seed)
    {
        var map = Generate(seed);

        Assert.True(map.StartNodes.Count() >= 2, "Er moet al een keuze zijn bij de eerste stap.");
        foreach (var node in map.Nodes)
        {
            if (node.Kind != NodeKind.Boss) Assert.NotEmpty(map.Children(node.Id));
            if (node.Row > 0) Assert.NotEmpty(map.Parents(node.Id));
            Assert.All(map.Children(node.Id), c => Assert.Equal(node.Row + 1, c.Row));
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Paden_kruisen_nooit(ulong seed)
    {
        var map = Generate(seed);
        var edges = map.Edges.Select(e => (From: map.Node(e.From), To: map.Node(e.To)))
            .Where(e => e.To.Kind != NodeKind.Boss)
            .ToList();

        foreach (var a in edges)
        foreach (var b in edges)
        {
            if (a.From.Row != b.From.Row) continue;
            bool crosses = a.From.Column < b.From.Column && a.To.Column > b.To.Column;
            Assert.False(crosses, $"Rij {a.From.Row}: {a.From.Column}→{a.To.Column} kruist {b.From.Column}→{b.To.Column}");
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Elke_map_heeft_elites_een_winkel_en_een_event(ulong seed)
    {
        var map = Generate(seed);

        Assert.True(map.Nodes.Count(n => n.Kind == NodeKind.Elite) >= MapGenerator.MinElites);
        Assert.Contains(map.Nodes, n => n.Kind == NodeKind.Shop);
        Assert.Contains(map.Nodes, n => n.Kind == NodeKind.Event);
        Assert.DoesNotContain(map.Nodes, n => n.Kind == NodeKind.Elite && n.Row < MapGenerator.FirstEliteRow);
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Geen_elite_winkel_of_rustvuur_twee_keer_na_elkaar(ulong seed)
    {
        var map = Generate(seed);

        foreach (var edge in map.Edges)
        {
            var from = map.Node(edge.From);
            var to = map.Node(edge.To);
            if (from.Kind is NodeKind.Elite or NodeKind.Shop or NodeKind.Rest)
            {
                Assert.NotEqual(from.Kind, to.Kind);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Seeds))]
    public void Gevechten_en_events_hebben_een_inhoud(ulong seed)
    {
        var map = Generate(seed);

        foreach (var node in map.Nodes)
        {
            switch (node.Kind)
            {
                case NodeKind.Fight:
                    Assert.Contains(node.Encounter!, node.Row <= 1 ? Bestiary.EasyPool : Bestiary.NormalPool);
                    break;
                case NodeKind.Elite:
                    Assert.Contains(node.Encounter!, Bestiary.ElitePool);
                    break;
                case NodeKind.Event:
                    Assert.Contains(node.Encounter!, Adventures.All);
                    break;
            }
        }
    }
}
