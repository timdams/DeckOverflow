using System.Globalization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Text;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Relics;

/// <summary>
/// Alle relics. De volgorde telt: als twee relics dezelfde kaart gratis kunnen maken,
/// wint de eerste. Counter staat voor Floating Point, zodat die laatste niet verspild wordt.
/// </summary>
public static class RelicCatalog
{
    public const string Counter = "counter";
    public const string FloatingPoint = "floating-point";
    public const string ScrapPouch = "scrap-pouch";
    public const string AnchorBarrel = "anchor-barrel";
    public const string GreatPot = "great-pot";
    public const string InkWell = "ink-well";
    public const string TallyCounter = "tally-counter";
    public const string CoinMold = "coin-mold";

    public static readonly IReadOnlyList<string> All = [Counter, InkWell, FloatingPoint, ScrapPouch, AnchorBarrel, GreatPot, TallyCounter, CoinMold];

    /// <summary>Een vers exemplaar, zonder status uit een vorig gevecht.</summary>
    public static Relic Create(string id) => id switch
    {
        Counter => new CounterRelic(),
        FloatingPoint => new FloatingPointRelic(),
        ScrapPouch => new ScrapPouchRelic(),
        AnchorBarrel => new AnchorBarrelRelic(),
        GreatPot => new GreatPotRelic(),
        InkWell => new InkWellRelic(),
        TallyCounter => new TallyCounterRelic(),
        CoinMold => new CoinMoldRelic(),
        _ => throw new ArgumentException($"Onbekende relic: {id}", nameof(id))
    };

    /// <summary>Verse exemplaren in catalogusvolgorde.</summary>
    public static List<Relic> CreateAll(IEnumerable<string> ids) =>
        [.. ids.OrderBy(id => IndexOf(id)).Select(Create)];

    private static int IndexOf(string id)
    {
        for (int i = 0; i < All.Count; i++) if (All[i] == id) return i;
        throw new ArgumentException($"Onbekende relic: {id}", nameof(id));
    }
}

/// <summary>Elke derde kaart in een gevecht kost 0. Modulo en tellen.</summary>
public sealed class CounterRelic : Relic
{
    public override string Id => RelicCatalog.Counter;
    public override bool MakesFree(CardDefinition card, int cardsPlayed) => cardsPlayed % 3 == 2;
}

/// <summary>Je eerste Vlottende kaart per gevecht kost 0.</summary>
public sealed class FloatingPointRelic : Relic
{
    private bool _used;

    public override string Id => RelicCatalog.FloatingPoint;
    public override bool MakesFree(CardDefinition card, int cardsPlayed) => !_used && card.Kind == ValueKind.Double;
    public override void OnMadeFree() => _used = true;
}

/// <summary>Bewaart wat er van jouw schade afgekapt wordt. Afkappen als bron.</summary>
public sealed class ScrapPouchRelic : Relic
{
    public const int Threshold = 3;
    private double _scraps;

    public override string Id => RelicCatalog.ScrapPouch;
    public override TextRef Text => TextRef.Of("relic.scrap-pouch.text", ("amount", Threshold));
    public override string Counter => $"{_scraps.ToString(CultureInfo.InvariantCulture)}/{Threshold}";

    public override void OnDamageTruncated(double lost) => _scraps = Math.Round(_scraps + lost, 2);

    public override bool TryFire(out double damage)
    {
        damage = Threshold;
        if (_scraps < Threshold) return false;
        _scraps = Math.Round(_scraps - Threshold, 2);
        return true;
    }
}

public sealed class AnchorBarrelRelic : Relic
{
    public const int Block = 6;

    public override string Id => RelicCatalog.AnchorBarrel;
    public override TextRef Text => TextRef.Of("relic.anchor-barrel.text", ("amount", Block));
    public override double BlockAtCombatStart => Block;
}

public sealed class GreatPotRelic : Relic
{
    public const int MaxHp = 8;

    public override string Id => RelicCatalog.GreatPot;
    public override TextRef Text => TextRef.Of("relic.great-pot.text", ("amount", MaxHp));
    public override int MaxHpOnGain => MaxHp;
}

/// <summary>Je eerste Ink of Read per gevecht kost 0. Voor wie tekst wil plakken.</summary>
public sealed class InkWellRelic : Relic
{
    private bool _used;

    public override string Id => RelicCatalog.InkWell;
    public override bool MakesFree(CardDefinition card, int cardsPlayed) =>
        !_used && card.Effect is ModifierEffect { Operand.IsText: true } or ModifierEffect { Op: ModifierOp.Parse };
    public override void OnMadeFree() => _used = true;
}

/// <summary>Na elk gewonnen gevecht heel je één meer dan de vorige keer: <c>++count</c>.</summary>
public sealed class TallyCounterRelic : Relic
{
    public override string Id => RelicCatalog.TallyCounter;
    public override int HealAfterWin(int combatsWon) => combatsWon;
}

/// <summary>50% meer goud uit gevechten, afgerond met <c>Math.Round</c>: 22.5 wordt 22.</summary>
public sealed class CoinMoldRelic : Relic
{
    public const double Factor = 1.5;

    public override string Id => RelicCatalog.CoinMold;
    public override double GoldFactor => Factor;
}
