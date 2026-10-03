using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Codex;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Tests;

/// <summary>
/// Effective Power: zijn HP is een bericht dat crasht vanaf 32 tekens. Plakken is hier de bedoeling,
/// en wie ziet dat "2.5" drie tekens is, wint het snelst.
/// </summary>
public class EffectivePowerTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CombatSetup Against(params CardDefinition[] extra) =>
        new(Scenarios.Player(), Bestiary.Create(Bestiary.EffectivePower), [.. TestHelpers.TestDeck, .. extra]);

    [Fact]
    public void Een_Whack_plakt_een_teken_een_Floating_kaart_negen()
    {
        var combat = TestHelpers.StartWithHand(Against(), CardCatalog.Strike.Id, CardCatalog.FloatingStrike.Id);

        combat.Play(CardCatalog.Strike.Id, E);
        Assert.Equal("effective. Power6", combat.Enemy().Text);

        combat.Play(CardCatalog.FloatingStrike.Id, E);
        Assert.Equal("effective. Power62.52.52.5", combat.Enemy().Text);
        Assert.Equal(32, combat.Enemy().TextLimit);
    }

    [Fact]
    public void Vanaf_32_tekens_crasht_het_bericht_en_valt_hij_om()
    {
        var combat = TestHelpers.StartWithHand(Against(CardCatalog.FloatingRain), CardCatalog.FloatingStrike.Id, CardCatalog.FloatingRain.Id);
        combat.Play(CardCatalog.FloatingStrike.Id, E);   // 16 + 9 = 25 tekens

        var events = combat.Play(CardCatalog.FloatingRain.Id, E).WithoutSeq();   // + "1.5" ... tot 32

        Assert.Contains(events, e => e is TextCrashed { Limit: 32 });
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.StringLength);
    }

    [Fact]
    public void Tekst_op_tekst_plakt_ook_Ink_werkt_hier_wel()
    {
        var combat = TestHelpers.StartWithHand(Against(CardCatalog.Ink), CardCatalog.Ink.Id, CardCatalog.Strike.Id);
        combat.Play(CardCatalog.Ink.Id, P);

        combat.Play(CardCatalog.Strike.Id, E);

        Assert.Equal("effective. Power61", combat.Enemy().Text);
    }

    [Fact]
    public void Het_bericht_parsen_crasht_met_een_FormatException()
    {
        var combat = TestHelpers.StartWithHand(Against(CardCatalog.ReadTheLabel), CardCatalog.ReadTheLabel.Id);

        var events = combat.Play(CardCatalog.ReadTheLabel.Id, E).WithoutSeq();

        Assert.Contains(events, e => e is ExceptionThrown { Exception: "FormatException" });
        Assert.Equal(ValueKind.String, combat.Enemy().Kind);
    }
}
