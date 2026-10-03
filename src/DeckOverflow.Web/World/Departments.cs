using DeckOverflow.Web.Progress;

namespace DeckOverflow.Web.World;

/// <summary>
/// Een afdeling op de fabrieksplattegrond: een blok hoofdstukken met een eigen spelvorm.
/// Geen spelregel maar meta-voortgang, dus het staat in de shell. Naam en uitleg staan in
/// <c>en.json</c> onder <c>ui.world.&lt;key&gt;</c>.
/// </summary>
/// <param name="Step">Het nummer op de plattegrond, zoals een stap in een handleiding: het eerste hoofdstuk.</param>
/// <param name="Chapters">De hoofdstukken van Zie Scherp Scherper die de afdeling dekt.</param>
/// <param name="Url">Waar de afdeling speelbaar is, als dat buiten het spel is (een prototype). Leeg: het spel zelf of nog niet speelbaar.</param>
/// <param name="Art">Een tekening voor de kaart van de afdeling, of leeg voor een silhouet.</param>
public sealed record Department(string Key, int Step, IReadOnlyList<int> Chapters, bool Playable, string? Url = null, string? Art = null);

public static class Departments
{
    public const string CardHall = "card-hall";
    public const string ControlRoom = "control-room";

    /// <summary>In de volgorde van het boek. Alleen de Card Hall en (als prototype) de Controlekamer zijn speelbaar.</summary>
    public static readonly IReadOnlyList<Department> All =
    [
        new(CardHall, 2, [2, 3, 4], Playable: true, Art: "art/wide/title.webp"),
        new(ControlRoom, 5, [5], Playable: true, Url: "spike-8/", Art: "art/actors/colossus.png"),
        new("conveyor-belt", 6, [6], Playable: false),
        new("tool-wall", 7, [7], Playable: false),
        new("warehouse", 8, [8], Playable: false),
        new("blueprints", 9, [9, 10, 11, 12, 13, 14, 15, 16, 17], Playable: false),
    ];

    /// <summary>
    /// De Card Hall staat altijd open; een andere afdeling zodra ze ontgrendeld is. De Controlekamer gaat
    /// open als de laatste baas van de Card Hall valt. Wie de onthulling via het vangnet kreeg, ziet ze nog dicht.
    /// </summary>
    public static bool IsOpen(Department d, IReadOnlyDictionary<string, Unlock> unlocks) =>
        d.Key == CardHall || unlocks.ContainsKey(d.Key);
}
