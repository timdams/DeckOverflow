namespace DeckOverflow.Engine.Cards;

public static class CardCatalog
{
    public static readonly CardDefinition Slag =
        new("slag", "Slag", 1, TargetMode.Enemy, new DamageEffect(6), "Doe 6 schade.");

    public static readonly CardDefinition Schild =
        new("schild", "Schild", 1, TargetMode.Self, new BlockEffect(5), "Krijg 5 blok.");

    public static readonly CardDefinition Herstel =
        new("herstel", "Herstel", 1, TargetMode.Any, new HealEffect(6), "Herstel 6 HP. Kies je doelwit.");
}
