using DeckOverflow.ControlRoom.Achievements;
using DeckOverflow.ControlRoom.Levels;
using DeckOverflow.Core.Codex;

namespace DeckOverflow.ControlRoom.Gambits;

/// <summary>Eén zet in een opgenomen duel: wat er gebeurde, en hoe alles er daarna bij stond.</summary>
public sealed record DuelFrame(IReadOnlyList<DuelEvent> Events, DuelSnapshot Snapshot);

/// <summary>
/// Een heel duel, vooraf uitgerekend. De motor is deterministisch en een duel duurt hoogstens 80 zetten,
/// dus de shell kan het helemaal opnemen en daarna afspelen, pauzeren en terugspoelen naar elke zet.
/// </summary>
/// <param name="Start">Hoe alles erbij stond voor de eerste zet.</param>
/// <param name="Moments">De Codex-momenten van de speler, met de getallen van het eerste moment.</param>
/// <param name="Panels">De ✗-panelen die dit duel verdiende.</param>
public sealed record DuelRecording(
    string Level,
    IReadOnlyList<Rule> Rules,
    DuelSnapshot Start,
    IReadOnlyList<DuelFrame> Frames,
    Outcome Outcome,
    int Turns,
    IReadOnlyList<CodexMoment> Moments,
    IReadOnlyList<string> Panels)
{
    public bool Won => Outcome == Outcome.PlayerWon;

    /// <summary>De score zoals Opus Magnum: twee getallen, elk apart om te verbeteren. Lager is beter.</summary>
    public Score? Score => Won ? new Score(Turns, Rules.Count) : null;

    public static DuelRecording Of(Level level, IReadOnlyList<Rule> rules)
    {
        var duel = LevelCatalog.Start(level, rules);
        var start = duel.Snapshot();
        var frames = new List<DuelFrame>();
        while (!duel.IsOver)
            frames.Add(new DuelFrame(duel.Step(), duel.Snapshot()));
        return new DuelRecording(level.Key, rules, start, frames, duel.Outcome!.Value, duel.Turn,
            duel.Moments, XRegister.Earned(level, duel));
    }
}

/// <summary>Beurten en regels van een gewonnen duel.</summary>
public sealed record Score(int Turns, int Rules);
