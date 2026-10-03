using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Events;

namespace DeckOverflow.Engine.Achievements;

/// <summary>
/// Een ✗-paneel uit de handleiding: iets wat verboden is en wat jij toch deed. Alleen spelprestaties,
/// nooit leerprestaties. Naam en uitleg staan in <c>en.json</c> onder <c>xpanel.&lt;key&gt;</c>.
/// </summary>
/// <param name="Hidden">Verborgen tot je hem vindt, zodat geruchten zich op de speelplaats verspreiden.</param>
public sealed record XPanel(string Key, bool Hidden = false);

/// <summary>Het ✗-register: de panelen, en hoe de motor ze herkent aan de events van een gevecht of een run.</summary>
public static class XRegister
{
    public const string FeedTheMachine = "feed-the-machine";
    public const string MessageTooLong = "message-too-long";
    public const string CasterWraps = "caster-wraps";
    public const string BigNumber = "big-number";
    public const string BadText = "bad-text";
    public const string AgainAriane = "again-ariane";
    public const string FrozenIndex = "frozen-index";
    public const string DividedToNothing = "divided-to-nothing";
    /// <summary>De laatste baas van de Card Hall (de deckbuilder, H2 tot H4), niet van de hele fabriek.</summary>
    public const string CardHallCleared = "card-hall-cleared";

    /// <summary>Niet door de motor herkend maar door de shell: een Codex-pagina tot het einde lezen.</summary>
    public const string ReadTheManual = "read-the-manual";

    /// <summary>Een aanval of treffer van minstens zoveel telt als een groot getal.</summary>
    public const int BigNumberThreshold = 500;

    public static readonly IReadOnlyList<XPanel> All =
    [
        new(FeedTheMachine),
        new(MessageTooLong),
        new(DividedToNothing),
        new(BigNumber),
        new(BadText),
        new(AgainAriane),
        new(FrozenIndex, Hidden: true),
        new(CasterWraps, Hidden: true),
        new(CardHallCleared),
        new(ReadTheManual, Hidden: true),
    ];

    /// <summary>Welke panelen deze events verdienen, in een gevecht tegen <paramref name="enemy"/>.</summary>
    public static IEnumerable<string> Earned(IReadOnlyList<GameEvent> events, string? enemy)
    {
        var died = events.OfType<CombatantDied>().Any(d => d.TargetId == Combat.Combat.EnemyId);

        // Een vijand helen tot hij omklapt en sterft
        if (died && events.OfType<ValueOverflowed>().Any(o => o.TargetId == Combat.Combat.EnemyId && o.After == 0))
            yield return FeedTheMachine;

        // Effective Power in één kaart van zijn volle bericht tot de crash
        if (events.OfType<TextCrashed>().Any() && events.OfType<TextAppended>().FirstOrDefault()?.Before == "effective. Power")
            yield return MessageTooLong;

        // De Caster klapt om als hij een byte wordt
        if (enemy == Bestiary.Caster && events.OfType<TypeChanged>().Any(t => t.Wrapped))
            yield return CasterWraps;

        // Een aanval van honderden: Ink, Spare Screw en Read
        if (events.OfType<ModifiersApplied>().Any(m => m.After >= BigNumberThreshold))
            yield return BigNumber;

        // Tekst parsen die geen getal is
        if (events.OfType<ExceptionThrown>().Any(e => e.Exception == nameof(FormatException)))
            yield return BadText;

        // Convert laten crashen op een reus
        if (events.OfType<ConversionCrashed>().Any())
            yield return AgainAriane;

        // The Index groeit niet meer: het afkappen at de groei op
        if (events.OfType<ValueGrew>().Any(g => g.After == g.Before))
            yield return FrozenIndex;

        // Een bewuste intent tot nul gedeeld
        if (events.OfType<AttackLaunched>().Any(a => a.SourceId == Combat.Combat.EnemyId && a.Value == 0 && a.Expression.Contains(" / ")))
            yield return DividedToNothing;

        // De Card Hall uitgespeeld: de baas van de laatste act van de deckbuilder
        if (events.OfType<RunEnded>().Any(r => r.Won))
            yield return CardHallCleared;
    }
}
