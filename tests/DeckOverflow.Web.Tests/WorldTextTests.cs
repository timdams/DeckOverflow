using System.Text.Json;
using DeckOverflow.Web.World;

namespace DeckOverflow.Web.Tests;

/// <summary>
/// De wereld spreekt elke taal: elke afdeling op de plattegrond en elk ✗-paneel heeft zijn tekst in
/// <c>nl.json</c> en <c>en.json</c>. De panelen van de afdelingen zelf testen hun eigen testprojecten;
/// hier staan de wereldpanelen en wat de afdelingen samen moeten kloppen.
/// </summary>
public class WorldTextTests
{
    private static readonly string TextDir = Path.Combine(FindRoot(), "src", "DeckOverflow.Web", "wwwroot", "text");

    public static TheoryData<string> Languages => new() { "nl", "en" };

    private static Dictionary<string, string> Load(string language) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(TextDir, $"{language}.json")))!;

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DeckOverflow.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("DeckOverflow.sln niet gevonden.");
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Elke_afdeling_heeft_een_naam_een_genre_en_een_uitleg(string language)
    {
        var texts = Load(language);
        foreach (var d in Departments.All)
        {
            Assert.Contains($"ui.world.{d.Key}.name", texts.Keys);
            Assert.Contains($"ui.world.{d.Key}.genre", texts.Keys);
            Assert.Contains($"ui.world.{d.Key}.text", texts.Keys);
        }
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Een_gebouwde_afdeling_met_een_slot_zegt_waar_de_sleutel_ligt(string language)
    {
        var texts = Load(language);
        Assert.All(Departments.All.Where(d => d.Key != Departments.CardHall && d.Availability != Availability.Someday),
            d => Assert.Contains($"ui.world.{d.Key}.needs", texts.Keys));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void Elk_paneel_van_de_fabriek_heeft_een_naam_en_een_uitleg(string language)
    {
        var texts = Load(language);
        Assert.All(XPanels.All, p =>
        {
            Assert.Contains($"xpanel.{p.Key}.name", texts.Keys);
            Assert.Contains($"xpanel.{p.Key}.text", texts.Keys);
        });
    }

    [Fact]
    public void Een_paneel_hoort_bij_een_afdeling_van_de_plattegrond_of_bij_de_wereld()
    {
        Assert.All(XPanels.Sources, s =>
            Assert.True(s.Department == "world" || Departments.All.Any(d => d.Key == s.Department), s.Department));
    }

    [Fact]
    public void De_sleutel_van_een_paneel_is_uniek_over_de_hele_fabriek()
    {
        var keys = XPanels.All.Select(p => p.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }
}
