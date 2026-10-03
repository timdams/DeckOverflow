using System.Text.Json.Serialization;
using DeckOverflow.Web.World;

namespace DeckOverflow.Web.Progress;

/// <summary>De drie toestanden van een onderdeel op de onderdelenlijst (docs/wereld/README.md, mastery).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PartState>))]
public enum PartState { InBag, Unpacked, Assembled }

/// <summary>Hoe een afdeling openging: door de baas van de vorige, via het vangnet, of omdat de docent ze vrijgaf.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<UnlockHow>))]
public enum UnlockHow { Boss, SafetyNet, Teacher }

public sealed record Unlock(UnlockHow How, DateTimeOffset At);

/// <summary>
/// Alles wat over runs heen blijft. Geen spelregel maar meta-voortgang, dus het staat in de shell.
/// Een onderdeel is een Codex-pagina: uitgepakt zodra de pagina open is, en wat uitgepakt is,
/// gaat nooit meer terug in de zak.
/// </summary>
public sealed class PlayerProgress
{
    /// <summary>Open Codex-pagina's, met de getallen van het eerste moment.</summary>
    public Dictionary<string, IReadOnlyDictionary<string, string>> Codex { get; init; } = [];

    /// <summary>Hoe ver elke Codex-pagina gelezen is.</summary>
    public Dictionary<string, int> CodexRead { get; init; } = [];

    /// <summary>Gemonteerde onderdelen. Gemonteerd: het moment in een gewonnen gevecht in drie verschillende runs, of de elite van het concept verslaan (docs/wereld/README.md, mastery). Nog niet gebouwd.</summary>
    public HashSet<string> Assembled { get; init; } = [];

    /// <summary>Ontgrendelde afdelingen. De Card Hall staat altijd open en hoort hier niet in.</summary>
    public Dictionary<string, Unlock> Unlocks { get; init; } = [];

    /// <summary>Verdiende ✗-panelen.</summary>
    public HashSet<string> XPanels { get; init; } = [];

    /// <summary>Onderdelen die nergens voor dienen, uit kisten (een tease, <see cref="CardHall.Runs.Trinkets"/>).</summary>
    public HashSet<string> Trinkets { get; init; } = [];

    /// <summary>De hoogste act die je ooit bereikte: de startpunten.</summary>
    public int ReachedAct { get; set; } = 1;

    public int RunsStarted { get; set; }

    public bool IntroSeen { get; set; }

    /// <summary>Waarom de onthulling gebeurde (<c>won</c>, <c>crack</c>), of leeg als ze nog niet gebeurde.</summary>
    public string? RevealedBy { get; set; }

    [JsonIgnore] public bool Revealed => RevealedBy is not null;

    [JsonIgnore] public bool CardHallCleared => Unlocks.ContainsKey(Departments.ControlRoom);

    public PartState StateOf(string part) =>
        Assembled.Contains(part) ? PartState.Assembled
        : Codex.ContainsKey(part) ? PartState.Unpacked
        : PartState.InBag;

    /// <summary>Een afdeling ontgrendelen. De eerste manier blijft staan.</summary>
    public bool TryUnlock(string department, UnlockHow how, DateTimeOffset at) =>
        Unlocks.TryAdd(department, new Unlock(how, at));
}
