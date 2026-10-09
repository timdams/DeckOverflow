using System.Text.Json;
using System.Text.Json.Serialization;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.Core.Random;

namespace DeckOverflow.CardHall.Runs;

/// <summary>Wat de scorecontrole van een ingestuurde dagelijkse run vindt.</summary>
public enum ScoreVerdict
{
    Ok,
    /// <summary>De commandolijst is geen geldige lijst commands.</summary>
    Unreadable,
    /// <summary>Opnieuw afgespeeld is de run niet gewonnen of verloren.</summary>
    Unfinished,
    /// <summary>Een gevecht werd gewonnen met de sneltoets W van de superuser.</summary>
    DebugWin,
    /// <summary>Opnieuw afgespeeld komt er een andere score uit.</summary>
    WrongScore,
}

/// <summary>
/// De dagelijkse run van de Prikklok: iedereen speelt dezelfde run op dezelfde UTC-datum, altijd vanaf
/// act 1 met de gewone start. De shell start ze hier en de nachtelijke scorecontrole speelt ze hier
/// opnieuw af, zodat beide zeker dezelfde seed en setup gebruiken. De motor kent geen klok: de datum komt
/// van buiten.
/// </summary>
public static class DailyRun
{
    /// <summary>Altijd de gewone start: act 1, met de Gieterij.</summary>
    public static RunSetup Setup { get; } = new();

    public static Run Start(DateOnly utcDate) => Run.Start(DailySeed.For(utcDate), Setup);

    /// <summary>
    /// Een ingestuurde run nakijken: opnieuw afspelen met de echte motor en de score vergelijken. Een run met
    /// <see cref="DebugWin"/> telt nooit, ook niet als de score klopt.
    /// </summary>
    public static ScoreVerdict Check(DateOnly utcDate, IReadOnlyList<ICommand> commands, int claimedScore)
    {
        if (commands.Any(c => c is DebugWin)) return ScoreVerdict.DebugWin;

        var run = Run.Replay(DailySeed.For(utcDate), commands, Setup);
        if (run.Phase is not (RunPhase.Won or RunPhase.Lost)) return ScoreVerdict.Unfinished;
        return run.Score.Total == claimedScore ? ScoreVerdict.Ok : ScoreVerdict.WrongScore;
    }

    /// <summary>Zoals <see cref="Check(DateOnly, IReadOnlyList{ICommand}, int)"/>, voor de commandolijst als JSON.</summary>
    public static ScoreVerdict Check(DateOnly utcDate, string commandsJson, int claimedScore) =>
        CommandsFromJson(commandsJson) is { } commands ? Check(utcDate, commands, claimedScore) : ScoreVerdict.Unreadable;

    /// <summary>
    /// De commandolijst zoals ze bewaard en ingestuurd wordt. Altijd met de standaardinstellingen van
    /// System.Text.Json (dezelfde vorm als <c>ReplayTests</c>), zodat de shell en de scorecontrole dezelfde
    /// vorm lezen en schrijven. Gegenereerd, zodat een getrimde build niets weglaat.
    /// </summary>
    public static string CommandsToJson(IEnumerable<ICommand> commands) => JsonSerializer.Serialize(commands.ToList(), CommandJson.Default.ListICommand);

    /// <summary>Leeg als de JSON geen lijst commands is.</summary>
    public static IReadOnlyList<ICommand>? CommandsFromJson(string json)
    {
        try
        {
            var commands = JsonSerializer.Deserialize(json, CommandJson.Default.ListICommand);
            return commands is null || commands.Any(c => c is null) ? null : commands;
        }
        catch (Exception e) when (e is JsonException or NotSupportedException)
        {
            return null;
        }
    }
}

[JsonSerializable(typeof(List<ICommand>))]
internal sealed partial class CommandJson : JsonSerializerContext;
