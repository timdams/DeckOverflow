using DeckOverflow.Core.Random;
using DeckOverflow.CardHall.Runs;

namespace DeckOverflow.CardHall.Maps;

/// <summary>
/// Een korte act zoals in Slay the Spire: een paar paden die van onder naar boven lopen,
/// elkaar raken maar nooit kruisen. Vaste rijen geven ritme: gevecht onderaan,
/// schat halverwege, rustvuur voor de baas.
/// </summary>
public static class MapGenerator
{
    /// <summary>Rijen onder de baas: een act van 10 tot 15 minuten.</summary>
    public const int Rows = 6;
    public const int Columns = 5;
    public const int Paths = 4;
    public const int TreasureRow = 3;
    public const int RestRow = Rows - 1;
    public const int FirstEliteRow = 2;

    public const int MinElites = 2;

    private static readonly (NodeKind Kind, int Weight)[] EarlyWeights =
        [(NodeKind.Fight, 50), (NodeKind.Event, 30), (NodeKind.Shop, 20)];

    private static readonly (NodeKind Kind, int Weight)[] LateWeights =
        [(NodeKind.Fight, 40), (NodeKind.Elite, 25), (NodeKind.Event, 20), (NodeKind.Shop, 8), (NodeKind.Rest, 7)];

    /// <summary>Soorten die je niet twee keer na elkaar wil: dan voelt het pad als herhaling.</summary>
    private static readonly HashSet<NodeKind> NoRepeat = [NodeKind.Elite, NodeKind.Shop, NodeKind.Rest];

    /// <summary>De map van act 1.</summary>
    public static ActMap Generate(SeededRng rng) => Generate(rng, Acts.VatValley);

    public static ActMap Generate(SeededRng rng, ActDefinition act)
    {
        // Elke poging gebruikt dezelfde rng verder, dus het resultaat blijft vast per seed
        for (int attempt = 0; attempt < 100; attempt++)
        {
            if (TryGenerate(rng, act) is { } map) return map;
        }
        throw new InvalidOperationException("Geen geldige map na 100 pogingen.");
    }

    private static ActMap? TryGenerate(SeededRng rng, ActDefinition act)
    {
        var edges = new HashSet<((int Row, int Col) From, (int Row, int Col) To)>();
        var cells = new HashSet<(int Row, int Col)>();

        int firstStart = -1;
        for (int p = 0; p < Paths; p++)
        {
            int col = rng.NextInt(Columns);
            // Minstens twee verschillende vertrekpunten, anders is er geen eerste keuze
            while (p == 1 && col == firstStart) col = rng.NextInt(Columns);
            if (p == 0) firstStart = col;

            cells.Add((0, col));
            for (int row = 0; row < Rows - 1; row++)
            {
                int next = NextColumn(rng, edges, row, col);
                edges.Add(((row, col), (row + 1, next)));
                cells.Add((row + 1, next));
                col = next;
            }
        }

        // Ids van onder naar boven, links naar rechts
        var ordered = cells.OrderBy(c => c.Row).ThenBy(c => c.Col).ToList();
        var ids = ordered.Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);
        int bossId = ordered.Count;

        var mapEdges = edges.Select(e => new MapEdge(ids[e.From], ids[e.To]))
            .Concat(ordered.Where(c => c.Row == Rows - 1).Select(c => new MapEdge(ids[c], bossId)))
            .OrderBy(e => e.From).ThenBy(e => e.To)
            .ToList();

        var kinds = new Dictionary<int, NodeKind>();
        var encounters = new Dictionary<int, string?>();
        var parents = mapEdges.GroupBy(e => e.To).ToDictionary(g => g.Key, g => g.Select(e => e.From).ToList());

        foreach (var cell in ordered)
        {
            int id = ids[cell];
            var parentIds = parents.GetValueOrDefault(id) ?? [];
            NodeKind kind = cell.Row switch
            {
                0 => NodeKind.Fight,
                TreasureRow => NodeKind.Treasure,
                RestRow => NodeKind.Rest,
                _ => RollKind(rng, cell.Row, parentIds.Select(p => kinds[p]).ToList())
            };
            kinds[id] = kind;
            encounters[id] = PickEncounter(rng, act, kind, cell.Row, parentIds.Select(p => encounters[p]).ToList());
        }

        if (kinds.Values.Count(k => k == NodeKind.Elite) < MinElites) return null;
        if (!kinds.Values.Contains(NodeKind.Shop)) return null;
        if (!kinds.Values.Contains(NodeKind.Event)) return null;

        var nodes = ordered
            .Select(c => new MapNode(ids[c], c.Row, c.Col, kinds[ids[c]], encounters[ids[c]]))
            .Append(new MapNode(bossId, Rows, Columns / 2, NodeKind.Boss, act.Boss))
            .ToList();

        return new ActMap(nodes, mapEdges);
    }

    /// <summary>
    /// Schuin omhoog mag, zolang het geen bestaand pad kruist. Rechtdoor kruist nooit.
    /// </summary>
    private static int NextColumn(SeededRng rng, HashSet<((int, int), (int, int))> edges, int row, int col)
    {
        List<int> options = [col - 1, col, col + 1];
        rng.Shuffle(options);
        foreach (int next in options)
        {
            if (next < 0 || next >= Columns) continue;
            bool crosses = next != col && edges.Contains(((row, next), (row + 1, col)));
            if (!crosses) return next;
        }
        return col;
    }

    private static NodeKind RollKind(SeededRng rng, int row, List<NodeKind> parentKinds)
    {
        var weights = row < FirstEliteRow ? EarlyWeights : LateWeights;
        int total = weights.Sum(w => w.Weight);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            int roll = rng.NextInt(total);
            NodeKind kind = weights.First(w => (roll -= w.Weight) < 0).Kind;

            if (NoRepeat.Contains(kind) && parentKinds.Contains(kind)) continue;
            // Geen rustvuur vlak voor de rij met rustvuren
            if (kind == NodeKind.Rest && row == RestRow - 1) continue;
            return kind;
        }
        return NodeKind.Fight;
    }

    private static string? PickEncounter(SeededRng rng, ActDefinition act, NodeKind kind, int row, List<string?> parentEncounters)
    {
        IReadOnlyList<string> pool = kind switch
        {
            NodeKind.Fight => row <= 1 ? act.EasyPool : act.NormalPool,
            NodeKind.Elite => act.ElitePool,
            // Vroeg in act 1 is een onbekende knoop de kruik: het wondermoment
            NodeKind.Event => row < FirstEliteRow && act.EarlyEvent is { } early ? [early] : act.Events,
            _ => []
        };
        if (pool.Count == 0) return null;

        // Liefst niet dezelfde vijand of hetzelfde event twee keer na elkaar
        string pick = pool[rng.NextInt(pool.Count)];
        for (int attempt = 0; attempt < 4 && parentEncounters.Contains(pick); attempt++)
        {
            pick = pool[rng.NextInt(pool.Count)];
        }
        return pick;
    }
}
