namespace DeckOverflow.CardHall.Runs;

/// <summary>De events. Wat een keuze doet, staat in <see cref="Run"/>; de teksten in <c>en.json</c>.</summary>
public static class Adventures
{
    /// <summary>De openingskeuze voor de eerste knoop, zoals Neow in Slay the Spire.</summary>
    public const string Foundry = "foundry";

    public const string Crucible = "crucible";
    public const string LeakingBarrel = "leaking-barrel";
    public const string CopyMachine = "copy-machine";
    public const string ScrapBin = "scrap-bin";
    /// <summary>Act 2: een bediende rondt je goud af met <c>Math.Round</c>, bankiersafronding inbegrepen.</summary>
    public const string RoundingDesk = "rounding-desk";

    /// <summary>Alle events, over de acts heen. Welk event in welke act zit, staat in <see cref="Acts"/>.</summary>
    public static readonly IReadOnlyList<string> All = [Crucible, LeakingBarrel, CopyMachine, ScrapBin, RoundingDesk];

    public const int MeltHpCost = 5;
    public const int BarrelGold = 45;
    public const int BarrelHpCost = 7;
    public const int CopyHpCost = 6;
    public const int ScrapGold = 40;

    /// <summary>Goud afgerond op honderdtallen: <c>Math.Round(150 / 100.0)</c> is 2, maar <c>Math.Round(250 / 100.0)</c> ook.</summary>
    public static int RoundGold(int gold) => (int)Math.Round(gold / 100.0) * 100;
}
