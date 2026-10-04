using System.Text.Json;
using DeckOverflow.ControlRoom.Achievements;
using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.ControlRoom.Levels;

namespace DeckOverflow.ControlRoom.Tests;

/// <summary>
/// De teksten van de Controlekamer, in elke taal: elk gevecht, elke check en zet, elk paneel en elke echte bug.
/// De sleutels die letterlijk in de shell staan, controleert <c>StringsTests</c> van de Card Hall al voor de hele shell.
/// </summary>
public class StringsTests
{
    private static readonly string TextFolder = Path.Combine(FindRepoRoot(), "src", "DeckOverflow.Web", "wwwroot", "text");

    public static TheoryData<string> Languages => new("nl", "en");

    private static Dictionary<string, string> Load(string language) =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(TextFolder, $"{language}.json")))!;

    private static IEnumerable<string> Keys()
    {
        foreach (var level in LevelCatalog.All)
        {
            yield return $"room.enemy.{level.Key}";
            yield return $"room.flavor.{level.Key}";
            // De Codex-pagina's van de Card Hall (afkappen, overflow) noemen het doelwit met enemy.<key>
            yield return $"enemy.{level.Key}";
            if (level.Bug)
            {
                yield return $"bug.{level.Key}.title";
                yield return $"bug.{level.Key}.story";
            }
        }
        foreach (var check in Enum.GetValues<Check>())
        {
            yield return $"room.check.{check}";
            yield return $"room.pick.{check}";
        }
        foreach (var move in Enum.GetValues<Move>())
        {
            yield return $"room.move.{move}";
            yield return $"room.move-help.{move}";
        }
        foreach (string who in new[] { "player", "enemy" })
            foreach (string what in new[] { "none", "whack", "whack-charged", "brace", "repair", "repair-empty", "wind-up", "wind-up-again", "overflow", "counter", "skipped" })
                yield return $"room.log.{who}.{what}";
        foreach (var panel in XRegister.All)
        {
            yield return $"xpanel.{panel.Key}.name";
            yield return $"xpanel.{panel.Key}.text";
        }
    }

    [Theory, MemberData(nameof(Languages))]
    public void Elke_tekst_van_de_Controlekamer_bestaat(string language)
    {
        var texts = Load(language);
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
