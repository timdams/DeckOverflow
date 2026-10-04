using System.Text.Json;
using System.Text.RegularExpressions;
using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Relics;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Core.Text;

namespace DeckOverflow.Tests;

/// <summary>
/// Alle spelteksten staan in <c>wwwroot/text/en.json</c>, met een vertaling per taal ernaast
/// (<c>nl.json</c>). Deze tests vangen een sleutel die ontbreekt of een getal dat niet in de
/// tekst geraakt, voor een speler het ziet.
/// </summary>
public partial class StringsTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly Dictionary<string, string> Texts = JsonSerializer.Deserialize<Dictionary<string, string>>(
        File.ReadAllText(Path.Combine(RepoRoot, "src", "DeckOverflow.Web", "wwwroot", "text", "en.json")))!;

    [Fact]
    public void Elke_kaart_heeft_een_naam()
    {
        Assert.All(CardCatalog.Everything(), c => AssertKey($"card.{c.BaseId}"));
    }

    [Fact]
    public void Elke_kaarttekst_bestaat_en_krijgt_al_haar_getallen()
    {
        Assert.All(CardCatalog.Everything(), c => AssertFilled(CardText.Of(c)));
    }

    [Fact]
    public void Elke_vijand_en_de_speler_hebben_een_naam()
    {
        Assert.All(Bestiary.All.Append(Scenarios.PlayerKey), key => AssertKey($"enemy.{key}"));
    }

    [Fact]
    public void Elke_relic_heeft_een_naam_en_een_volledige_tekst()
    {
        foreach (string id in RelicCatalog.All)
        {
            AssertKey($"relic.{id}");
            AssertFilled(RelicCatalog.Create(id).Text);
        }
    }

    [Fact]
    public void Elke_soort_knoop_en_elk_event_heeft_een_naam()
    {
        Assert.All(Enum.GetValues<NodeKind>(), k => AssertKey($"node.{k.ToString().ToLowerInvariant()}"));
        foreach (string key in Adventures.All.Append(Adventures.Foundry))
        {
            AssertKey($"event.{key}.title");
            AssertKey($"event.{key}.text");
        }
    }

    [Fact]
    public void Elk_x_paneel_heeft_een_naam_en_een_uitleg()
    {
        foreach (var panel in CardHall.Achievements.XRegister.All)
        {
            AssertKey($"xpanel.{panel.Key}.name");
            AssertKey($"xpanel.{panel.Key}.text");
        }
    }

    [Fact]
    public void Elke_codexpagina_heeft_een_naam_een_verhaal_en_code()
    {
        foreach (var entry in Core.Codex.CodexCatalog.All)
        {
            AssertKey($"codex.{entry.Key}.name");
            AssertKey($"codex.{entry.Key}.happened");
            AssertKey($"codex.{entry.Key}.code");
        }
    }

    /// <summary>
    /// Elke sleutel die letterlijk in de code staat, in motor, shell of stage, moet in en.json staan.
    /// Zo vang je ook weigeringen en knoppen die geen andere test raakt.
    /// </summary>
    [Fact]
    public void Elke_sleutel_in_de_code_staat_in_en_json()
    {
        string sep = Path.DirectorySeparatorChar.ToString();
        string[] folders = [Path.Combine(RepoRoot, "src", "DeckOverflow.CardHall"), Path.Combine(RepoRoot, "src", "DeckOverflow.Web")];
        var files = folders
            .SelectMany(f => Directory.EnumerateFiles(f, "*.*", SearchOption.AllDirectories))
            .Where(f => f.EndsWith(".cs") || f.EndsWith(".razor") || f.EndsWith(".js"))
            .Where(f => !f.Contains($"{sep}obj{sep}") && !f.Contains($"{sep}bin{sep}") && !f.Contains($"{sep}lib{sep}"));

        var keys = files
            .SelectMany(f => KeyLiteral().Matches(File.ReadAllText(f)).Select(m => (File: Path.GetFileName(f), Key: m.Groups["key"].Value)))
            .Distinct()
            .ToList();

        Assert.NotEmpty(keys);
        var missing = keys.Where(k => !Texts.ContainsKey(k.Key)).Select(k => $"{k.Key} ({k.File})").ToList();
        Assert.True(missing.Count == 0, "Ontbreekt in en.json: " + string.Join(", ", missing));
    }

    /// <summary>
    /// Elke taal heeft dezelfde sleutels als en.json: een nieuwe tekst schrijf je in alle talen,
    /// en een sleutel die nergens meer gebruikt wordt, verdwijnt overal.
    /// </summary>
    [Theory]
    [InlineData("nl")]
    public void Elke_taal_heeft_dezelfde_sleutels_als_en_json(string language)
    {
        var other = LoadTexts(language);
        var missing = Texts.Keys.Except(other.Keys).ToList();
        var extra = other.Keys.Except(Texts.Keys).ToList();
        Assert.True(missing.Count == 0, $"Ontbreekt in {language}.json: " + string.Join(", ", missing));
        Assert.True(extra.Count == 0, $"Staat in {language}.json maar niet in en.json: " + string.Join(", ", extra));
    }

    /// <summary>Een vertaling die een plaatshouder verliest of verzint, toont een getal niet of een sleutel wel.</summary>
    [Theory]
    [InlineData("nl")]
    public void Elke_vertaling_heeft_dezelfde_plaatshouders(string language)
    {
        var wrong = LoadTexts(language)
            .Where(t => Texts.TryGetValue(t.Key, out string? en) && !Placeholders(en).SetEquals(Placeholders(t.Value)))
            .Select(t => t.Key)
            .ToList();
        Assert.True(wrong.Count == 0, $"Andere plaatshouders in {language}.json: " + string.Join(", ", wrong));
    }

    /// <summary>
    /// In H2 tot H5 heet het crashen, zoals in het boek; het woord exception komt pas in H10. Alleen de Codex-pagina
    /// van dat hoofdstuk mag het noemen (beslist op 4 oktober 2026).
    /// </summary>
    [Theory]
    [InlineData("nl")]
    [InlineData("en")]
    public void Geen_speltekst_noemt_een_exception_voor_H10(string language)
    {
        var texts = language == "en" ? Texts : LoadTexts(language);
        var wrong = texts
            .Where(t => !t.Key.StartsWith("codex.exceptions.") && t.Value.Contains("exception", StringComparison.OrdinalIgnoreCase))
            .Select(t => t.Key)
            .ToList();
        Assert.True(wrong.Count == 0, $"Noemt een exception in {language}.json: " + string.Join(", ", wrong));
    }

    private static Dictionary<string, string> LoadTexts(string language) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(
            File.ReadAllText(Path.Combine(RepoRoot, "src", "DeckOverflow.Web", "wwwroot", "text", $"{language}.json")))!;

    private static HashSet<string> Placeholders(string text) =>
        Placeholder().Matches(text).Select(m => m.Value).ToHashSet();

    private static void AssertKey(string key) =>
        Assert.True(Texts.ContainsKey(key), $"Ontbreekt in en.json: {key}");

    /// <summary>De sleutel bestaat en elke plaatshouder in de tekst krijgt een waarde.</summary>
    private static void AssertFilled(TextRef text)
    {
        AssertKey(text.Key);
        foreach (Match m in Placeholder().Matches(Texts[text.Key]))
        {
            string name = m.Groups["name"].Value;
            Assert.True(text.Args?.ContainsKey(name) == true, $"{text.Key}: geen waarde voor {{{name}}}");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeckOverflow.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("DeckOverflow.sln niet gevonden.");
    }

    [GeneratedRegex("""["'](?<key>(?:card|effect|enemy|relic|node|event|rest|reject|ui|stage|room|belt)\.[a-z0-9.\-]+)["']""")]
    private static partial Regex KeyLiteral();

    [GeneratedRegex(@"\{(?:(?:card|relic|enemy):)?(?<name>[a-zA-Z]+)\}")]
    private static partial Regex Placeholder();
}
