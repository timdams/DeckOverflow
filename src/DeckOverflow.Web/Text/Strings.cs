using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using DeckOverflow.Engine.Text;

namespace DeckOverflow.Web.Text;

/// <summary>
/// Alle spelteksten komen uit <c>wwwroot/text/en.json</c>. De motor geeft sleutels en getallen,
/// hier worden het zinnen. De stage doet hetzelfde in <c>stage/strings.js</c>.
/// Een ontbrekende sleutel toont zichzelf, zodat je hem meteen ziet staan.
/// </summary>
public sealed partial class Strings
{
    private Dictionary<string, string> _texts = [];

    public bool Loaded { get; private set; }

    public async Task LoadAsync(HttpClient http)
    {
        _texts = await http.GetFromJsonAsync<Dictionary<string, string>>("text/en.json") ?? [];
        Loaded = true;
    }

    /// <summary>Een vaste tekst met benoemde waarden: <c>T("ui.floor", ("floor", 3))</c>.</summary>
    public string T(string key, params (string Name, object Value)[] args) =>
        Format(key, args.ToDictionary(a => a.Name, a => Convert.ToString(a.Value, CultureInfo.InvariantCulture) ?? ""));

    public string T(TextRef text) => Format(text.Key, text.Args);

    /// <summary>"strike+" heet "Strike+": de naam hangt aan de id zonder plus.</summary>
    public string Card(string id) => Raw($"card.{id.TrimEnd('+')}") + (id.EndsWith('+') ? "+" : "");

    public string Relic(string id) => Raw($"relic.{id}");

    /// <summary>Bestaat er een tekst voor deze sleutel? Voor optionele teksten, zoals het verhaal achter een bug.</summary>
    public bool Has(string key) => _texts.ContainsKey(key);

    public string Enemy(string key) => Raw($"enemy.{key}");

    private string Raw(string key) => _texts.GetValueOrDefault(key) ?? key;

    private string Format(string key, IReadOnlyDictionary<string, string>? args) =>
        Placeholder().Replace(Raw(key), m =>
        {
            string kind = m.Groups["kind"].Value;
            string name = m.Groups["name"].Value;
            if (args is null || !args.TryGetValue(name, out string? value)) return m.Value;
            return kind switch
            {
                "card" => Card(value),
                "relic" => Relic(value),
                "enemy" => Enemy(value),
                _ => value
            };
        });

    [GeneratedRegex(@"\{(?:(?<kind>card|relic|enemy):)?(?<name>[a-zA-Z]+)\}")]
    private static partial Regex Placeholder();
}
