using DeckOverflow.Core.Values;

namespace DeckOverflow.CardHall.Cards;

/// <summary>
/// Alle kaarten. Welke kaart in welke act bij de beloningen komt, staat in <see cref="Runs.Acts"/>. Een verbeterde kaart heeft dezelfde id met een "+" erachter.
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

    // ---------- De getypeerde aanval: H2 (deling, double) en H3 (tekst) ----------

    /// <summary><c>× 1.0</c>: je volgende aanval wordt een double, zodat een deling haar decimalen houdt.</summary>
    public static readonly CardDefinition FloatingPoint =
        new("floating-point", 0, TargetMode.Self, new ModifierEffect(ModifierOp.Multiply, TypedValue.Double(1.0)), Rarity: Rarity.Common);

    /// <summary><c>/ 2</c>, maar twee keer: <c>7 / 2</c> is twee keer 3, <c>7.0 / 2</c> twee keer 3.5.</summary>
    public static readonly CardDefinition Split =
        new("split", 0, TargetMode.Self, new ModifierEffect(ModifierOp.Divide, TypedValue.Int(2), DoubleHits: true), Rarity: Rarity.Common);

    /// <summary><c>+ "1"</c>: tekst plakt. <c>7 + "1"</c> is <c>"71"</c>, maar tekst raakt geen getal.</summary>
    public static readonly CardDefinition Ink =
        new("ink", 1, TargetMode.Self, new ModifierEffect(ModifierOp.Add, TypedValue.String("1")), Rarity: Rarity.Uncommon);

    /// <summary>
    /// <c>int.Parse(…)</c>: tekst wordt een getal. Ongeldige tekst crasht, en je beurt eindigt.
    /// Zeldzaam en duur: samen met Ink en Spare Screw maakt ze van een Whack honderden schade.
    /// </summary>
    public static readonly CardDefinition Read =
        new("read", 2, TargetMode.Self, new ModifierEffect(ModifierOp.Parse, default), Rarity: Rarity.Rare);

    public static readonly CardDefinition ByteTrap =
        new("byte-trap", 2, TargetMode.Enemy, new ComboEffect(new CastEffect(ValueKind.Byte), new HealEffect(6)), Rarity: Rarity.Rare);

    // ---------- Act 2: de Gieterij ----------

    /// <summary>
    /// <c>Convert.ToByte</c>: rondt af, en crasht als het getal niet past. Op een kleine vijand een byte
    /// die je kan laten omklappen; op een reus een <c>OverflowException</c> die zijn aanval kost.
    /// </summary>
    public static readonly CardDefinition MeasureTwice =
        new("measure-twice", 1, TargetMode.Enemy, new ConvertEffect(ValueKind.Byte), Rarity: Rarity.Uncommon);

    /// <summary><c>int.Parse</c> op een vijand: The Label wordt een getal dat je kan raken.</summary>
    public static readonly CardDefinition ReadTheLabel =
        new("read-the-label", 1, TargetMode.Enemy, new ParseEffect(), Rarity: Rarity.Common);

    // ---------- Act 2: de Drukkerij ----------

    /// <summary><c>.Length</c>: een tekstvijand wordt een getal, zo groot als zijn tekst lang is.</summary>
    public static readonly CardDefinition CountLetters =
        new("count-letters", 1, TargetMode.Enemy, new LengthEffect(), Rarity: Rarity.Common);

    /// <summary>
    /// <c>+ 'A'</c>: een <c>char</c> is een getal, dus <c>6 + 'A'</c> is 71. Op tekst plakt hij als letter.
    /// Zeldzaam en duur, want een letter is meteen 65 of meer.
    /// </summary>
    public static readonly CardDefinition LetterA =
        new("letter-a", 2, TargetMode.Self, new ModifierEffect(ModifierOp.Add, TypedValue.Char('A')), Rarity: Rarity.Rare);

    /// <summary><c>isSolid = !isSolid</c>: zet de telling van de Bool Ghost recht, zonder treffer.</summary>
    public static readonly CardDefinition Flip =
        new("flip", 1, TargetMode.Enemy, new FlipEffect(), Rarity: Rarity.Common);

    /// <summary><c>% 5</c> op de aanval van een vijand: 16 wordt 1, 23 wordt 3, 25 wordt 0. Modulo als verdediging.</summary>
    public static readonly CardDefinition Remainder =
        new("remainder", 1, TargetMode.Enemy, new RemainderEffect(5), Rarity: Rarity.Uncommon);

    /// <summary>Wat je in act 1 na een gevecht, in de winkel of in een kist kan vinden.</summary>
    public static readonly IReadOnlyList<CardDefinition> RewardPool =
    [
        FloatingStrike, HeavyStrike, FloatingRain, ThickShield, Mend, RemoldInt, RemoldByte,
        SetTo1, Add, DoubleUp,
        FloatingPoint, Split, Ink, Read,
        ByteTrap, Flip, Remainder
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
        MeasureTwice with { Id = "measure-twice+", Cost = 0 },
        ReadTheLabel with { Id = "read-the-label+", Cost = 0 },
        CountLetters with { Id = "count-letters+", Cost = 0 },
        LetterA with { Id = "letter-a+", Cost = 1 },
        // Split+ deelt door 2.0: een double, dus geen verlies meer aan de deling van gehele getallen
        Split with { Id = "split+", Effect = new ModifierEffect(ModifierOp.Divide, TypedValue.Double(2.0), DoubleHits: true) },
        FloatingPoint with { Id = "floating-point+", Effect = new ModifierEffect(ModifierOp.Multiply, TypedValue.Double(1.5)) },
        Ink with { Id = "ink+", Cost = 0 },
        Read with { Id = "read+", Cost = 1 },
        Flip with { Id = "flip+", Cost = 0 },
        Remainder with { Id = "remainder+", Effect = new RemainderEffect(3) },
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
        var all = StarterDeck().Concat(RewardPool).Append(MeasureTwice).Append(ReadTheLabel).Append(CountLetters).Append(LetterA).Distinct().ToList();
        var upgraded = all.Select(Upgrade).OfType<CardDefinition>().ToList();
        var poured = all.Concat(upgraded).Where(CanPour).Select(Pour);
        return all.Concat(upgraded).Concat(poured);
    }
}
