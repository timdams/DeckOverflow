using System.Globalization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace DeckOverflow.Web.Text;

/// <summary>
/// Alle spelteksten komen uit <c>wwwroot/text/en.json</c>, zoals in het spel.
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

    /// <summary>Een tekst met benoemde waarden: <c>T("log.whack", ("damage", 6))</c>.</summary>
    public string T(string key, params (string Name, object Value)[] args)
    {
        var values = args.ToDictionary(a => a.Name, a => Convert.ToString(a.Value, CultureInfo.InvariantCulture) ?? "");
        return Placeholder().Replace(Raw(key), m => values.GetValueOrDefault(m.Groups[1].Value) ?? m.Value);
    }

    public bool Has(string key) => _texts.ContainsKey(key);

    private string Raw(string key) => _texts.GetValueOrDefault(key) ?? key;

    [GeneratedRegex(@"\{([a-zA-Z]+)\}")]
    private static partial Regex Placeholder();
}
