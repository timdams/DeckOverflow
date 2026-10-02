using System.Net.Http.Json;
using DeckOverflow.Engine.Maps;

namespace DeckOverflow.Web.Art;

/// <summary>
/// Welke tekenstijl actief is en welk plaatje bij welke kaart, relic of knoop hoort.
/// De koppeling staat in <c>wwwroot/art/art.json</c>; de stage leest hetzelfde bestand.
/// Geen spelregels: dit kiest alleen een bestand.
/// </summary>
public sealed class ArtStyle
{
    private Manifest _manifest = new([], [], [], [], []);

    public bool Loaded { get; private set; }

    public string Current { get; private set; } = "";

    public IReadOnlyList<string> Styles => _manifest.Styles;

    /// <summary>Pixelart mag de browser niet wazig opschalen.</summary>
    public bool Pixelated => IsPixelated(Current);

    public bool IsPixelated(string style) => _manifest.Pixelated.Contains(style);

    public async Task LoadAsync(HttpClient http)
    {
        _manifest = await http.GetFromJsonAsync<Manifest>("art/art.json") ?? _manifest;
        Current = _manifest.Styles.FirstOrDefault() ?? "";
        Loaded = true;
    }

    /// <summary>Kiest een stijl; een onbekende naam (oude localStorage) laat alles zoals het is.</summary>
    public void Use(string? style)
    {
        if (style is null || style == Current || !_manifest.Styles.Contains(style)) return;
        Current = style;
    }

    /// <summary>"strike+" toont hetzelfde plaatje als "strike".</summary>
    public string? Card(string id) => Url(_manifest.Cards, id.TrimEnd('+'));

    public string? Relic(string id) => Url(_manifest.Relics, id);

    public string? Node(NodeKind kind) => Url(_manifest.Nodes, kind.ToString().ToLowerInvariant());

    private string? Url(Dictionary<string, string> map, string key) =>
        map.TryGetValue(key, out string? file) ? $"art/{Current}/{file}.png" : null;

    private sealed record Manifest(
        List<string> Styles,
        List<string> Pixelated,
        Dictionary<string, string> Cards,
        Dictionary<string, string> Relics,
        Dictionary<string, string> Nodes);
}
