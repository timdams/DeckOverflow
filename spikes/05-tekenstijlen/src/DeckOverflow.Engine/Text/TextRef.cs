namespace DeckOverflow.Engine.Text;

/// <summary>
/// Een speltekst zonder taal: een sleutel uit <c>wwwroot/text/en.json</c> plus de waarden die erin komen.
/// De motor kent geen spelteksten; de shell en de stage zoeken ze op.
/// </summary>
/// <param name="Args">
/// Waarden voor de plaatshouders. In de tekst betekent <c>{amount}</c> de waarde zelf,
/// <c>{card:from}</c> de naam van de kaart met die id, en zo ook <c>{relic:…}</c> en <c>{enemy:…}</c>.
/// </param>
public sealed record TextRef(string Key, IReadOnlyDictionary<string, string>? Args = null)
{
    public static TextRef Of(string key, params (string Name, object Value)[] args) =>
        new(key, args.Length == 0 ? null : args.ToDictionary(a => a.Name, a => Format(a.Value)));

    /// <summary>Gelijk als sleutel en waarden gelijk zijn, zodat snapshots als waarde vergelijken.</summary>
    public bool Equals(TextRef? other) =>
        other is not null && Key == other.Key
        && (Args ?? Empty).Count == (other.Args ?? Empty).Count
        && (Args ?? Empty).All(a => other.Args!.TryGetValue(a.Key, out string? v) && v == a.Value);

    public override int GetHashCode() => Key.GetHashCode();

    private static readonly Dictionary<string, string> Empty = [];

    private static string Format(object value) => value switch
    {
        double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
        _ => value.ToString() ?? ""
    };
}
