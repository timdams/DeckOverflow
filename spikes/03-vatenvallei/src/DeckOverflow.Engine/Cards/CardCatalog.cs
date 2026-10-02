using System.Globalization;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

/// <summary>
/// Alle kaarten van spike 3. Een verbeterde kaart heeft dezelfde id met een "+" erachter.
/// De getallen zijn eerste gokken.
/// </summary>
public static class CardCatalog
{
    // ---------- Starterdeck ----------

    public static readonly CardDefinition Slag =
        new("slag", "Slag", 1, TargetMode.Enemy, new DamageEffect(6), "Doe 6 schade.", ValueKind.Int);

    public static readonly CardDefinition VlottendeSlag =
        new("vlottende-slag", "Vlottende Slag", 1, TargetMode.Enemy, new DamageEffect(2.5, Hits: 3), "3× 2.5 schade.", ValueKind.Double, Rarity.Common);

    public static readonly CardDefinition Schild =
        new("schild", "Schild", 1, TargetMode.Self, new BlockEffect(5), "Krijg 5 blok.");

    public static readonly CardDefinition Herstel =
        new("herstel", "Herstel", 1, TargetMode.Any, new HealEffect(6), "Herstel 6 HP. Kies je doelwit.");

    public static readonly CardDefinition GietOmInt =
        new("giet-int", "Giet om", 1, TargetMode.Enemy, new CastEffect(ValueKind.Int), "Giet een vijand om.");

    public static readonly CardDefinition GietOmByte =
        new("giet-byte", "Giet om", 1, TargetMode.Enemy, new CastEffect(ValueKind.Byte), "Giet een vijand om.");

    // ---------- Beloningen ----------

    public static readonly CardDefinition ZwareSlag =
        new("zware-slag", "Zware Slag", 2, TargetMode.Enemy, new DamageEffect(14), "Doe 14 schade.", ValueKind.Int, Rarity.Common);

    public static readonly CardDefinition VlottendeRegen =
        new("vlottende-regen", "Vlottende Regen", 1, TargetMode.Enemy, new DamageEffect(1.5, Hits: 4), "4× 1.5 schade.", ValueKind.Double, Rarity.Common);

    public static readonly CardDefinition DikSchild =
        new("dik-schild", "Dik Schild", 2, TargetMode.Self, new BlockEffect(13), "Krijg 13 blok.", Rarity: Rarity.Common);

    public static readonly CardDefinition ZetOp1 =
        new("zet-op-1", "Zet op 1", 1, TargetMode.Enemy, new SetAttackEffect(1), "De aanval van een vijand wordt 1.", Rarity: Rarity.Uncommon);

    public static readonly CardDefinition VoegToe =
        new("voeg-toe", "Voeg toe", 0, TargetMode.Self, new ModifierEffect(ModifierOp.Add, 3), "Je volgende kaart: +3.", Rarity: Rarity.Uncommon);

    public static readonly CardDefinition Verdubbel =
        new("verdubbel", "Verdubbel", 1, TargetMode.Self, new ModifierEffect(ModifierOp.Multiply, 2), "Je volgende kaart: ×2.", Rarity: Rarity.Uncommon);

    public static readonly CardDefinition Byteval =
        new("byteval", "Byteval", 2, TargetMode.Enemy, new ComboEffect(new CastEffect(ValueKind.Byte), new HealEffect(6)),
            "Giet een vijand om en herstel hem 6 HP.", Rarity: Rarity.Rare);

    /// <summary>Wat je na een gevecht, in de winkel of in een kist kan vinden.</summary>
    public static readonly IReadOnlyList<CardDefinition> RewardPool =
        [VlottendeSlag, ZwareSlag, VlottendeRegen, DikSchild, ZetOp1, VoegToe, Verdubbel, Byteval];

    public static IReadOnlyList<CardDefinition> StarterDeck() =>
    [
        Slag, Slag, Slag,
        VlottendeSlag, VlottendeSlag,
        Schild, Schild,
        Herstel,
        GietOmInt, GietOmByte
    ];

    // ---------- Verbeteren aan het rustvuur ----------

    private static readonly Dictionary<string, CardDefinition> Upgrades = new[]
    {
        Slag with { Id = "slag+", Name = "Slag+", Effect = new DamageEffect(9), Text = "Doe 9 schade." },
        VlottendeSlag with { Id = "vlottende-slag+", Name = "Vlottende Slag+", Effect = new DamageEffect(3.5, Hits: 3), Text = "3× 3.5 schade." },
        Schild with { Id = "schild+", Name = "Schild+", Effect = new BlockEffect(8), Text = "Krijg 8 blok." },
        Herstel with { Id = "herstel+", Name = "Herstel+", Effect = new HealEffect(9), Text = "Herstel 9 HP. Kies je doelwit." },
        GietOmInt with { Id = "giet-int+", Name = "Giet om+", Cost = 0 },
        GietOmByte with { Id = "giet-byte+", Name = "Giet om+", Cost = 0 },
        ZwareSlag with { Id = "zware-slag+", Name = "Zware Slag+", Effect = new DamageEffect(19), Text = "Doe 19 schade." },
        VlottendeRegen with { Id = "vlottende-regen+", Name = "Vlottende Regen+", Effect = new DamageEffect(1.5, Hits: 6), Text = "6× 1.5 schade." },
        DikSchild with { Id = "dik-schild+", Name = "Dik Schild+", Effect = new BlockEffect(18), Text = "Krijg 18 blok." },
        ZetOp1 with { Id = "zet-op-1+", Name = "Zet op 1+", Cost = 0 },
        VoegToe with { Id = "voeg-toe+", Name = "Voeg toe+", Effect = new ModifierEffect(ModifierOp.Add, 5), Text = "Je volgende kaart: +5." },
        Verdubbel with { Id = "verdubbel+", Name = "Verdubbel+", Cost = 0 },
        Byteval with { Id = "byteval+", Name = "Byteval+", Cost = 1 },
    }.ToDictionary(c => c.Id.TrimEnd('+'));

    /// <summary>De verbeterde versie, of null als de kaart niet (meer) beter kan.</summary>
    public static CardDefinition? Upgrade(CardDefinition card) =>
        card.IsUpgraded ? null : Upgrades.GetValueOrDefault(card.Id);

    // ---------- Omgieten in de Smeltkroes ----------

    /// <summary>Kan deze kaart in de Smeltkroes? Alleen kaarten die een double afvuren.</summary>
    public static bool CanPour(CardDefinition card) =>
        card is { Kind: ValueKind.Double, Effect: DamageEffect };

    /// <summary>
    /// Een Vlottende kaart omgegoten naar int: kost voortaan 0, maar elke treffer
    /// verliest zijn decimalen zoals <c>(int)2.5</c>.
    /// </summary>
    public static CardDefinition Pour(CardDefinition card)
    {
        if (!CanPour(card)) throw new ArgumentException($"{card.Name} kan niet in de Smeltkroes.", nameof(card));

        var damage = (DamageEffect)card.Effect;
        int amount = IntRules.Truncate(damage.Amount).Result;
        string suffix = card.IsUpgraded ? "+" : "";
        string baseName = card.Name.Replace("Vlottende ", "").TrimEnd('+');

        return card with
        {
            Id = $"gegoten-{card.Id.Replace("vlottende-", "").TrimEnd('+')}{suffix}",
            Name = $"Gegoten {baseName}{suffix}",
            Cost = 0,
            Kind = ValueKind.Int,
            Effect = damage with { Amount = amount },
            Text = $"{damage.Hits}× {Num(amount)} schade."
        };
    }

    private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);
}
