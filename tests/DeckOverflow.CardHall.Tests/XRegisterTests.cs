using DeckOverflow.CardHall.Achievements;
using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.CardHall.Maps;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Core.Values;

namespace DeckOverflow.Tests;

/// <summary>Het ✗-register: panelen voor spelprestaties, herkend aan de events.</summary>
public class XRegisterTests
{
    private const int E = Combat.EnemyId;

    [Fact]
    public void Een_vijand_helen_tot_hij_omklapt_en_sterft_is_een_paneel()
    {
        var combat = TestHelpers.StartWithHand(Bestiary.Golem, CardCatalog.Mend.Id);

        var events = combat.Play(CardCatalog.Mend.Id, E);

        Assert.Contains(XRegister.FeedTheMachine, XRegister.Earned(events, Bestiary.Golem));
    }

    [Fact]
    public void Effective_Power_in_een_kaart_laten_crashen_is_een_paneel()
    {
        GameEvent[] events =
        [
            new TextAppended(E, "effective. Power", "0.75", "effective. Power0.75"),
            new TextCrashed(E, 32, 32),
        ];

        Assert.Contains(XRegister.MessageTooLong, XRegister.Earned(events, Bestiary.EffectivePower));
    }

    [Fact]
    public void Pas_als_het_in_een_kaart_gebeurt_niet_na_eerdere_treffers()
    {
        GameEvent[] events =
        [
            new TextAppended(E, "effective. Power62.5", "1.5", "effective. Power62.51.5"),
            new TextCrashed(E, 32, 32),
        ];

        Assert.DoesNotContain(XRegister.MessageTooLong, XRegister.Earned(events, Bestiary.EffectivePower));
    }

    [Fact]
    public void Een_aanval_van_honderden_is_een_paneel()
    {
        GameEvent[] events = [new ModifiersApplied("strike", 6, 613, "int.Parse(6 + \"1\" + 3)")];

        Assert.Contains(XRegister.BigNumber, XRegister.Earned(events, Bestiary.Colossus));
    }

    [Fact]
    public void De_Caster_laten_omklappen_en_The_Index_bevriezen_zijn_verborgen_panelen()
    {
        GameEvent[] caster = [new TypeChanged(E, ValueKind.Double, ValueKind.Byte, 300, 44, 255, 0, Wrapped: true)];
        GameEvent[] index = [new ValueGrew(E, 19, 1.05, 19.95, 19)];

        Assert.Contains(XRegister.CasterWraps, XRegister.Earned(caster, Bestiary.Caster));
        Assert.Contains(XRegister.FrozenIndex, XRegister.Earned(index, Bestiary.Index));
        Assert.True(XRegister.All.Single(p => p.Key == XRegister.CasterWraps).Hidden);
    }

    [Fact]
    public void Een_bewuste_intent_tot_nul_gedeeld_is_een_paneel()
    {
        GameEvent[] events = [new AttackLaunched(E, Combat.PlayerId, "30 / (30 + 1)", 0)];

        Assert.Contains(XRegister.DividedToNothing, XRegister.Earned(events, Bestiary.Splitter));
    }

    [Fact]
    public void De_run_geeft_een_paneel_maar_een_keer()
    {
        var run = Run.Start(255, new RunSetup(Deck: TestHelpers.TestDeck, Opening: false,
            Map: ActMap.Path((NodeKind.Elite, Bestiary.Golem), (NodeKind.Elite, Bestiary.Golem), (NodeKind.Rest, null))));
        var all = new List<GameEvent>();

        run.Handle(new ChooseNode(0));
        all.AddRange(run.Handle(new DebugWin()));
        run.Handle(new SkipReward());
        run.Handle(new ChooseNode(1));
        all.AddRange(run.Handle(new DebugWin()));

        var earned = all.OfType<XPanelEarned>().Select(p => p.Key).ToList();
        Assert.Equal(earned.Distinct().Count(), earned.Count);
    }

    [Fact]
    public void Elk_paneel_heeft_een_unieke_sleutel()
    {
        Assert.Equal(XRegister.All.Count, XRegister.All.Select(p => p.Key).Distinct().Count());
    }
}
