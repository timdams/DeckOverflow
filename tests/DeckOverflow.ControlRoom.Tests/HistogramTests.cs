using System.Text.Json;
using DeckOverflow.ControlRoom.Levels;

namespace DeckOverflow.ControlRoom.Tests;

/// <summary>
/// <c>wwwroot/control-room/solutions.json</c> is het histogram van alle winnende borden per gevecht. Verandert een
/// gevecht, dan verandert het histogram: zet <c>DECKOVERFLOW_WRITE_SOLUTIONS=1</c> en draai deze test opnieuw.
/// </summary>
public class HistogramTests
{
    private static readonly string File = Path.Combine(FindRepoRoot(), "src", "DeckOverflow.Web", "wwwroot", "control-room", "solutions.json");

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Het_histogram_van_alle_oplossingen_is_bij()
    {
        var expected = JsonSerializer.Serialize(LevelCatalog.All.ToDictionary(l => l.Key, Histogram.Of), Options);

        if (Environment.GetEnvironmentVariable("DECKOVERFLOW_WRITE_SOLUTIONS") == "1")
            System.IO.File.WriteAllText(File, expected + "\n");

        Assert.True(System.IO.File.Exists(File), "solutions.json ontbreekt: draai met DECKOVERFLOW_WRITE_SOLUTIONS=1");
        Assert.Equal(expected, System.IO.File.ReadAllText(File).TrimEnd('\n').ReplaceLineEndings("\n").TrimEnd(),
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void Elk_gevecht_heeft_een_histogram_met_winnende_borden()
    {
        Assert.All(LevelCatalog.All, l => Assert.True(Histogram.Of(l).Total > 0, l.Key));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !System.IO.File.Exists(Path.Combine(dir.FullName, "DeckOverflow.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("DeckOverflow.sln niet gevonden.");
    }
}
