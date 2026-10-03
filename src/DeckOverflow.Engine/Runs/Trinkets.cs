namespace DeckOverflow.Engine.Runs;

/// <summary>
/// Onderdelen die nergens voor dienen: een tease uit de teaseladder. Een kist bevat soms een onderdeel
/// met een labeltje "hoort bij stap 6". In de Card Hall kan je er niets mee; na de onthulling vallen
/// ze op hun plaats bij de afdeling waar ze horen. Teksten in <c>en.json</c> onder <c>trinket.&lt;key&gt;</c>.
/// </summary>
/// <param name="Step">Het nummer van de afdeling op de plattegrond: het hoofdstuk waar het onderdeel bij hoort.</param>
public sealed record Trinket(string Key, int Step);

public static class Trinkets
{
    /// <summary>De kans in procent dat een kist ook een onderdeel bevat.</summary>
    public const int ChestChance = 50;

    public static readonly IReadOnlyList<Trinket> All =
    [
        new("rule-card", 5),
        new("belt-link", 6),
        new("peg-hook", 7),
        new("bin-label", 8),
    ];
}
