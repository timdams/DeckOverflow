namespace DeckOverflow.Engine.Relics;

public sealed record RelicDefinition(string Id, string Name, string Text);

/// <summary>
/// Relics van spike 3. De regels zelf zitten in <see cref="Combat.Combat"/> en <see cref="Runs.Run"/>;
/// hier staan alleen naam en tekst.
/// </summary>
public static class RelicCatalog
{
    public const string Teller = "teller";
    public const string VlottendeKomma = "vlottende-komma";
    public const string Restzak = "restzak";
    public const string Ankervat = "ankervat";
    public const string GrotePot = "grote-pot";

    /// <summary>Restzak vuurt af zodra er zoveel afgekapt is.</summary>
    public const int RestzakThreshold = 3;

    /// <summary>Wat Ankervat aan blok geeft bij de start van elk gevecht.</summary>
    public const int AnkervatBlock = 6;

    /// <summary>Wat Grote Pot aan max HP geeft.</summary>
    public const int GrotePotMaxHp = 8;

    public static readonly IReadOnlyList<RelicDefinition> All =
    [
        new(Teller, "Teller", "Elke derde kaart die je in een gevecht speelt, kost 0."),
        new(VlottendeKomma, "Vlottende Komma", "Je eerste Vlottende kaart in elk gevecht kost 0."),
        new(Restzak, "Restzak", $"Bewaart wat er van jouw schade afgekapt wordt. Bij {RestzakThreshold} vuurt hij {RestzakThreshold} schade af."),
        new(Ankervat, "Ankervat", $"Begin elk gevecht met {AnkervatBlock} blok."),
        new(GrotePot, "Grote Pot", $"+{GrotePotMaxHp} max HP."),
    ];

    public static RelicDefinition Get(string id) =>
        All.FirstOrDefault(r => r.Id == id) ?? throw new ArgumentException($"Onbekende relic: {id}", nameof(id));
}
