using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

public static class CardCatalog
{
    public static readonly CardDefinition Slag =
        new("slag", "Slag", 1, TargetMode.Enemy, new DamageEffect(6), "Doe 6 schade.", ValueKind.Int);

    public static readonly CardDefinition VlottendeSlag =
        new("vlottende-slag", "Vlottende Slag", 1, TargetMode.Enemy, new DamageEffect(2.5, Hits: 3), "3× 2.5 schade.", ValueKind.Double);

    public static readonly CardDefinition Schild =
        new("schild", "Schild", 1, TargetMode.Self, new BlockEffect(5), "Krijg 5 blok.");

    public static readonly CardDefinition Herstel =
        new("herstel", "Herstel", 1, TargetMode.Any, new HealEffect(6), "Herstel 6 HP. Kies je doelwit.");

    public static readonly CardDefinition GietOmInt =
        new("giet-int", "Giet om", 1, TargetMode.Enemy, new CastEffect(ValueKind.Int), "Giet een vijand om.");

    public static readonly CardDefinition GietOmByte =
        new("giet-byte", "Giet om", 1, TargetMode.Enemy, new CastEffect(ValueKind.Byte), "Giet een vijand om.");
}
