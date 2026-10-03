using DeckOverflow.Engine.Gambits;

namespace DeckOverflow.Tests;

public class DuelTests
{
    private static readonly BotSpec Hero = new("hero", MaxHp: 40, WhackDamage: 6, BlockAmount: 8, RepairAmount: 14, Repairs: 2);
    private static readonly BotSpec Dummy = new("dummy", MaxHp: 100, WhackDamage: 5, BlockAmount: 10, RepairAmount: 10, Repairs: 1);
    private static readonly Rule[] Idle = [];

    private static Condition C(Check check, int value = 0) => new(check, value);

    [Fact]
    public void De_eerste_regel_die_klopt_wint_en_de_rest_wordt_niet_bekeken()
    {
        Rule[] rules = [new(C(Check.MyHpBelow, 50), Move.WindUp), Rule.Otherwise(Move.Whack), new(C(Check.Always), Move.HoldFirmly)];
        var duel = new Duel(Hero, rules, Dummy, Idle);

        var events = duel.Step();

        Assert.Contains(new RuleFired(Side.Player, 0, Move.WindUp), events);
        Assert.Equal([1, 0, 0], duel.Stats(Side.Player).Checked);
        Assert.Equal([1, 0, 0], duel.Stats(Side.Player).Fired);
    }

    [Fact]
    public void Een_regel_onder_altijd_wordt_nooit_bekeken()
    {
        Rule[] rules = [Rule.Otherwise(Move.Whack), new(C(Check.MyHpBelow, 10), Move.PatchUp)];
        var duel = new Duel(Hero, rules, Dummy, [Rule.Otherwise(Move.Whack)]);

        duel.RunToEnd();

        Assert.Equal(0, duel.Stats(Side.Player).Checked[1]);
    }

    [Fact]
    public void Zonder_passende_regel_doet_een_automaat_niets()
    {
        var duel = new Duel(Hero, [new Rule(C(Check.FoeCharged), Move.Whack)], Dummy, Idle);

        var events = duel.Step();

        Assert.Contains(new NoRuleMatched(Side.Player), events);
        Assert.Equal(100, duel.Bot(Side.Enemy).Hp);
    }

    [Fact]
    public void Blok_vangt_een_zet_op_en_verdwijnt_bij_de_eigen_volgende_zet()
    {
        var duel = new Duel(Hero, [Rule.Otherwise(Move.HoldFirmly)], Dummy, [Rule.Otherwise(Move.Whack)]);

        duel.Step();                      // speler: blok 8
        var hit = duel.Step();            // vijand slaat 5, volledig opgevangen
        var next = duel.Step();           // speler: oud blok weg, nieuw blok 8

        Assert.Contains(new Whacked(Side.Enemy, 5, 5, 40, false), hit);
        Assert.Contains(new BlockExpired(Side.Player, 3), next);
        Assert.Equal(8, duel.Bot(Side.Player).Block);
    }

    [Fact]
    public void Opladen_verdubbelt_de_volgende_slag_en_raakt_dan_op()
    {
        Rule[] rules = [new(C(Check.IAmCharged), Move.Whack), Rule.Otherwise(Move.WindUp)];
        var duel = new Duel(Hero, rules, Dummy, Idle);

        duel.Step(); duel.Step();          // beurt 1: opladen
        var strike = duel.Step();          // beurt 2: dubbele slag

        Assert.Contains(new Whacked(Side.Player, 12, 0, 88, true), strike);
        Assert.False(duel.Bot(Side.Player).Charged);
    }

    [Fact]
    public void Lege_herstelling_vuurt_de_regel_toch_en_kost_de_beurt()
    {
        var hurt = Hero with { MaxHp = 40, Repairs = 0 };
        var duel = new Duel(hurt, [Rule.Otherwise(Move.PatchUp)], Dummy, Idle);

        var events = duel.Step();

        Assert.Contains(new RuleFired(Side.Player, 0, Move.PatchUp), events);
        Assert.Contains(new RepairEmpty(Side.Player), events);
    }

    [Fact]
    public void Herstel_gaat_nooit_boven_het_maximum()
    {
        var duel = new Duel(Hero, [Rule.Otherwise(Move.PatchUp)], Dummy, Idle);

        var events = duel.Step();

        Assert.Contains(new Repaired(Side.Player, 0, 40, 1), events);
    }

    [Fact]
    public void Elke_derde_beurt_kijkt_naar_het_beurtnummer()
    {
        var duel = new Duel(Hero, [new Rule(C(Check.EveryNthTurn, 3), Move.Whack)], Dummy, Idle);

        var fired = Enumerable.Range(0, 12).SelectMany(_ => duel.Step()).OfType<RuleFired>().Count();

        Assert.Equal(2, fired); // beurt 3 en 6
    }

    [Fact]
    public void Na_veertig_beurten_is_de_shift_voorbij()
    {
        var duel = new Duel(Hero, Idle, Dummy, Idle);

        Assert.Equal(Outcome.ShiftOver, duel.RunToEnd());
        Assert.Equal(Duel.MaxTurns, duel.Turn);
    }

    [Fact]
    public void Een_dode_vijand_slaat_niet_meer_terug()
    {
        var weak = Dummy with { MaxHp = 6 };
        var duel = new Duel(Hero, [Rule.Otherwise(Move.Whack)], weak, [Rule.Otherwise(Move.Whack)]);

        var events = duel.Step();

        Assert.Contains(new DuelEnded(Outcome.PlayerWon, 1), events);
        Assert.Empty(duel.Step());
        Assert.Equal(40, duel.Bot(Side.Player).Hp);
    }

    [Fact]
    public void EN_vraagt_dat_beide_voorwaarden_kloppen()
    {
        var rule = new Rule(C(Check.MyHpBelow, 50), Move.Whack, And: C(Check.FoeCharged));
        var duel = new Duel(Hero, [rule], Dummy, Idle);

        Assert.Contains(new NoRuleMatched(Side.Player), duel.Step());
    }
}
