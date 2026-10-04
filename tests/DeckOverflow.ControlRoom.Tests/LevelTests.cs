using DeckOverflow.ControlRoom.Achievements;
using DeckOverflow.ControlRoom.Gambits;
using DeckOverflow.ControlRoom.Levels;

namespace DeckOverflow.ControlRoom.Tests;

/// <summary>
/// De ontwerpbedoeling per gevecht, vastgepind. Faalt er een na het bijstellen van getallen,
/// dan is het gevecht veranderd: kijk eerst met de Solver of het nog doet wat het moet doen.
/// </summary>
public class LevelTests
{
    private static Condition C(Check check, int value = 0, bool not = false) => new(check, value, not);

    private static Duel Play(string key, params Rule[] rules)
    {
        var duel = LevelCatalog.Start(LevelCatalog.Get(key), rules);
        duel.RunToEnd();
        return duel;
    }

    public static TheoryData<string> Keys => new(LevelCatalog.All.Select(l => l.Key));

    /// <summary>Verliezen of de shift laten aflopen: allebei geen overwinning (zie De Typograaf).</summary>
    [Theory, MemberData(nameof(Keys))]
    public void Alleen_slaan_wint_nergens(string key)
    {
        var level = LevelCatalog.Get(key);
        Assert.NotEqual(Outcome.PlayerWon, Play(key, [.. level.StartRules]).Outcome);
    }

    [Theory, MemberData(nameof(Keys))]
    public void Elk_gevecht_is_op_meerdere_manieren_te_winnen_met_drie_regels(string key)
    {
        var level = LevelCatalog.Get(key);
        var wins = Solver.Wins(level, Math.Min(3, level.Slots));
        Assert.True(wins.Count >= (level.Slots == 2 ? 1 : 2), $"{key}: {wins.Count} oplossingen");
    }

    [Fact]
    public void Stamper_herstellen_op_tijd_wint_maar_de_volgorde_telt()
    {
        var heal = new Rule(C(Check.MyHpBelow, 10), Move.PatchUp);
        Assert.Equal(Outcome.PlayerWon, Play("stamper", heal, Rule.Otherwise(Move.Whack)).Outcome);

        var wrongOrder = Play("stamper", Rule.Otherwise(Move.Whack), heal);
        Assert.Equal(Outcome.EnemyWon, wrongOrder.Outcome);
        Assert.Equal(0, wrongOrder.Stats(Side.Player).Checked[1]);
    }

    [Fact]
    public void Press_blokken_als_hij_opgeladen_is_wint()
    {
        var brace = new Rule(C(Check.FoeCharged), Move.HoldFirmly);
        Assert.Equal(Outcome.PlayerWon, Play("press", brace, Rule.Otherwise(Move.Whack)).Outcome);
        Assert.Equal(Outcome.EnemyWon, Play("press", Rule.Otherwise(Move.Whack), brace).Outcome);
    }

    [Fact]
    public void Metronome_opladen_tegen_zijn_schild_wint()
    {
        var duel = Play("metronome", new Rule(C(Check.FoeBlocking), Move.WindUp), Rule.Otherwise(Move.Whack));
        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
    }

    [Fact]
    public void Mender_blijft_na_zijn_laatste_herstelling_hangen_in_zijn_bovenste_regel()
    {
        var duel = LevelCatalog.Start(LevelCatalog.Get("mender"),
            [new Rule(C(Check.EveryNthTurn, 4), Move.PatchUp), Rule.Otherwise(Move.Whack)]);
        var events = new List<DuelEvent>();
        while (!duel.IsOver) events.AddRange(duel.Step());

        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
        Assert.Contains(new RepairEmpty(Side.Enemy), events);
    }

    [Fact]
    public void Goto_fail_bekijkt_zijn_regels_onder_de_dubbele_regel_nooit()
    {
        var duel = Play("goto-fail", new Rule(C(Check.FoeCharged), Move.HoldFirmly), Rule.Otherwise(Move.Whack));

        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
        var stats = duel.Stats(Side.Enemy);
        Assert.True(stats.Fired[2] > 0);
        Assert.Equal([0, 0], stats.Checked[3..]);
    }

    [Fact]
    public void Hetzelfde_regelbord_geeft_altijd_hetzelfde_duel()
    {
        Rule[] rules = [new(C(Check.FoeBlocking), Move.WindUp), Rule.Otherwise(Move.Whack)];
        var a = Play("metronome", rules);
        var b = Play("metronome", rules);
        Assert.Equal((a.Turn, a.Bot(Side.Player).Hp), (b.Turn, b.Bot(Side.Player).Hp));
    }

    [Fact]
    public void Sentry_laadt_op_bij_een_schild_of_als_jij_oplaadt()
    {
        var duel = LevelCatalog.Start(LevelCatalog.Get("sentry"), [Rule.Otherwise(Move.HoldFirmly)]);
        duel.Step();
        Assert.Contains(new WoundUp(Side.Enemy, false), duel.Step());
    }

    [Fact]
    public void Contrarian_slaat_alleen_zolang_jij_geen_schild_hebt()
    {
        var duel = LevelCatalog.Start(LevelCatalog.Get("contrarian"), [Rule.Otherwise(Move.HoldFirmly)]);
        duel.Step();
        Assert.Contains(new RuleFired(Side.Enemy, 2, Move.WindUp), duel.Step());
    }

    [Fact]
    public void Knight_Capital_blijft_hangen_in_zijn_oude_regel_als_die_eens_bereikt_wordt()
    {
        var level = LevelCatalog.Get("knight-capital");
        var locked = Solver.Wins(level, 3)
            .Select(w => DuelRecording.Of(level, w.Rules))
            .FirstOrDefault(r => r.Frames[^1].Snapshot.EnemyFired[0] > 0);
        Assert.NotNull(locked);

        // Eens zijn bovenste regel vuurt, vuurt alleen die nog: hij laadt op en doet niets anders meer
        var enemyRules = locked.Frames.SelectMany(f => f.Events).OfType<RuleFired>()
            .Where(f => f.Side == Side.Enemy).Select(f => f.RuleIndex).ToList();
        int first = enemyRules.IndexOf(0);
        Assert.All(enemyRules.Skip(first), index => Assert.Equal(0, index));
    }

    [Theory, MemberData(nameof(Keys))]
    public void Een_opname_speelt_hetzelfde_duel_als_de_motor(string key)
    {
        var level = LevelCatalog.Get(key);
        var recording = DuelRecording.Of(level, level.StartRules);
        var duel = LevelCatalog.Start(level, level.StartRules);
        duel.RunToEnd();

        Assert.Equal(duel.Outcome, recording.Outcome);
        Assert.Equal(duel.Turn, recording.Turns);
        Assert.Equal(duel.Bot(Side.Enemy).Hp, recording.Frames[^1].Snapshot.Enemy.Hp);
        Assert.Null(recording.Score);
    }

    [Fact]
    public void Een_gewonnen_opname_geeft_een_score_en_panelen()
    {
        var level = LevelCatalog.Get("stamper");
        var heal = new Rule(C(Check.MyHpBelow, 10), Move.PatchUp);
        var recording = DuelRecording.Of(level, [heal, Rule.Otherwise(Move.Whack)]);

        Assert.True(recording.Won);
        Assert.Equal(new Score(recording.Turns, 2), recording.Score);
        Assert.DoesNotContain(XRegister.DeadCode, recording.Panels);
    }

    [Fact]
    public void Winnen_met_een_regel_die_nooit_bekeken_werd_is_dode_code()
    {
        var recording = DuelRecording.Of(LevelCatalog.Get("metronome"),
            [new Rule(C(Check.FoeBlocking), Move.WindUp), Rule.Otherwise(Move.Whack), new Rule(C(Check.MyHpBelow, 10), Move.PatchUp)]);

        Assert.True(recording.Won);
        Assert.Contains(XRegister.DeadCode, recording.Panels);
    }

    [Fact]
    public void De_shift_laten_aflopen_verdient_overuren()
    {
        // Elke klap van de Metronome (6) verdwijnt in jouw schild (8), en jij slaat nooit
        var recording = DuelRecording.Of(LevelCatalog.Get("metronome"), [Rule.Otherwise(Move.HoldFirmly)]);

        Assert.Equal(Outcome.ShiftOver, recording.Outcome);
        Assert.Equal([XRegister.Overtime], recording.Panels);
    }

    [Fact]
    public void Winnen_zonder_schram_kan_ergens_en_verdient_een_paneel()
    {
        var flawless = LevelCatalog.All
            .SelectMany(l => Solver.Wins(l, 2).Where(w => w.HpLeft == LevelCatalog.Player.MaxHp).Select(w => (l, w)))
            .First();
        var recording = DuelRecording.Of(flawless.l, flawless.w.Rules);

        Assert.Contains(XRegister.Flawless, recording.Panels);
    }

    [Fact]
    public void Het_laatste_gevecht_winnen_sluit_de_Controlekamer_af()
    {
        var level = LevelCatalog.All[^1];
        var win = Solver.Wins(level, 3)[0];
        var recording = DuelRecording.Of(level, win.Rules);

        Assert.Contains(XRegister.ControlRoomCleared, recording.Panels);
    }

    [Fact]
    public void Elke_paneelsleutel_is_uniek()
    {
        Assert.Equal(XRegister.All.Count, XRegister.All.Select(p => p.Key).Distinct().Count());
    }

    [Fact]
    public void Cutter_kapt_elke_klap_af_maar_opgeladen_valt_er_niets_af()
    {
        var level = LevelCatalog.Get("cutter");
        var plain = LevelCatalog.Start(level, [Rule.Otherwise(Move.Whack)]);
        var hit = plain.Step();
        Assert.Contains(new ValueTruncated(Side.Enemy, 2.5, 2), hit);
        Assert.Contains(plain.Moments, m => m.Key == Core.Codex.CodexCatalog.IntTruncation && m.Values["before"] == "2.5");

        var charged = LevelCatalog.Start(level, [new Rule(C(Check.IAmCharged), Move.Whack), Rule.Otherwise(Move.WindUp)]);
        charged.Step(); charged.Step();
        var strike = charged.Step();
        Assert.Contains(new Whacked(Side.Player, 5, 0, 5, 19, true), strike);
        Assert.DoesNotContain(strike, e => e is ValueTruncated);
    }

    [Fact]
    public void Cutter_wint_veel_vaker_dankzij_de_halve_punt()
    {
        var level = LevelCatalog.Get("cutter");
        var whole = level with { Player = level.PlayerBot with { WhackDamage = 2 } };
        Assert.True(Solver.Wins(level, 3).Count > 5 * Solver.Wins(whole, 3).Count);
    }

    [Fact]
    public void Overload_wie_oplapt_boven_215_loopt_over()
    {
        var level = LevelCatalog.Get("overload");
        var greedy = DuelRecording.Of(level, [new Rule(C(Check.MyHpBelow, 240), Move.PatchUp), Rule.Otherwise(Move.Whack)]);

        Assert.Contains(greedy.Frames.SelectMany(f => f.Events), e => e is ValueOverflowed);
        Assert.Contains(greedy.Moments, m => m.Key == Core.Codex.CodexCatalog.Overflow && m.Values["type"] == "byte");
        Assert.NotEqual(Outcome.PlayerWon, greedy.Outcome);
    }

    [Fact]
    public void Overload_is_niet_te_winnen_zonder_oplappen_en_wel_met_een_veilige_grens()
    {
        var wins = Solver.Wins(LevelCatalog.Get("overload"), 3);
        Assert.DoesNotContain(wins, w => w.Rules.All(r => r.Then != Move.PatchUp));
        Assert.Contains(wins, w => w.Rules.Any(r => r.Then == Move.PatchUp && r.When.Check == Check.MyHpBelow && r.When.Value <= 215));
    }

    [Fact]
    public void Een_byte_die_precies_op_nul_overloopt_valt_door_zijn_eigen_zet_om()
    {
        var me = new BotSpec("player", MaxHp: 216, WhackDamage: 1, BlockAmount: 0, RepairAmount: 40, Repairs: 1, Core.Values.ValueKind.Byte);
        var duel = new Duel(me, [Rule.Otherwise(Move.PatchUp)], new BotSpec("dummy", 50, 1, 0, 0, 0), []);

        var events = duel.Step();

        Assert.Contains(new ValueOverflowed(Side.Player, 216, 40, 0, 0), events);
        Assert.Equal(Outcome.EnemyWon, duel.Outcome);
    }

    [Fact]
    public void Telex_een_klap_plakt_en_de_komma_telt_als_teken()
    {
        var duel = LevelCatalog.Start(LevelCatalog.Get("telex"), [Rule.Otherwise(Move.Whack)]);
        var hit = duel.Step();

        Assert.Contains(new TextAppended(Side.Enemy, "40", "5.5", "405.5"), hit);
        Assert.Contains(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.StringConcat);
    }

    [Fact]
    public void Telex_crasht_als_zijn_tekst_te_lang_wordt()
    {
        var level = LevelCatalog.Get("telex");
        var win = DuelRecording.Of(level, Solver.Wins(level, 3)[0].Rules);

        var crash = Assert.Single(win.Frames.SelectMany(f => f.Events).OfType<TextCrashed>());
        Assert.True(crash.Length >= 18);
        Assert.Contains(win.Moments, m => m.Key == Core.Codex.CodexCatalog.StringLength);
    }

    [Fact]
    public void Telex_wint_alleen_dankzij_de_komma()
    {
        var level = LevelCatalog.Get("telex");
        var whole = level with { Player = level.PlayerBot with { WhackDamage = 5 } };
        var wins = Solver.Wins(level, 3);
        Assert.NotEmpty(wins);
        Assert.Empty(Solver.Wins(whole, 3));
        Assert.True(wins.Count(w => w.Rules.All(r => r.Then != Move.WindUp)) > wins.Count(w => w.Rules.Any(r => r.Then == Move.WindUp)));
    }

    [Fact]
    public void Schatter_rondt_af_naar_het_dichtste_even_getal()
    {
        var duel = LevelCatalog.Start(LevelCatalog.Get("estimator"), [Rule.Otherwise(Move.Whack)]);
        var first = duel.Step();                    // beurt 1, jij: geen schild, 3.5 wordt 4
        Assert.Contains(new ValueRounded(Side.Enemy, 3.5, 4), first);
        duel.Step();                                // beurt 1, hij: slaat
        duel.Step();                                // beurt 2, jij
        var guarded = duel.Step();                  // beurt 2, hij: turn % 2 == 0, een schild van 1
        Assert.Contains(guarded, e => e is Braced);
        var second = duel.Step();                   // beurt 3, jij: 3.5 - 1 = 2.5 wordt 2
        Assert.Contains(new ValueRounded(Side.Enemy, 2.5, 2), second);
    }

    [Fact]
    public void Dag_248_valt_stil_als_zijn_teller_overloopt()
    {
        var level = LevelCatalog.Get("day-248");
        var recording = DuelRecording.Of(level, [Rule.Otherwise(Move.HoldFirmly)]);
        var events = recording.Frames.SelectMany(f => f.Events).ToList();

        var overflow = Assert.Single(events.OfType<CounterOverflowed>());
        Assert.Equal((255, 0), (overflow.Before, overflow.After));
        int at = events.IndexOf(overflow);
        Assert.All(events.Skip(at).OfType<RuleFired>(), f => Assert.Equal(Side.Player, f.Side));
        Assert.Contains(events.Skip(at), e => e is NoRuleMatched { Side: Side.Enemy });
        Assert.Contains(recording.Moments, m => m.Key == Core.Codex.CodexCatalog.Overflow && m.Values["added"] == "1");
    }

    [Fact]
    public void Reus_omgieten_houdt_hp_modulo_256_over()
    {
        var level = LevelCatalog.Get("giant");
        var duel = new Duel(level.PlayerBot, [Rule.Otherwise(Move.CastToByte)], level.Enemy, []);
        var cast = duel.Step();
        Assert.Contains(new TypeChanged(Side.Enemy, Core.Values.ValueKind.Int, Core.Values.ValueKind.Byte, 600, 88, true, "cast"), cast);
        Assert.Contains(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.Casting);
    }

    [Fact]
    public void Reus_meteen_omgieten_wint_pas_onder_500_niet()
    {
        var level = LevelCatalog.Get("giant");
        var early = DuelRecording.Of(level, [new Rule(C(Check.FoeHpBelow, 500), Move.Whack), Rule.Otherwise(Move.CastToByte)]);
        var late = DuelRecording.Of(level, [new Rule(C(Check.FoeHpBelow, 300), Move.Whack), new Rule(C(Check.FoeHpBelow, 500), Move.CastToByte), Rule.Otherwise(Move.Whack)]);

        Assert.Equal(Outcome.PlayerWon, early.Outcome);
        Assert.NotEqual(Outcome.PlayerWon, late.Outcome);
        Assert.All(Solver.Wins(level, 3), w => Assert.Contains(w.Rules, r => r.Then == Move.CastToByte));
    }

    [Fact]
    public void Een_regel_altijd_omgieten_bovenaan_blijft_vuren_en_verspilt_elke_beurt()
    {
        var recording = DuelRecording.Of(LevelCatalog.Get("giant"), [Rule.Otherwise(Move.CastToByte), Rule.Otherwise(Move.Whack)]);
        Assert.True(recording.Frames.SelectMany(f => f.Events).OfType<TypeUnchanged>().Count() > 5);
        Assert.NotEqual(Outcome.PlayerWon, recording.Outcome);
    }

    [Fact]
    public void Omgieten_op_een_byte_verandert_niets()
    {
        var level = LevelCatalog.Get("giant");
        var duel = new Duel(level.PlayerBot, [Rule.Otherwise(Move.CastToByte)], level.Enemy, []);
        duel.Step(); duel.Step();
        Assert.Contains(new TypeUnchanged(Side.Enemy, Core.Values.ValueKind.Byte), duel.Step());
    }

    [Fact]
    public void Titaan_Convert_crasht_boven_255_en_zijn_volgende_zet_valt_weg()
    {
        var level = LevelCatalog.Get("titan");
        var duel = LevelCatalog.Start(level, [Rule.Otherwise(Move.ConvertToByte)]);

        Assert.Contains(new ConversionCrashed(Side.Enemy, 480), duel.Step());
        Assert.Contains(new MoveSkipped(Side.Enemy), duel.Step());
        // Je voelt de crash, maar de naam (exception) komt pas in H10: hier opent ze geen pagina
        Assert.DoesNotContain(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.Exceptions);
        Assert.DoesNotContain(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.Convert);
    }

    [Fact]
    public void Titaan_Convert_onder_256_lukt_gewoon_en_maakt_hem_een_byte()
    {
        var level = LevelCatalog.Get("titan");
        var duel = new Duel(level.PlayerBot, [Rule.Otherwise(Move.ConvertToByte)], level.Enemy with { MaxHp = 200 }, []);

        Assert.Contains(new TypeChanged(Side.Enemy, Core.Values.ValueKind.Int, Core.Values.ValueKind.Byte, 200, 200, false, "convert"), duel.Step());
        Assert.Contains(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.Convert && m.Values["value"] == "200");
        Assert.DoesNotContain(duel.Step(), e => e is MoveSkipped);
    }

    [Fact]
    public void Titaan_is_te_winnen_met_Convert()
    {
        Assert.Contains(Solver.Wins(LevelCatalog.Get("titan"), 3), w => w.Rules.Any(r => r.Then == Move.ConvertToByte));
    }

    // ---------- 12. De Typograaf: een char is een getal ----------

    [Fact]
    public void Typograaf_wie_alleen_mept_blijft_steken_op_een_hoofdletter()
    {
        var duel = Play("typographer", Rule.Otherwise(Move.Whack));

        Assert.Equal(Outcome.ShiftOver, duel.Outcome);
        // Hij staat tussen 'A' en 'a': daar vangt zijn schild elke gewone klap op
        int hp = duel.Bot(Side.Enemy).Hp;
        Assert.InRange(hp, 'A', 'a' - 1);
        Assert.Equal(Core.Values.ValueKind.Char, duel.Bot(Side.Enemy).Kind);
    }

    [Fact]
    public void Typograaf_opgeladen_kom_je_door_zijn_hoofdletters()
    {
        var duel = Play("typographer", new Rule(C(Check.IAmCharged), Move.Whack), new Rule(C(Check.FoeBlocking), Move.WindUp), Rule.Otherwise(Move.Whack));
        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
    }

    [Fact]
    public void Typograaf_een_klap_op_een_char_opent_de_pagina_met_letters()
    {
        var level = LevelCatalog.Get("typographer");
        var duel = LevelCatalog.Start(level, [Rule.Otherwise(Move.Whack)]);
        duel.Step();

        var moment = Assert.Single(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.CharIsNumber);
        Assert.Equal("'z' - 10", moment.Values["expression"]);
        Assert.Equal("'p' (112)", moment.Values["value"]);
    }

    [Fact]
    public void Typograaf_zijn_regel_staat_in_C_sharp_met_letters()
    {
        var caps = LevelCatalog.Get("typographer").EnemyRules[0];
        Assert.Equal("me.Hp < 'a' && !(me.Hp < 'A')", caps.Expression());
        Assert.True(LevelCatalog.Get("typographer").Condition(Check.FoeHpBelow, 'a').Char);
        Assert.False(LevelCatalog.Get("typographer").Condition(Check.MyHpBelow, 20).Char);
    }

    // ---------- 16. De Kassabon: parsen ----------

    [Fact]
    public void Kassabon_eerst_lezen_wint_snel()
    {
        var duel = Play("receipt", new Rule(C(Check.FoeHpBelow, 30), Move.Whack), Rule.Otherwise(Move.Parse));

        Assert.Equal(Outcome.PlayerWon, duel.Outcome);
        Assert.True(duel.Turn <= 6, $"{duel.Turn} beurten");
        Assert.Contains(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.Parse && m.Values["value"] == "20");
    }

    [Fact]
    public void Kassabon_lezen_na_een_klap_crasht_en_je_volgende_zet_valt_weg()
    {
        var level = LevelCatalog.Get("receipt");
        var duel = LevelCatalog.Start(level, [new Rule(C(Check.EveryNthTurn, 2), Move.Parse), Rule.Otherwise(Move.Whack)]);
        duel.Step(); duel.Step();   // beurt 1: "20" + 5.5

        Assert.Contains(new ParseCrashed(Side.Player, "205.5"), duel.Step());
        duel.Step();
        Assert.Contains(new MoveSkipped(Side.Player), duel.Step());
        // Je voelt de crash, maar de naam (exception) komt pas in H10
        Assert.DoesNotContain(duel.Moments, m => m.Key == Core.Codex.CodexCatalog.Exceptions);
    }

    [Fact]
    public void Kassabon_lezen_na_een_opgeladen_klap_maakt_er_een_groot_getal_van()
    {
        var level = LevelCatalog.Get("receipt");
        var duel = LevelCatalog.Start(level, [new Rule(C(Check.IAmCharged), Move.Whack), new Rule(C(Check.EveryNthTurn, 3), Move.Parse), Rule.Otherwise(Move.WindUp)]);
        var events = Enumerable.Range(0, 6).SelectMany(_ => duel.Step()).ToList();

        Assert.Contains(new TextParsed(Side.Enemy, "2011", 2011), events);
        Assert.Equal(2011, duel.Bot(Side.Enemy).Hp);
    }

    [Fact]
    public void Kassabon_tekst_vergelijk_je_niet_met_een_getal()
    {
        var level = LevelCatalog.Get("receipt");
        var duel = LevelCatalog.Start(level, [new Rule(C(Check.FoeHpBelow, 100), Move.WindUp), Rule.Otherwise(Move.Whack)]);
        Assert.DoesNotContain(duel.Step(), e => e is WoundUp);
    }

    [Fact]
    public void Kassabon_is_ook_te_winnen_zonder_lezen()
    {
        Assert.Contains(Solver.Wins(LevelCatalog.Get("receipt"), 2), w => w.Rules.All(r => r.Then != Move.Parse));
    }
}
