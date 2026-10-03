using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;

namespace DeckOverflow.Engine.Runs;

/// <summary>
/// Wat een act anders maakt: zijn vijanden, zijn baas en de kaarten die erbij komen.
/// De regels van eerdere acts blijven gelden, dus ook hun kaarten blijven in de pool.
/// </summary>
/// <param name="Key">Vaste sleutel voor naam en art (<c>act.&lt;key&gt;</c> in <c>en.json</c>).</param>
/// <param name="EasyPool">Gewone gevechten in de eerste twee rijen: om in te komen.</param>
/// <param name="EarlyEvent">Wat een onbekende knoop vroeg in de act is, of leeg voor een gewoon event.</param>
/// <param name="NewCards">Kaarten die in deze act bij de beloningen komen.</param>
public sealed record ActDefinition(
    int Number,
    string Key,
    string Boss,
    IReadOnlyList<string> EasyPool,
    IReadOnlyList<string> NormalPool,
    IReadOnlyList<string> ElitePool,
    IReadOnlyList<string> Events,
    string? EarlyEvent,
    IReadOnlyList<CardDefinition> NewCards);

public static class Acts
{
    /// <summary>Act 1: H2. Types, afkappen, overflow, deling, voorrang.</summary>
    public static readonly ActDefinition VatValley = new(
        1, "vat-valley", Bestiary.Reckoner,
        EasyPool: [Bestiary.Slime, Bestiary.Knight],
        NormalPool: [Bestiary.Knight, Bestiary.Ghost, Bestiary.Dripper, Bestiary.Splitter, Bestiary.Stray],
        ElitePool: [Bestiary.Golem, Bestiary.Counter],
        Events: [Adventures.Crucible, Adventures.LeakingBarrel, Adventures.CopyMachine, Adventures.ScrapBin],
        EarlyEvent: Bestiary.Jug,
        NewCards: CardCatalog.RewardPool);

    /// <summary>Act 2, De Drukkerij (in het spel: The Print Shop): H3. Tekst: string plakt, char is een getal, Length.</summary>
    public static readonly ActDefinition PrintShop = new(
        2, "print-shop", Bestiary.Typesetter,
        EasyPool: [Bestiary.TypeBlock, Bestiary.PaperGolem],
        NormalPool: [Bestiary.TypeBlock, Bestiary.PaperGolem, Bestiary.Ghost, Bestiary.Splitter, Bestiary.Stray],
        ElitePool: [Bestiary.EffectivePower, Bestiary.Y2K],
        Events: [Adventures.CopyMachine, Adventures.ScrapBin, Adventures.LeakingBarrel, Adventures.Crucible],
        EarlyEvent: null,
        NewCards: [CardCatalog.CountLetters, CardCatalog.LetterA]);

    /// <summary>Act 3, De Gieterij (in het spel: The Mold Works): H4. Expliciet omzetten: cast, Convert, afronden.</summary>
    public static readonly ActDefinition MoldWorks = new(
        3, "mold-works", Bestiary.Caster,
        EasyPool: [Bestiary.Ingot, Bestiary.Rounder],
        NormalPool: [Bestiary.Rounder, Bestiary.Label, Bestiary.Ingot, Bestiary.Knight, Bestiary.Ghost],
        ElitePool: [Bestiary.Colossus, Bestiary.Index],
        Events: [Adventures.RoundingDesk, Adventures.Crucible, Adventures.CopyMachine, Adventures.ScrapBin, Adventures.LeakingBarrel],
        EarlyEvent: null,
        NewCards: [CardCatalog.MeasureTwice, CardCatalog.ReadTheLabel]);

    public static readonly IReadOnlyList<ActDefinition> All = [VatValley, PrintShop, MoldWorks];

    public static ActDefinition Get(int number) =>
        All.FirstOrDefault(a => a.Number == number) ?? throw new ArgumentOutOfRangeException(nameof(number), $"Act {number} bestaat niet.");

    public static bool IsLast(ActDefinition act) => act.Number == All[^1].Number;

    /// <summary>Alle beloningskaarten tot en met deze act.</summary>
    public static IReadOnlyList<CardDefinition> CardPool(int upToAct) =>
        [.. All.Where(a => a.Number <= upToAct).SelectMany(a => a.NewCards).Distinct()];
}
