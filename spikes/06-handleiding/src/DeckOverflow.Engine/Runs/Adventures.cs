namespace DeckOverflow.Engine.Runs;

/// <summary>De events. Wat een keuze doet, staat in <see cref="Run"/>; de teksten in <c>en.json</c>.</summary>
public static class Adventures
{
    /// <summary>De openingskeuze voor de eerste knoop, zoals Neow in Slay the Spire.</summary>
    public const string Foundry = "foundry";

    public const string Crucible = "crucible";
    public const string LeakingBarrel = "leaking-barrel";

    /// <summary>Wat een onbekende knoop verderop in de act kan zijn.</summary>
    public static readonly IReadOnlyList<string> All = [Crucible, LeakingBarrel];

    public const int MeltHpCost = 5;
    public const int BarrelGold = 45;
    public const int BarrelHpCost = 7;
}
