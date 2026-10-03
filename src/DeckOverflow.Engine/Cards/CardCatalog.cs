using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Cards;

/// <summary>
/// Alle kaarten van Act 1. Een verbeterde kaart heeft dezelfde id met een "+" erachter.
/// Namen staan in <c>en.json</c> onder <c>card.&lt;id&gt;</c>. De getallen zijn eerste gokken.
/// </summary>
public static class CardCatalog
{
    // ---------- Starterdeck: elke kaart werkt al in het eerste gevecht ----------

    public static readonly CardDefinition Strike =
        new("strike", 1, TargetMode.Enemy, new DamageEffect(6), ValueKind.Int);

    public static readonly CardDefinition FloatingStrike =
        new("floating-strike", 1, TargetMode.Enemy, new DamageEffect(2.5, Hits: 3), ValueKind.Double, Rarity.Common);

    public static readonly CardDefinition Shield =
        new("shield", 1, TargetMode.Self, new BlockEffect(5));

    /// <summary>Voeg toe: de "Bash" van het starterdeck. Toont zijn regel al in de eerste beurt.</summary>
    public static readonly CardDefinition Add =
        new("add", 0, TargetMode.Self, new ModifierEffect(ModifierOp.Add, 3), Rarity: Rarity.Uncommon);

    // ---------- Beloningen ----------

    public static readonly CardDefinition Mend =
        new("mend", 1, TargetMode.Any, new HealEffect(6), Rarity: Rarity.Common);

    public static readonly CardDefinition RemoldInt =
        new("remold-int", 1, TargetMode.Enemy, new CastEffect(ValueKind.Int), Rarity: Rarity.Common);

    public static readonly CardDefinition RemoldByte =
        new("remold-byte", 1, TargetMode.Enemy, new CastEffect(ValueKind.Byte), Rarity: Rarity.Common);

    public static readonly CardDefinition HeavyStrike =
        new("heavy-strike", 2, TargetMode.Enemy, new DamageEffect(14), ValueKind.Int, Rarity.Common);

    public static readonly CardDefinition FloatingRain =
        new("floating-rain", 1, TargetMode.Enemy, new DamageEffect(1.5, Hits: 4), ValueKind.Double, Rarity.Common);

    public static readonly CardDefinition ThickShield =
        new("thick-shield", 2, TargetMode.Self, new BlockEffect(13), Rarity: Rarity.Common);

    public static readonly CardDefinition SetTo1 =
        new("set-to-1", 1, TargetMode.Enemy, new SetAttackEffect(1), Rarity: Rarity.Uncommon);

    public static readonly CardDefinition DoubleUp =
        new("double-up", 1, TargetMode.Self, new ModifierEffect(ModifierOp.Multiply, 2), Rarity: Rarity.Uncommon);

    public static readonly CardDefinition ByteTrap =
        new("byte-trap", 2, TargetMode.Enemy, new ComboEffect(new CastEffect(ValueKind.Byte), new HealEffect(6)), Rarity: Rarity.Rare);

    /// <summary>Wat je na een gevecht, in de winkel of in een kist kan vinden.</summary>
    public static readonly IReadOnlyList<CardDefinition> RewardPool =
    [
        FloatingStrike, HeavyStrike, FloatingRain, ThickShield, Mend, RemoldInt, RemoldByte,
        SetTo1, Add, DoubleUp,
        ByteTrap
    ];

    /// <summary>
    /// Tien kaarten die je allemaal meteen snapt. Omgieten en Herstel zitten er bewust niet in:
    /// in het eerste gevecht doen ze niets, dus ontdek je ze pas als beloning.
    /// </summary>
    public static IReadOnlyList<CardDefinition> StarterDeck() =>
    [
        Strike, Strike, Strike, Strike,
        FloatingStrike, FloatingStrike,
        Shield, Shield, Shield,
        Add
    ];

    // ---------- Verbeteren aan het rustvuur ----------

    private static readonly Dictionary<string, CardDefinition> Upgrades = new[]
    {
        Strike with { Id = "strike+", Effect = new DamageEffect(9) },
        FloatingStrike with { Id = "floating-strike+", Effect = new DamageEffect(3.5, Hits: 3) },
        Shield with { Id = "shield+", Effect = new BlockEffect(8) },
        Add with { Id = "add+", Effect = new ModifierEffect(ModifierOp.Add, 5) },
        Mend with { Id = "mend+", Effect = new HealEffect(9) },
        RemoldInt with { Id = "remold-int+", Cost = 0 },
        RemoldByte with { Id = "remold-byte+", Cost = 0 },
        HeavyStrike with { Id = "heavy-strike+", Effect = new DamageEffect(19) },
        FloatingRain with { Id = "floating-rain+", Effect = new DamageEffect(1.5, Hits: 6) },
        ThickShield with { Id = "thick-shield+", Effect = new BlockEffect(18) },
        SetTo1 with { Id = "set-to-1+", Cost = 0 },
        DoubleUp with { Id = "double-up+", Cost = 0 },
        ByteTrap with { Id = "byte-trap+", Cost = 1 },
    }.ToDictionary(c => c.BaseId);

    /// <summary>De verbeterde versie, of null als de kaart niet (meer) beter kan.</summary>
    public static CardDefinition? Upgrade(CardDefinition card) =>
        card.IsUpgraded ? null : Upgrades.GetValueOrDefault(card.Id);

    // ---------- Omgieten in de Smeltkroes ----------

    /// <summary>Kan deze kaart in de Smeltkroes? Alleen kaarten die een double afvuren.</summary>
    public static bool CanPour(CardDefinition card) =>
        card is { Kind: ValueKind.Double, Effect: DamageEffect };

    /// <summary>
    /// Een Vlottende kaart omgegoten naar int: kost voortaan 0, maar elke treffer
    /// verliest zijn decimalen zoals <c>(int)2.5</c>. Floating Strike wordt Molded Strike.
    /// </summary>
    public static CardDefinition Pour(CardDefinition card)
    {
        if (!CanPour(card)) throw new ArgumentException($"{card.Id} kan niet in de Smeltkroes.", nameof(card));

        var damage = (DamageEffect)card.Effect;
        string suffix = card.IsUpgraded ? "+" : "";

        return card with
        {
            Id = $"molded-{card.BaseId.Replace("floating-", "")}{suffix}",
            Cost = 0,
            Kind = ValueKind.Int,
            Effect = damage with { Amount = IntRules.Truncate(damage.Amount).Result },
        };
    }

    /// <summary>Elke kaart die in een run kan opduiken, ook verbeterd en omgegoten. Voor tests.</summary>
    public static IEnumerable<CardDefinition> Everything()
    {
        var all = StarterDeck().Concat(RewardPool).Distinct().ToList();
        var upgraded = all.Select(Upgrade).OfType<CardDefinition>().ToList();
        var poured = all.Concat(upgraded).Where(CanPour).Select(Pour);
        return all.Concat(upgraded).Concat(poured);
    }
}
