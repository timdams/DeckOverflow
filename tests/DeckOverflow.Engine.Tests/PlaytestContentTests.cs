using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Relics;
using DeckOverflow.Engine.Runs;

namespace DeckOverflow.Tests;

/// <summary>De events en relics die erbij kwamen voor de eerste playtest.</summary>
public class PlaytestContentTests
{
    private static Run OnPath(RunSetup setup, params (NodeKind, string?)[] steps) =>
        Run.Start(255, setup with { Map = ActMap.Path(steps), Opening = false });

    [Theory]
    [InlineData(150, 200)]
    [InlineData(250, 200)]   // bankiersafronding: 2.5 wordt 2
    [InlineData(350, 400)]   // 3.5 wordt 4
    [InlineData(50, 0)]      // 0.5 wordt 0
    [InlineData(149, 100)]
    public void De_Rounding_Desk_rondt_af_met_bankiersafronding(int gold, int rounded)
    {
        Assert.Equal(rounded, Adventures.RoundGold(gold));
    }

    [Fact]
    public void De_Rounding_Desk_toont_vooraf_wat_er_met_je_goud_gebeurt()
    {
        var run = OnPath(new RunSetup(Gold: 250), (NodeKind.Event, Adventures.RoundingDesk), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));

        var detail = run.Snapshot().Event!.Options[0].Detail;
        run.Handle(new ChooseEventOption(0));

        Assert.Equal("200", detail.Args!["rounded"]);
        Assert.Equal(200, run.Gold);
    }

    [Fact]
    public void De_Copy_Machine_kopieert_een_kaart_en_kost_HP()
    {
        var run = OnPath(new RunSetup(), (NodeKind.Event, Adventures.CopyMachine), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        int before = run.Deck.Count;
        string first = run.Deck[0].Id;

        run.Handle(new ChooseEventOption(0, DeckIndex: 0));

        Assert.Equal(before + 1, run.Deck.Count);
        Assert.Equal(first, run.Deck[^1].Id);
        Assert.Equal(50 - Adventures.CopyHpCost, run.Hp);
    }

    [Fact]
    public void De_Scrap_Bin_verwijdert_gratis_een_kaart_of_geeft_goud()
    {
        var toss = OnPath(new RunSetup(), (NodeKind.Event, Adventures.ScrapBin), (NodeKind.Rest, null));
        toss.Handle(new ChooseNode(0));
        int before = toss.Deck.Count;
        toss.Handle(new ChooseEventOption(0, DeckIndex: 0));
        Assert.Equal(before - 1, toss.Deck.Count);

        var dig = OnPath(new RunSetup(Gold: 10), (NodeKind.Event, Adventures.ScrapBin), (NodeKind.Rest, null));
        dig.Handle(new ChooseNode(0));
        dig.Handle(new ChooseEventOption(1));
        Assert.Equal(10 + Adventures.ScrapGold, dig.Gold);
    }

    [Fact]
    public void Tally_Counter_heelt_na_elke_winst_een_meer()
    {
        var run = OnPath(new RunSetup(Hp: 50, Relics: [RelicCatalog.TallyCounter]),
            (NodeKind.Fight, Bestiary.Slime), (NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        run.Handle(new ChooseNode(0));
        var first = run.Handle(new DebugWin());
        run.Handle(new SkipReward());
        run.Handle(new ChooseNode(1));
        var second = run.Handle(new DebugWin());

        // Op volle HP heelt hij niets zichtbaar; de teller loopt wel: 1, dan 2
        Assert.Equal(1, new TallyCounterRelic().HealAfterWin(1));
        Assert.Equal(2, new TallyCounterRelic().HealAfterWin(2));
        Assert.DoesNotContain(first.Concat(second), e => e is RunHpChanged { Amount: < 0 });
    }

    [Fact]
    public void Coin_Mold_geeft_meer_goud_afgerond_met_Math_Round()
    {
        var plain = OnPath(new RunSetup(Gold: 0), (NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        var mold = OnPath(new RunSetup(Gold: 0, Relics: [RelicCatalog.CoinMold]), (NodeKind.Fight, Bestiary.Slime), (NodeKind.Rest, null));
        plain.Handle(new ChooseNode(0));
        mold.Handle(new ChooseNode(0));

        plain.Handle(new DebugWin());
        mold.Handle(new DebugWin());

        // Zelfde seed, zelfde worp: Coin Mold geeft Math.Round(worp * 1.5)
        Assert.Equal((int)Math.Round(plain.Gold * CoinMoldRelic.Factor), mold.Gold);
    }

    [Fact]
    public void Ink_Well_maakt_de_eerste_Ink_gratis()
    {
        var relic = new InkWellRelic();

        Assert.True(relic.MakesFree(CardCatalog.Ink, 0));
        relic.OnMadeFree();
        Assert.False(relic.MakesFree(CardCatalog.Ink, 1));
        Assert.False(new InkWellRelic().MakesFree(CardCatalog.Strike, 0));
    }
}
