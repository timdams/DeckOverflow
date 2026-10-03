using DeckOverflow.Engine.Achievements;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Codex;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Random;
using DeckOverflow.Engine.Runs;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Tests;

/// <summary>
/// Act 2, de Drukkerij (H3): een <c>char</c> is een getal, tekst plakt, en <c>Length</c> maakt van tekst een getal.
/// Plus The Counter, de tweede elite van act 1: een <c>int</c> die unchecked overloopt.
/// </summary>
public class PrintShopTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static CombatSetup Against(string enemy, params CardDefinition[] extra) =>
        new(Scenarios.Player(), Bestiary.Create(enemy), [.. TestHelpers.TestDeck, .. extra]);

    // ---------- Type Block: '0' is 48 ----------

    [Fact]
    public void Type_Block_heeft_48_HP_want_het_teken_0_is_48()
    {
        var combat = Combat.Start(Against(Bestiary.TypeBlock), 1);

        Assert.Equal(ValueKind.Char, combat.Enemy().Kind);
        Assert.Equal('0', combat.Enemy().Hp);
        Assert.Equal(48, combat.Enemy().Hp);
    }

    [Fact]
    public void Schade_op_een_char_trekt_af_van_zijn_code()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.TypeBlock), CardCatalog.Strike.Id);

        combat.Play(CardCatalog.Strike.Id, E);

        // '0' - 6 is '*' (42)
        Assert.Equal('*', combat.Enemy().Hp);
        Assert.Contains(combat.Moments, m => m.Key == CodexCatalog.CharIsNumber);
    }

    // ---------- Paper Golem: "1" + 2 is 12 ----------

    [Fact]
    public void De_Paper_Golem_valt_aan_met_tekst_die_plakt()
    {
        var pattern = Bestiary.Create(Bestiary.PaperGolem).Pattern;

        Assert.Equal(12, pattern[0].Value);
        Assert.Equal(3, pattern[1].Value);
    }

    // ---------- The Typesetter en Count Letters ----------

    [Fact]
    public void Count_Letters_maakt_van_zijn_zin_de_Length()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Typesetter, CardCatalog.CountLetters), CardCatalog.CountLetters.Id);
        string text = combat.Enemy().Text!;

        var events = combat.Play(CardCatalog.CountLetters.Id, E).WithoutSeq();

        Assert.Contains(new TextCounted(E, text, text.Length), events);
        Assert.Equal(ValueKind.Int, combat.Enemy().Kind);
        Assert.Equal(text.Length, combat.Enemy().Hp);
    }

    [Fact]
    public void Wie_eerst_slaat_en_dan_telt_vecht_tegen_een_langere_zin()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Typesetter, CardCatalog.CountLetters), CardCatalog.Strike.Id, CardCatalog.CountLetters.Id);
        int before = combat.Enemy().Text!.Length;
        combat.Play(CardCatalog.Strike.Id, E);

        combat.Play(CardCatalog.CountLetters.Id, E);

        Assert.Equal(before + 1, combat.Enemy().Hp);
    }

    [Fact]
    public void Om_de_drie_beurten_zet_hij_een_nieuwe_zin_met_jouw_schade_erin()
    {
        var combat = Combat.Start(Against(Bestiary.Typesetter), 1);

        combat.Handle(new EndTurn());
        combat.Handle(new EndTurn());
        var third = combat.Handle(new EndTurn()).WithoutSeq();

        var reset = Assert.Single(third.OfType<TextReset>());
        Assert.Contains("YOU HIT ME FOR 0", reset.Text);
        Assert.Equal(ValueKind.String, combat.Enemy().Kind);
    }

    [Fact]
    public void Count_Letters_op_een_getal_weigert()
    {
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Knight, CardCatalog.CountLetters), CardCatalog.CountLetters.Id);

        var events = combat.Play(CardCatalog.CountLetters.Id, E);

        Assert.Equal("reject.not-text", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
    }

    // ---------- Letter A: een char is een getal ----------

    [Fact]
    public void Een_letter_bij_een_getal_is_een_getal_en_bij_tekst_een_letter()
    {
        Assert.Equal(TypedValue.Int(71), TypedValue.Int(6).Plus(TypedValue.Char('A')));
        Assert.Equal(TypedValue.Int(66), TypedValue.Char('A').Plus(TypedValue.Int(1)));
        Assert.Equal(TypedValue.String("40A"), TypedValue.String("40").Plus(TypedValue.Char('A')));
        Assert.Equal("'A'", TypedValue.Char('A').Literal);
    }

    [Fact]
    public void Letter_A_maakt_van_een_Whack_71_schade()
    {
        var letterPlus = CardCatalog.Upgrade(CardCatalog.LetterA)!;
        var combat = TestHelpers.StartWithHand(Against(Bestiary.Colossus, letterPlus), letterPlus.Id, CardCatalog.Strike.Id);
        combat.Play(letterPlus.Id, P);

        var events = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();

        Assert.Contains(new ModifiersApplied(CardCatalog.Strike.Id, 6, 71, "6 + 'A'"), events);
        Assert.Equal(506 - 71, combat.Enemy().Hp);
    }

    // ---------- The Counter: int.MaxValue ----------

    [Fact]
    public void The_Counter_loopt_unchecked_over_als_je_hem_heelt()
    {
        var almost = Bestiary.Create(Bestiary.Counter) with
        {
            Stats = Bestiary.Create(Bestiary.Counter).Stats with { Hp = int.MaxValue - 3 }
        };
        var combat = TestHelpers.StartWithHand(new CombatSetup(Scenarios.Player(), almost, TestHelpers.TestDeck), CardCatalog.Mend.Id);

        var events = combat.Play(CardCatalog.Mend.Id, E).WithoutSeq();

        var overflow = Assert.Single(events.OfType<ValueOverflowed>());
        Assert.Equal(int.MaxValue, overflow.Max);
        Assert.True(overflow.After < 0);
        Assert.Equal(CombatOutcome.Won, combat.Outcome);
        Assert.Contains(XRegister.FeedTheMachine, XRegister.Earned(events, Bestiary.Counter));
    }

    [Fact]
    public void The_Counter_telt_zichzelf_naar_de_overflow_als_je_lang_genoeg_overleeft()
    {
        var tough = new CombatSetup(Scenarios.Player(300, 300), Bestiary.Create(Bestiary.Counter), TestHelpers.TestDeck);
        var combat = Combat.Start(tough, 1);

        for (int turn = 0; turn < 6 && combat.Outcome == CombatOutcome.Ongoing; turn++) combat.Handle(new EndTurn());

        Assert.Equal(CombatOutcome.Won, combat.Outcome);
    }

    // ---------- De acts ----------

    [Fact]
    public void Act_1_is_getallen_act_2_tekst_act_3_omzetten()
    {
        Assert.Equal([Acts.VatValley, Acts.PrintShop, Acts.MoldWorks], Acts.All);
        Assert.Equal([1, 2, 3], Acts.All.Select(a => a.Number));
        Assert.Contains(Bestiary.Counter, Acts.VatValley.ElitePool);
        Assert.DoesNotContain(Bestiary.EffectivePower, Acts.VatValley.ElitePool);
        Assert.Contains(Bestiary.EffectivePower, Acts.PrintShop.ElitePool);
    }

    [Theory]
    [InlineData(3UL)]
    [InlineData(17UL)]
    [InlineData(99UL)]
    public void De_map_van_de_Drukkerij_gebruikt_haar_vijanden_en_de_Typesetter(ulong seed)
    {
        var map = MapGenerator.Generate(new SeededRng(seed), Acts.PrintShop);

        Assert.Equal(Bestiary.Typesetter, map.Nodes.Single(n => n.Kind == NodeKind.Boss).Encounter);
        Assert.All(map.Nodes.Where(n => n.Kind == NodeKind.Elite), n => Assert.Contains(n.Encounter!, Acts.PrintShop.ElitePool));
    }
}
