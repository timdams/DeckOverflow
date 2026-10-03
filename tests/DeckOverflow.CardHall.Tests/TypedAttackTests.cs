using DeckOverflow.CardHall.Cards;
using DeckOverflow.CardHall.Combat;
using DeckOverflow.CardHall.Commands;
using DeckOverflow.CardHall.Events;
using DeckOverflow.Core.Values;

namespace DeckOverflow.Tests;

/// <summary>
/// De getypeerde aanval: een aanval is een waarde met een C#-type, en modifiers zijn echte operatoren.
/// H2: <c>7 / 2</c> is 3, <c>7.0 / 2</c> is 3.5. H3: <c>"1" + "2"</c> is <c>"12"</c>.
/// </summary>
public class TypedAttackTests
{
    private const int P = Combat.PlayerId;
    private const int E = Combat.EnemyId;

    private static readonly CardDefinition StrikePlus = CardCatalog.Upgrade(CardCatalog.Strike)!;

    /// <summary>Read+ kost 1, zodat Ink, Read en een aanval in één beurt passen.</summary>
    private static readonly CardDefinition ReadPlus = CardCatalog.Upgrade(CardCatalog.Read)!;

    private static Combat Start(string enemy, params CardDefinition[] cards)
    {
        // Genoeg vulling, zodat StartWithHand een seed vindt met precies deze kaarten in de hand
        var deck = cards.Concat(Enumerable.Repeat(CardCatalog.Shield, 10 - cards.Length)).ToList();
        var setup = new CombatSetup(Scenarios.Player(), Bestiary.Create(enemy), deck);
        return TestHelpers.StartWithHand(setup, cards.Select(c => c.Id).ToArray());
    }

    // ---------- De regels zelf: echte C# ----------

    [Fact]
    public void Twee_ints_delen_geeft_een_deling_van_gehele_getallen()
    {
        Assert.Equal(TypedValue.Int(3), TypedValue.Int(7).DividedBy(TypedValue.Int(2)));
        Assert.Equal(TypedValue.Int(0), TypedValue.Int(1).DividedBy(TypedValue.Int(2)));
    }

    [Fact]
    public void Een_double_maakt_de_hele_expressie_double()
    {
        Assert.Equal(TypedValue.Double(3.5), TypedValue.Double(7).DividedBy(TypedValue.Int(2)));
        Assert.Equal(TypedValue.Double(7), TypedValue.Int(7).Times(TypedValue.Double(1.0)));
    }

    [Theory]
    [InlineData("1", "2", "12")]
    public void Tekst_plakt(string a, string b, string expected)
    {
        Assert.Equal(TypedValue.String(expected), TypedValue.String(a).Plus(TypedValue.String(b)));
    }

    [Fact]
    public void Een_getal_plakt_mee_als_tekst_zoals_C_sharp_het_schrijft()
    {
        Assert.Equal(TypedValue.String("71"), TypedValue.Int(7).Plus(TypedValue.String("1")));
        // C# schrijft 7.0 als "7", en 2.5 als "2.5"
        Assert.Equal(TypedValue.String("71"), TypedValue.Double(7).Plus(TypedValue.String("1")));
        Assert.Equal(TypedValue.String("2.51"), TypedValue.Double(2.5).Plus(TypedValue.String("1")));
    }

    [Fact]
    public void Parse_zet_tekst_om_en_crasht_op_ongeldige_tekst()
    {
        Assert.Equal(TypedValue.Int(71), TypedValue.String("71").Parse());
        Assert.Throws<FormatException>(() => TypedValue.String("2.51").Parse());
        Assert.Throws<OverflowException>(() => TypedValue.String("99999999999").Parse());
    }

    [Fact]
    public void Wat_niet_compileert_gooit_een_compileerfout()
    {
        Assert.Throws<CompileError>(() => TypedValue.Int(7).Parse());
        Assert.Throws<CompileError>(() => TypedValue.String("7").Times(TypedValue.Int(2)));
    }

    [Fact]
    public void Twee_ints_lopen_unchecked_over()
    {
        Assert.Equal(TypedValue.Int(int.MinValue), TypedValue.Int(int.MaxValue).Plus(TypedValue.Int(1)));
    }

    [Fact]
    public void Een_literal_staat_er_zoals_je_hem_in_C_sharp_typt()
    {
        Assert.Equal("7", TypedValue.Int(7).Literal);
        Assert.Equal("1.0", TypedValue.Double(1).Literal);
        Assert.Equal("2.5", TypedValue.Double(2.5).Literal);
        Assert.Equal("\"71\"", TypedValue.String("71").Literal);
    }

    // ---------- In een gevecht: H2 ----------

    [Fact]
    public void Split_slaat_twee_keer_de_helft_en_verliest_de_rest_aan_de_deling()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Split, StrikePlus);
        combat.Play(CardCatalog.Split.Id, P);

        var events = combat.Play(StrikePlus.Id, E).WithoutSeq();

        // 9 / 2 is 4, twee keer: 8 in plaats van 9
        Assert.Contains(new ModifiersApplied(StrikePlus.Id, 9, 4, "9 / 2"), events);
        Assert.Equal([4.0, 4.0], events.OfType<DamageDealt>().Select(d => d.Amount));
    }

    [Fact]
    public void Floating_Point_voor_Split_houdt_de_decimalen_op_een_double_vijand()
    {
        var combat = Start(Bestiary.Dripper, CardCatalog.FloatingPoint, CardCatalog.Split, StrikePlus);
        combat.Play(CardCatalog.FloatingPoint.Id, P);
        combat.Play(CardCatalog.Split.Id, P);

        var events = combat.Play(StrikePlus.Id, E).WithoutSeq();

        Assert.Contains(new ModifiersApplied(StrikePlus.Id, 9, 4.5, "9 × 1.0 / 2"), events);
        Assert.Equal(19.5 - 9, combat.Enemy().Hp);
    }

    [Fact]
    public void Op_een_int_vijand_sneuvelen_de_decimalen_alsnog_bij_aankomst()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.FloatingPoint, CardCatalog.Split, StrikePlus);
        combat.Play(CardCatalog.FloatingPoint.Id, P);
        combat.Play(CardCatalog.Split.Id, P);

        var events = combat.Play(StrikePlus.Id, E).WithoutSeq();

        Assert.Equal(2, events.OfType<ValueTruncated>().Count(t => t is { Before: 4.5, After: 4 }));
    }

    // ---------- In een gevecht: H3 ----------

    [Fact]
    public void Tekst_raakt_geen_getal_en_de_kaart_weigert_zonder_iets_te_kosten()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Ink, CardCatalog.Strike);
        combat.Play(CardCatalog.Ink.Id, P);
        int energy = combat.Energy;

        var events = combat.Play(CardCatalog.Strike.Id, E);

        Assert.Equal(new PlayRejected(combat.HandIndexOf(CardCatalog.Strike.Id), "reject.text-not-number"), Assert.Single(events) with { Seq = 0 });
        Assert.Equal(energy, combat.Energy);
        Assert.Equal(["+\"1\""], combat.Snapshot().Modifiers);
    }

    [Fact]
    public void Read_zet_de_geplakte_tekst_om_naar_een_groot_getal()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Ink, ReadPlus, CardCatalog.Strike);
        combat.Play(CardCatalog.Ink.Id, P);
        combat.Play(ReadPlus.Id, P);

        var events = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();

        // 6 + "1" is "61", en int.Parse("61") is 61
        Assert.Contains(new ModifiersApplied(CardCatalog.Strike.Id, 6, 61, "int.Parse(6 + \"1\")"), events);
        Assert.Equal(506 - 61, combat.Enemy().Hp);
    }

    [Fact]
    public void Read_zonder_tekst_weigert()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Read);

        var events = combat.Play(CardCatalog.Read.Id, P);

        Assert.Equal("reject.nothing-to-read", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
    }

    [Fact]
    public void Een_double_plakken_en_parsen_crasht_met_een_FormatException_en_kost_je_beurt()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Ink, ReadPlus, CardCatalog.FloatingStrike);
        combat.Play(CardCatalog.Ink.Id, P);
        combat.Play(ReadPlus.Id, P);

        var events = combat.Play(CardCatalog.FloatingStrike.Id, E).WithoutSeq();

        // 2.5 + "1" is "2.51", en dat is geen geheel getal
        Assert.Contains(new ExceptionThrown("FormatException", "int.Parse(2.5 + \"1\")"), events);
        Assert.Contains(events, e => e is TurnEnded);
        Assert.Contains(events, e => e is AttackLaunched);
        Assert.Equal(506, combat.Enemy().Hp);
        Assert.Empty(combat.Snapshot().Modifiers);
    }

    [Fact]
    public void Tekst_op_een_schild_compileert_ook_niet()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Ink, CardCatalog.Shield);
        combat.Play(CardCatalog.Ink.Id, P);

        var events = combat.Play(CardCatalog.Shield.Id, P);

        Assert.Equal("reject.text-not-number", Assert.IsType<PlayRejected>(Assert.Single(events)).Reason);
    }

    [Fact]
    public void Een_getal_bij_tekst_plakt_ook_Spare_Screw_na_Ink_geeft_meer_cijfers()
    {
        var combat = Start(Bestiary.Colossus, CardCatalog.Ink, CardCatalog.Add, ReadPlus, CardCatalog.Strike);
        combat.Play(CardCatalog.Ink.Id, P);
        combat.Play(CardCatalog.Add.Id, P);
        combat.Play(ReadPlus.Id, P);

        var events = combat.Play(CardCatalog.Strike.Id, E).WithoutSeq();

        // 6 + "1" + 3 is "613": een getal bij tekst plakt, het telt niet op
        Assert.Contains(new ModifiersApplied(CardCatalog.Strike.Id, 6, 613, "int.Parse(6 + \"1\" + 3)"), events);
        Assert.True(combat.Enemy().Hp == 0);
    }
}
