using System.Text.Json.Serialization;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Runs;
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

    /// <summary>
    /// Afdelingen die op de plattegrond al uit hun doos klapten (docs/wereld/README.md, Uit de doos): dat moment
    /// gebeurt één keer per afdeling, de eerste keer dat ze voor jou opengaat.
    /// </summary>
    public HashSet<string> Unboxed { get; init; } = [];

    /// <summary>Verdiende ✗-panelen.</summary>
    public HashSet<string> XPanels { get; init; } = [];

    /// <summary>Onderdelen die nergens voor dienen, uit kisten (een tease, <see cref="CardHall.Runs.Trinkets"/>).</summary>
    public HashSet<string> Trinkets { get; init; } = [];

    /// <summary>Per gevecht in de Controlekamer: je beste score en het bord waaraan je laatst werkte.</summary>
    public Dictionary<string, ControlRoomRecord> ControlRoom { get; init; } = [];

    /// <summary>Per bestelling op de Lopende Band: je beste score en het bord waaraan je laatst werkte.</summary>
    public Dictionary<string, ConveyorBeltRecord> ConveyorBelt { get; init; } = [];

    /// <summary>De hoogste act die je ooit bereikte: de startpunten.</summary>
    public int ReachedAct { get; set; } = 1;

    public int RunsStarted { get; set; }

    public bool IntroSeen { get; set; }

    /// <summary>
    /// De Prikklok: je eerste voltooide dagelijkse run van de laatste dag dat je er een uitspeelde. Alleen die
    /// telt; wie daarna opnieuw speelt, prikt niet meer (D-006). Leeg tot je de eerste uitspeelt.
    /// </summary>
    public PunchCard? Punch { get; set; }

    /// <summary>Waarom de onthulling gebeurde (<c>won</c>, <c>crack</c>), of leeg als ze nog niet gebeurde.</summary>
    public string? RevealedBy { get; set; }

    [JsonIgnore] public bool Revealed => RevealedBy is not null;

    [JsonIgnore] public bool CardHallCleared => Unlocks.ContainsKey(Departments.ControlRoom);

    public PartState StateOf(string part) =>
        Assembled.Contains(part) ? PartState.Assembled
        : Codex.ContainsKey(part) ? PartState.Unpacked
        : PartState.InBag;

    /// <summary>
    /// Een dagelijkse run is uitgespeeld. De eerste van de dag prikt; een volgende telt niet meer (D-006). Een run
    /// met de sneltoets W prikt nooit, en de server zou ze toch afwijzen. Alleen wat prikt, verandert <see cref="Punch"/>.
    /// </summary>
    public PunchOutcome TryPunch(DateOnly day, int score, IReadOnlyList<ICommand> commands)
    {
        if (commands.Any(c => c is DebugWin)) return PunchOutcome.DebugWin;
        if (Punch?.Date == day) return PunchOutcome.Again;
        Punch = new PunchCard { Date = day, Score = score, Commands = DailyRun.CommandsToJson(commands) };
        return PunchOutcome.Counted;
    }

    /// <summary>Een afdeling ontgrendelen. De eerste manier blijft staan.</summary>
    public bool TryUnlock(string department, UnlockHow how, DateTimeOffset at) =>
        Unlocks.TryAdd(department, new Unlock(how, at));
}

/// <summary>Hoe een uitgespeelde dagelijkse run uitkwam bij de Prikklok.</summary>
public enum PunchOutcome { Counted, Again, DebugWin }

/// <summary>
/// Eén geprikte dagelijkse run van de Kaartenhal. De commandolijst blijft bewaard tot de score verstuurd is,
/// zodat een haperend schoolnetwerk niets kost: ze gaat mee bij de volgende kans.
/// </summary>
public sealed class PunchCard
{
    /// <summary>De UTC-datum van de seed, niet die van het einde: een run kan over middernacht lopen.</summary>
    public DateOnly Date { get; init; }
    public int Score { get; init; }
    /// <summary>De commandolijst als JSON (<c>DailyRun.CommandsToJson</c>), leeg zodra ze verstuurd is.</summary>
    public string? Commands { get; set; }
    public bool Sent { get; set; }
}

/// <summary>
/// Eén gevecht in de Controlekamer. Beurten en regels zijn aparte scores, zoals in Opus Magnum:
/// je beste aantal beurten hoeft niet van hetzelfde bord te komen als je beste aantal regels.
/// </summary>
public sealed class ControlRoomRecord
{
    public int? BestTurns { get; set; }
    public int? BestRules { get; set; }
    public List<DeckOverflow.ControlRoom.Gambits.Rule> Board { get; set; } = [];

    public bool Beaten => BestTurns is not null;
}

/// <summary>
/// Eén bestelling op de Lopende Band. Machines en tikken zijn aparte scores, zoals in Opus Magnum.
/// Het bord staat als JSON per stuk, want een stuk kan een band, machine, poort of teller zijn.
/// </summary>
public sealed class ConveyorBeltRecord
{
    public int? BestMachines { get; set; }
    public int? BestCycles { get; set; }
    public List<BeltPiece> Board { get; set; } = [];

    public bool Beaten => BestMachines is not null;
}

/// <summary>Eén stuk op het bord van de Lopende Band: waar het ligt, en wat het is.</summary>
public sealed record BeltPiece(int X, int Y, string Json);
