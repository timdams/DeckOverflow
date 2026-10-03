using System.Net.Http.Json;
using DeckOverflow.Engine.Maps;

namespace DeckOverflow.Web.Art;

/// <summary>
/// Welk plaatje bij welke kaart, relic, knoop of scène hoort. De koppeling staat in
/// <c>wwwroot/art/art.json</c>; de stage leest hetzelfde bestand. Iconen zonder koppeling
/// (hp, goud, ...) hebben een vaste naam in <c>art/icons/</c>. Geen spelregels: dit kiest
/// alleen een bestand.
/// </summary>
public sealed class ArtStyle
{
    private Manifest _manifest = new([], [], [], [], []);

    public bool Loaded { get; private set; }

    public async Task LoadAsync(HttpClient http)
    {
        _manifest = await http.GetFromJsonAsync<Manifest>("art/art.json") ?? _manifest;
        Loaded = true;
    }

    /// <summary>"strike+" toont hetzelfde plaatje als "strike".</summary>
    public string? Card(string id) => Url(_manifest.Cards, id.TrimEnd('+'));

    public string? Relic(string id) => Url(_manifest.Relics, id);

    public string? Node(NodeKind kind) => Url(_manifest.Nodes, kind.ToString().ToLowerInvariant());

    public string? Scene(string key) => Url(_manifest.Scenes, key);

    /// <summary>De tekening bij een ✗-paneel.</summary>
    public string? Panel(string key) => Url(_manifest.Panels ?? [], key);

    public static string Icon(string name) => $"art/icons/{name}.png";

    private static string? Url(Dictionary<string, string> map, string key) =>
        map.TryGetValue(key, out string? file) ? $"art/{file}.png" : null;

    private sealed record Manifest(
        Dictionary<string, string> Cards,
        Dictionary<string, string> Relics,
        Dictionary<string, string> Nodes,
        Dictionary<string, string> Scenes,
        Dictionary<string, string>? Panels);
}
