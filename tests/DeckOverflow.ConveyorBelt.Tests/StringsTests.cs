using System.Text.Json;
using DeckOverflow.ConveyorBelt.Achievements;
using DeckOverflow.ConveyorBelt.Belts;

namespace DeckOverflow.ConveyorBelt.Tests;

/// <summary>
/// De teksten van de Lopende Band, in elke taal: elke bestelling, elk product, elk paneel en de echte bug.
/// De sleutels die letterlijk in de shell staan, controleert <c>StringsTests</c> van de Card Hall al voor de hele shell.
/// </summary>
public class StringsTests
{
    private static readonly string TextFolder = Path.Combine(FindRepoRoot(), "src", "DeckOverflow.Web", "wwwroot", "text");

    public static TheoryData<string> Languages => new("nl", "en");

    private static IEnumerable<string> Keys()
    {
        foreach (var level in LevelCatalog.All)
        {
            yield return $"belt.level.{level.Key}.name";
            yield return $"belt.level.{level.Key}.brief";
            // Het product is een vijand uit de Card Hall; de Codex noemt hem met enemy.<key>
            yield return $"enemy.{level.Product}";
            if (level.Bug)
            {
                yield return $"bug.{level.Key}.title";
                yield return $"bug.{level.Key}.story";
            }
        }
        foreach (var kind in Enum.GetValues<OfferKind>()) yield return $"belt.ui.offer.{kind}";
        foreach (var piece in new[] { "Belt", "Machine", "Gate", "Counter" }) yield return $"belt.ui.piece.{piece}";
        foreach (var ending in Enum.GetValues<Ending>()) yield return $"belt.ui.ending.{ending}";
        foreach (var panel in XRegister.All)
        {
            yield return $"xpanel.{panel.Key}.name";
            yield return $"xpanel.{panel.Key}.text";
        }
    }

    [Theory, MemberData(nameof(Languages))]
    public void Elke_tekst_van_de_Lopende_Band_bestaat(string language)
    {
        var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(TextFolder, $"{language}.json")))!;
        var missing = Keys().Where(k => !texts.ContainsKey(k)).ToList();
        Assert.True(missing.Count == 0, $"Ontbreekt in {language}.json: " + string.Join(", ", missing));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeckOverflow.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("DeckOverflow.sln niet gevonden.");
    }
}
