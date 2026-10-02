namespace DeckOverflow.Engine.Runs;

/// <summary>De events op de map. Wat een keuze doet, staat in <see cref="Run"/>.</summary>
public static class Adventures
{
    public const string Smeltkroes = "smeltkroes";
    public const string LekkendVat = "lekkend-vat";

    public static readonly IReadOnlyList<string> All = [Smeltkroes, LekkendVat];

    public const int SmeltHpCost = 5;
    public const int VatGold = 45;
    public const int VatHpCost = 7;
}
