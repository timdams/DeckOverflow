using System.Globalization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Random;
using DeckOverflow.Engine.Relics;
using DeckOverflow.Engine.Text;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>
/// Eén gevecht. Een command gaat erin, een lijst events komt eruit.
/// Geen tijd, geen UI, geen System.Random.
/// </summary>
public sealed class Combat
{
    public const int PlayerId = 0;
    public const int EnemyId = 1;

    private readonly CombatSetup _setup;
    private readonly Combatant _player;
    private readonly Combatant _enemy;
    private readonly Deck _deck;
    private readonly List<Relic> _relics;
    private readonly List<ModifierEffect> _modifiers = [];
    private List<GameEvent> _events = [];
    private int _seq;

    /// <summary>Overschreven aanval (Zet op 1), alleen tot de vijand aanvalt.</summary>
    private Intent? _assignedAttack;
    /// <summary>De vijand crashte: zijn volgende aanval valt weg.</summary>
    private bool _enemyCrashed;
    private int _typeCycleIndex;
    private int _cardsPlayed;

    private Combat(CombatSetup setup, ulong seed)
    {
        _setup = setup;
        _player = new Combatant(PlayerId, setup.Player, isEnemy: false);
        _enemy = new Combatant(EnemyId, setup.Enemy.Stats, isEnemy: true);
        _deck = new Deck(setup.Deck, new SeededRng(seed));
        _relics = RelicCatalog.CreateAll(setup.Relics ?? []);
        Turn = 1;
        Energy = setup.MaxEnergy;
        _deck.Draw(setup.HandSize);
    }

    public int Turn { get; private set; }
    public int Energy { get; private set; }
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.Ongoing;

    /// <summary>Wat de vijand deze beurt van plan is.</summary>
    public Intent CurrentIntent =>
        _assignedAttack ?? _setup.Enemy.Pattern[(Turn - 1) % _setup.Enemy.Pattern.Count];

    public static Combat Start(CombatSetup setup, ulong seed) => new(setup, seed);

    /// <summary>Enige manier om de status te wijzigen.</summary>
    public IReadOnlyList<GameEvent> Handle(ICommand command)
    {
        _events = [];

        if (Outcome != CombatOutcome.Ongoing)
        {
            Emit(new PlayRejected(-1, "reject.combat-over"));
            return _events;
        }

        switch (command)
        {
            case PlayCard play: HandlePlayCard(play); break;
            case EndTurn: HandleEndTurn(); break;
            default: throw new ArgumentException($"Onbekend command: {command.GetType().Name}", nameof(command));
        }

        return _events;
    }

    public CombatSnapshot Snapshot() => new(
        Turn,
        Energy,
        _setup.MaxEnergy,
        [.. _deck.Hand.Select(c => new CardView(c.Id, CostOf(c), CardText.Of(c), c.Target, c.Kind, c.CastTo, CostOf(c) <= Energy))],
        _deck.DrawCount,
        _deck.DiscardCount,
        [View(_player, intent: null), View(_enemy, _enemy.IsDead ? null : CurrentIntent)],
        Outcome,
        [.. _modifiers.Select(Label)],
        [.. _relics.Select(r => new RelicView(r.Id, r.Counter))]);

    // ---------- Commands ----------

    private void HandlePlayCard(PlayCard play)
    {
        if (play.HandIndex < 0 || play.HandIndex >= _deck.Hand.Count)
        {
            Emit(new PlayRejected(play.HandIndex, "reject.not-in-hand"));
            return;
        }

        CardDefinition card = _deck.Hand[play.HandIndex];
        int cost = CostOf(card);

        if (cost > Energy)
        {
            Emit(new PlayRejected(play.HandIndex, "reject.no-energy"));
            return;
        }

        Combatant? target = ResolveTarget(card.Target, play.TargetId);
        if (target is null)
        {
            Emit(new PlayRejected(play.HandIndex, "reject.bad-target"));
            return;
        }

        if (card.Effect is CastEffect or ConvertEffect && target.Kind == card.CastTo)
        {
            Emit(new PlayRejected(play.HandIndex, "reject.already-type"));
            return;
        }

        if (card.Effect is ModifierEffect { Op: ModifierOp.Parse } && !_modifiers.Any(m => m.Operand.IsText))
        {
            Emit(new PlayRejected(play.HandIndex, "reject.nothing-to-read"));
            return;
        }

        // De getypeerde aanval: eerst uitrekenen wat de modifiers van deze kaart maken.
        // Compileert het niet, dan weigert de kaart. Crasht het, dan kost het je de beurt.
        Exception? crash = null;
        if (_modifiers.Count > 0 && ModifiedAmount(card) is { } input)
        {
            try
            {
                if (Evaluate(input.Amount, input.Kind).Value.IsText)
                {
                    Emit(new PlayRejected(play.HandIndex, "reject.text-not-number"));
                    return;
                }
            }
            catch (CompileError)
            {
                Emit(new PlayRejected(play.HandIndex, "reject.type-error"));
                return;
            }
            catch (Exception e) when (e is FormatException or OverflowException or DivideByZeroException)
            {
                crash = e;
            }
        }

        Energy -= cost;
        _deck.DiscardFromHand(play.HandIndex);
        Emit(new CardPlayed(card.Id, PlayerId, target.Id));
        PayRelics(card);
        _cardsPlayed++;

        if (crash is not null)
        {
            // Nog geen catch in act 1: een exception beëindigt gewoon je beurt
            string expression = Evaluate(ModifiedAmount(card)!.Value.Amount, ModifiedAmount(card)!.Value.Kind, describeOnly: true).Expression;
            _modifiers.Clear();
            Emit(new ExceptionThrown(crash.GetType().Name, expression));
            HandleEndTurn();
            return;
        }

        Apply(card, card.Effect, target);
        FireRelics();
        CheckOutcome();
    }

    private void HandleEndTurn()
    {
        Emit(new TurnEnded(Turn));
        _deck.DiscardHand();

        // Het schild van de vijand houdt één beurt van de speler stand
        if (_enemy.Block > 0)
        {
            Emit(new BlockExpired(EnemyId, _enemy.Block));
            _enemy.Block = 0;
        }

        // Vijand valt aan, tenzij hij crashte
        Intent attack = CurrentIntent;
        _assignedAttack = null;
        if (_enemyCrashed)
        {
            _enemyCrashed = false;
            Emit(new AttackSkipped(EnemyId));
        }
        else
        {
            Emit(new AttackLaunched(EnemyId, PlayerId, attack.Expression, attack.Value));
            DealDamage(_player, attack.Value, fromPlayer: false);
            if (CheckOutcome()) return;
        }

        // Helen en schild opbouwen volgen ook de regels van zijn (misschien omgegoten) type
        if (_setup.Enemy.HealAfterAttack > 0)
        {
            Heal(_enemy, _setup.Enemy.HealAfterAttack);
            if (CheckOutcome()) return;
        }
        if (_setup.Enemy.BlockAfterAttack > 0) GainBlock(_enemy, _setup.Enemy.BlockAfterAttack);
        if (_setup.Enemy.GrowthAfterAttack > 0) Grow(_enemy, _setup.Enemy.GrowthAfterAttack);
        if (_setup.Enemy.TypeCycle is { Count: > 0 } cycle)
        {
            _typeCycleIndex = (_typeCycleIndex + 1) % cycle.Count;
            if (cycle[_typeCycleIndex] != _enemy.Kind) Cast(_enemy, cycle[_typeCycleIndex]);
            if (CheckOutcome()) return;
        }

        // Nieuwe beurt
        Turn++;
        Energy = _setup.MaxEnergy;
        if (_player.Block > 0)
        {
            Emit(new BlockExpired(PlayerId, _player.Block));
            _player.Block = 0;
        }
        _deck.Draw(_setup.HandSize);
        Emit(new TurnStarted(Turn, Energy));

        Intent next = CurrentIntent;
        Emit(new IntentRevealed(EnemyId, next.Expression, next.Hidden ? null : next.Value));
    }

    // ---------- Kaarteffecten ----------

    private void Apply(CardDefinition card, Effect effect, Combatant target)
    {
        switch (effect)
        {
            case DamageEffect d:
            {
                (double perHit, int hitFactor) = Modify(card, d.Amount, card.Kind);
                for (int hit = 0; hit < d.Hits * hitFactor && !target.IsDead; hit++) DealDamage(target, perHit, fromPlayer: true);
                break;
            }

            case BlockEffect b: GainBlock(target, Modify(card, b.Amount, ValueKind.Int).Value); break;
            case HealEffect h: Heal(target, (int)Modify(card, h.Amount, ValueKind.Int).Value); break;

            // Een combo giet niet om naar wat hij al is, maar doet de rest wel
            case CastEffect c when target.Kind != c.To: Cast(target, c.To); break;
            case CastEffect: break;

            case ConvertEffect c when target.Kind != c.To: Convert(target, c.To); break;
            case ConvertEffect: break;

            case SetAttackEffect s: AssignAttack(s.Value); break;
            case ModifierEffect m: QueueModifier(m); break;

            case ComboEffect combo:
                Apply(card, combo.First, target);
                if (!target.IsDead) Apply(card, combo.Then, target);
                break;
        }
    }

    /// <summary>Toekenning: wat er stond, doet er niet meer toe.</summary>
    private void AssignAttack(int value)
    {
        Intent before = CurrentIntent;
        _assignedAttack = new Intent(value.ToString(CultureInfo.InvariantCulture), value);
        Emit(new IntentAssigned(EnemyId, before.Expression, value));
    }

    private void QueueModifier(ModifierEffect m)
    {
        _modifiers.Add(m);
        Emit(new ModifierQueued(Label(m), string.Join(" ", _modifiers.Select(Label))));
    }

    /// <summary>Het getal van deze kaart waarop de modifiers zouden vallen, en het type ervan. Leeg als ze er geen gebruikt.</summary>
    private static (double Amount, ValueKind? Kind)? ModifiedAmount(CardDefinition card) => FindAmount(card.Effect, card.Kind);

    private static (double Amount, ValueKind? Kind)? FindAmount(Effect effect, ValueKind? kind) => effect switch
    {
        DamageEffect d => (d.Amount, kind),
        BlockEffect b => (b.Amount, ValueKind.Int),
        HealEffect h => (h.Amount, ValueKind.Int),
        ComboEffect c => FindAmount(c.First, kind) ?? FindAmount(c.Then, kind),
        _ => null
    };

    /// <summary>
    /// Rekent de wachtende modifiers in volgorde uit op een waarde met een type, met echte C#:
    /// eerst +3 en dan ×2 geeft (6 + 3) × 2, en <c>7 / 2</c> is 3 maar <c>7.0 / 2</c> is 3.5.
    /// Met <paramref name="describeOnly"/> stopt het bij de eerste fout en geeft het alleen de expressie tot daar.
    /// </summary>
    private (TypedValue Value, int HitFactor, string Expression) Evaluate(double amount, ValueKind? kind, bool describeOnly = false)
    {
        TypedValue value = TypedValue.OfCard(amount, kind);
        string expression = Num(amount);
        int hitFactor = 1;

        foreach (ModifierEffect m in _modifiers)
        {
            string lit = m.Operand.Literal;
            string grouped = expression.Contains('+') || expression.Contains('-') ? $"({expression})" : expression;
            (expression, Func<TypedValue> step) = m.Op switch
            {
                ModifierOp.Add => ($"{expression} + {lit}", () => value.Plus(m.Operand)),
                ModifierOp.Multiply => ($"{grouped} × {lit}", () => value.Times(m.Operand)),
                ModifierOp.Divide => ($"{grouped} / {lit}", () => value.DividedBy(m.Operand)),
                ModifierOp.Parse => ($"int.Parse({expression})", (Func<TypedValue>)(() => value.Parse())),
                _ => throw new ArgumentOutOfRangeException(nameof(m.Op))
            };
            if (m.DoubleHits) hitFactor *= 2;

            if (describeOnly)
            {
                try { value = step(); }
                catch (Exception) { break; }
            }
            else
            {
                value = step();
            }
        }
        return (value, hitFactor, expression);
    }

    /// <summary>Past de wachtende modifiers toe op het getal van deze kaart. Al gecontroleerd bij het spelen.</summary>
    private (double Value, int HitFactor) Modify(CardDefinition card, double amount, ValueKind? kind)
    {
        if (_modifiers.Count == 0) return (amount, 1);

        var (value, hitFactor, expression) = Evaluate(amount, kind);
        _modifiers.Clear();
        Emit(new ModifiersApplied(card.Id, amount, value.Number, expression));
        return (value.Number, hitFactor);
    }

    // ---------- Relics ----------

    /// <summary>Wat een kaart nu kost. Bij twee relics die haar gratis kunnen maken, wint de eerste in de catalogus.</summary>
    private int CostOf(CardDefinition card) => FreeingRelic(card) is null ? card.Cost : 0;

    private Relic? FreeingRelic(CardDefinition card) =>
        card.Cost > 0 ? _relics.FirstOrDefault(r => r.MakesFree(card, _cardsPlayed)) : null;

    private void PayRelics(CardDefinition card)
    {
        if (FreeingRelic(card) is not { } relic) return;
        relic.OnMadeFree();
        Emit(new RelicTriggered(relic.Id));
    }

    /// <summary>Relics die schade afvuren, doen dat na de kaart, niet midden in een reeks treffers.</summary>
    private void FireRelics()
    {
        foreach (Relic relic in _relics)
        {
            while (!_enemy.IsDead && relic.TryFire(out double damage))
            {
                Emit(new RelicTriggered(relic.Id));
                DealDamage(_enemy, damage, fromPlayer: false);
            }
        }
    }

    // ---------- Regels ----------

    /// <summary>Eén treffer. Een geheel type kapt af; een double neemt de waarde exact.</summary>
    private void DealDamage(Combatant target, double amount, bool fromPlayer)
    {
        bool whole = CastRules.IsWhole(target.Kind);
        double remaining = amount;

        // De Rounder rondt af zolang hij een double is; als int kapt hij gewoon af
        if (target.IsEnemy && _setup.Enemy.RoundsIncoming && target.Kind == ValueKind.Double)
        {
            double rounded = Math.Round(remaining);
            if (rounded != remaining) Emit(new ValueRounded(target.Id, remaining, rounded, ValueSubject.Damage));
            remaining = rounded;
        }

        if (target.Block > 0 && remaining > 0)
        {
            // Blok van een geheel type: een halve schade kost een hele blokpunt
            double absorbed = Math.Min(target.Block, whole ? Math.Ceiling(remaining) : remaining);
            remaining = Math.Max(0, remaining - target.Block);
            target.Block -= absorbed;
            Emit(new BlockAbsorbed(target.Id, absorbed, target.Block));
        }

        if (remaining <= 0) return;

        double damage = remaining;
        if (whole)
        {
            (int truncated, double lost) = IntRules.Truncate(remaining);
            if (lost > 0)
            {
                Emit(new ValueTruncated(target.Id, remaining, truncated, lost, ValueSubject.Damage));
                if (fromPlayer && target.IsEnemy) _relics.ForEach(r => r.OnDamageTruncated(lost));
            }
            damage = truncated;
        }

        if (damage <= 0) return;

        double before = target.Hp;
        // Spelregel: schade stopt op 0. Underflow van byte is een mechaniek voor later.
        target.Hp = Math.Max(0, before - damage);
        Emit(new DamageDealt(target.Id, damage, before, target.Hp));

        if (target.IsDead) Emit(new CombatantDied(target.Id));
    }

    private void GainBlock(Combatant target, double amount)
    {
        if (CastRules.IsWhole(target.Kind))
        {
            (int truncated, double lost) = IntRules.Truncate(amount);
            if (lost > 0) Emit(new ValueTruncated(target.Id, amount, truncated, lost, ValueSubject.Block));
            amount = truncated;
        }

        if (amount <= 0) return;

        target.Block += amount;
        Emit(new BlockGained(target.Id, amount, target.Block));
    }

    private void Heal(Combatant target, int amount)
    {
        double before = target.Hp;

        switch (target.Kind)
        {
            case ValueKind.Byte:
                (byte result, bool overflowed) = ByteRules.Add((byte)before, amount);
                target.Hp = result;
                if (overflowed)
                {
                    Emit(new ValueOverflowed(target.Id, (int)before, amount, result, byte.MaxValue));
                }
                else
                {
                    Emit(new Healed(target.Id, amount, result));
                }
                break;

            default:
                // Spelregel: helen gaat niet boven max HP
                target.Hp = Math.Min(target.MaxHp, before + amount);
                Emit(new Healed(target.Id, target.Hp - before, target.Hp));
                break;
        }

        if (target.IsDead) Emit(new CombatantDied(target.Id));
    }

    /// <summary>Omgieten: HP en blok gaan door een echte C#-conversie.</summary>
    private void Cast(Combatant target, ValueKind to)
    {
        ValueKind from = target.Kind;
        double hpBefore = target.Hp;
        var hp = CastRules.Convert(hpBefore, to);

        if (hp.Lost > 0)
        {
            Emit(new ValueTruncated(target.Id, hpBefore, IntRules.Truncate(hpBefore).Result, hp.Lost, ValueSubject.Hp));
        }

        target.Kind = to;
        target.Hp = hp.Result;
        // Ook het schild verliest zijn restje
        if (CastRules.IsWhole(to)) target.Block = IntRules.Truncate(target.Block).Result;

        Emit(new TypeChanged(target.Id, from, to, hpBefore, target.Hp, target.MaxHp, target.Block, hp.Wrapped));

        if (target.IsDead) Emit(new CombatantDied(target.Id));
    }

    /// <summary>
    /// <c>Convert</c>: afronden in plaats van afkappen, en checked. Past het getal niet,
    /// dan verandert er niets aan het doelwit, maar crasht het en slaat het zijn aanval over.
    /// </summary>
    private void Convert(Combatant target, ValueKind to)
    {
        ValueKind from = target.Kind;
        double hpBefore = target.Hp;

        if (CastRules.ConvertChecked(hpBefore, to) is not { } hp)
        {
            Emit(new ConversionCrashed(target.Id, to, hpBefore));
            if (target.IsEnemy) _enemyCrashed = true;
            return;
        }

        if (hp != hpBefore) Emit(new ValueRounded(target.Id, hpBefore, hp, ValueSubject.Hp));
        target.Kind = to;
        target.Hp = hp;
        if (CastRules.IsWhole(to)) target.Block = Math.Round(target.Block);

        Emit(new TypeChanged(target.Id, from, to, hpBefore, target.Hp, target.MaxHp, target.Block, Wrapped: false));
        if (target.IsDead) Emit(new CombatantDied(target.Id));
    }

    /// <summary>Groei volgt het type: een int kapt <c>HP * factor</c> af, een byte klapt om.</summary>
    private void Grow(Combatant target, double factor)
    {
        double before = target.Hp;
        double raw = before * factor;
        var result = CastRules.Convert(raw, target.Kind);
        target.Hp = result.Result;
        Emit(new ValueGrew(target.Id, before, factor, raw, target.Hp));
        if (result.Lost > 0) Emit(new ValueTruncated(target.Id, raw, IntRules.Truncate(raw).Result, result.Lost, ValueSubject.Hp));
        if (target.IsDead) Emit(new CombatantDied(target.Id));
    }

    private bool CheckOutcome()
    {
        if (Outcome != CombatOutcome.Ongoing) return true;

        if (_enemy.IsDead) Outcome = CombatOutcome.Won;
        else if (_player.IsDead) Outcome = CombatOutcome.Lost;
        else return false;

        Emit(new CombatEnded(Outcome == CombatOutcome.Won));
        return true;
    }

    // ---------- Hulp ----------

    private Combatant? ResolveTarget(TargetMode mode, int targetId) => mode switch
    {
        TargetMode.Self => _player,
        TargetMode.Enemy when targetId == EnemyId && !_enemy.IsDead => _enemy,
        TargetMode.Any when targetId == PlayerId => _player,
        TargetMode.Any when targetId == EnemyId && !_enemy.IsDead => _enemy,
        _ => null
    };

    private void Emit(GameEvent e) => _events.Add(e with { Seq = ++_seq });

    private static string Num(double value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>Kort en zonder spaties, want de stage splitst de wachtende modifiers op een spatie: +3, ×1.0, /2, +"1".</summary>
    private static string Label(ModifierEffect m) => m.Op switch
    {
        ModifierOp.Add => $"+{m.Operand.Literal}",
        ModifierOp.Multiply => $"×{m.Operand.Literal}",
        ModifierOp.Divide => m.DoubleHits ? $"/{m.Operand.Literal}(×2)" : $"/{m.Operand.Literal}",
        _ => "int.Parse"
    };

    private static CombatantView View(Combatant c, Intent? intent) => new(
        c.Id, c.Key, c.Kind, c.Hp, c.MaxHp, c.Block, c.IsEnemy,
        intent is null ? null : new IntentView(intent.Expression, intent.Hidden ? null : intent.Value));
}
