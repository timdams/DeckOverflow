using DeckOverflow.Web.Progress;

namespace DeckOverflow.Web.World;

/// <summary>
/// Hoe ver een afdeling is voor spelers. <see cref="Released"/>: ontgrendel je zelf. <see cref="Soon"/>:
/// gebouwd en open voor de superuser, maar voor spelers nog dicht, ook als je de sleutel al verdiende; op de
/// plattegrond "binnenkort". <see cref="Someday"/>: nog niet gebouwd, "ooit".
/// </summary>
public enum Availability { Released, Soon, Someday }

/// <summary>
/// Een afdeling op de fabrieksplattegrond: een blok hoofdstukken met een eigen spelvorm.
/// Geen spelregel maar meta-voortgang, dus het staat in de shell. Naam en uitleg staan in
/// <c>nl.json</c> en <c>en.json</c> onder <c>ui.world.&lt;key&gt;</c>.
/// </summary>
/// <param name="Step">Het nummer op de plattegrond, zoals een stap in een handleiding: het eerste hoofdstuk.</param>
/// <param name="Chapters">De hoofdstukken van Zie Scherp Scherper die de afdeling dekt.</param>
/// <param name="Url">De pagina van de afdeling, als ze niet op het titelscherm begint. Leeg: de Card Hall of nog niet gebouwd.</param>
/// <param name="Art">Een tekening voor de kaart van de afdeling, of leeg voor een silhouet.</param>
public sealed record Department(string Key, int Step, IReadOnlyList<int> Chapters, Availability Availability, string? Url = null, string? Art = null);

public static class Departments
{
    public const string CardHall = "card-hall";
    public const string ControlRoom = "control-room";
    public const string ConveyorBelt = "conveyor-belt";

    /// <summary>
    /// In de volgorde van het boek. De Controlekamer en de Lopende Band zijn gebouwd maar nog dicht
    /// (beslist op 4 oktober 2026): wie de sleutel verdient, krijgt een tease, en de deur gaat open
    /// zodra ze op <see cref="Availability.Released"/> staan. De superuser kan er al in.
    /// </summary>
    public static readonly IReadOnlyList<Department> All =
    [
        new(CardHall, 2, [2, 3, 4], Availability.Released, Art: "art/departments/card-hall.png"),
        new(ControlRoom, 5, [5], Availability.Soon, Url: "control-room", Art: "art/departments/control-room.png"),
        new(ConveyorBelt, 6, [6], Availability.Soon, Url: "conveyor-belt", Art: "art/departments/conveyor-belt.png"),
        new("tool-wall", 7, [7], Availability.Someday, Art: "art/departments/tool-wall.png"),
        new("warehouse", 8, [8], Availability.Someday, Art: "art/departments/warehouse.png"),
        new("blueprints", 9, [9, 10, 11, 12, 13, 14, 15, 16, 17], Availability.Someday, Art: "art/departments/blueprints.png"),
    ];

    public static Department Get(string key) => All.First(d => d.Key == key);

    /// <summary>
    /// De sleutel is verdiend: de baas van de vorige afdeling viel, of de docent gaf ze vrij. De Controlekamer
    /// verdien je met de laatste baas van de Card Hall. Wie de onthulling via het vangnet kreeg, heeft hem nog niet.
    /// </summary>
    public static bool IsEarned(Department d, IReadOnlyDictionary<string, Unlock> unlocks) =>
        d.Key == CardHall || unlocks.ContainsKey(d.Key);

    /// <summary>
    /// De Card Hall staat altijd open; een andere afdeling als ze vrijgegeven is én de sleutel verdiend is.
    /// Een verdiende sleutel blijft bewaard, dus wie hem nu al heeft, kan meteen binnen zodra de afdeling vrijkomt.
    /// De <see cref="Backend.Superuser"/> mag in alles wat gebouwd is, ook "binnenkort" en zonder sleutel.
    /// </summary>
    public static bool IsOpen(Department d, IReadOnlyDictionary<string, Unlock> unlocks, bool superuser = false) =>
        superuser ? d.Availability != Availability.Someday
        : d.Availability == Availability.Released && IsEarned(d, unlocks);
}
