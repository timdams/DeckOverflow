using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Random;
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
    private List<GameEvent> _events = [];
    private int _seq;

    private Combat(CombatSetup setup, ulong seed)
    {
        _setup = setup;
        _player = new Combatant(PlayerId, setup.Player, isEnemy: false);
        _enemy = new Combatant(EnemyId, setup.Enemy.Stats, isEnemy: true);
        _deck = new Deck(setup.Deck, new SeededRng(seed));
        Turn = 1;
        Energy = setup.MaxEnergy;
        _deck.Draw(setup.HandSize);
    }

    public int Turn { get; private set; }
    public int Energy { get; private set; }
    public CombatOutcome Outcome { get; private set; } = CombatOutcome.Ongoing;

    public static Combat Start(CombatSetup setup, ulong seed) => new(setup, seed);

    /// <summary>Enige manier om de status te wijzigen.</summary>
    public IReadOnlyList<GameEvent> Handle(ICommand command)
    {
        _events = [];

        if (Outcome != CombatOutcome.Ongoing)
        {
            Emit(new PlayRejected(-1, "Het gevecht is al voorbij."));
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
        [.. _deck.Hand.Select(c => new CardView(c.Id, c.Name, c.Cost, c.Text, c.Target, c.Kind, c.Cost <= Energy))],
        _deck.DrawCount,
        _deck.DiscardCount,
        [View(_player, intent: null), View(_enemy, _enemy.IsDead ? null : _setup.Enemy.Attack)],
        Outcome);

    // ---------- Commands ----------

    private void HandlePlayCard(PlayCard play)
    {
        if (play.HandIndex < 0 || play.HandIndex >= _deck.Hand.Count)
        {
            Emit(new PlayRejected(play.HandIndex, "Die kaart zit niet in je hand. IndexOutOfRange, bijna."));
            return;
        }

        CardDefinition card = _deck.Hand[play.HandIndex];

        if (card.Cost > Energy)
        {
            Emit(new PlayRejected(play.HandIndex, "Geen energie meer. Zelfs een CPU moet ademen."));
            return;
        }

        Combatant? target = ResolveTarget(card.Target, play.TargetId);
        if (target is null)
        {
            Emit(new PlayRejected(play.HandIndex, "Dat doelwit past niet bij deze kaart."));
            return;
        }

        if (card.Effect is CastEffect cast && target.Kind == cast.To)
        {
            Emit(new PlayRejected(play.HandIndex, $"Die is al een {Name(cast.To)}. Omgieten verandert niets."));
            return;
        }

        Energy -= card.Cost;
        _deck.DiscardFromHand(play.HandIndex);
        Emit(new CardPlayed(card.Id, PlayerId, target.Id));

        switch (card.Effect)
        {
            case DamageEffect d:
                for (int hit = 0; hit < d.Hits && !target.IsDead; hit++) DealDamage(target, d.Amount);
                break;
            case BlockEffect b: GainBlock(target, b.Amount); break;
            case HealEffect h: Heal(target, h.Amount); break;
            case CastEffect c: Cast(target, c.To); break;
        }

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

        // Vijand valt aan
        Intent attack = _setup.Enemy.Attack;
        Emit(new AttackLaunched(EnemyId, PlayerId, attack.Expression, attack.Value));
        DealDamage(_player, attack.Value);
        if (CheckOutcome()) return;

        // Helen en schild opbouwen volgen ook de regels van zijn (misschien omgegoten) type
        if (_setup.Enemy.HealAfterAttack > 0)
        {
            Heal(_enemy, _setup.Enemy.HealAfterAttack);
            if (CheckOutcome()) return;
        }
        if (_setup.Enemy.BlockAfterAttack > 0) GainBlock(_enemy, _setup.Enemy.BlockAfterAttack);

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
        Emit(new IntentRevealed(EnemyId, attack.Expression, attack.Value));
    }

    // ---------- Regels ----------

    /// <summary>Eén treffer. Een geheel type kapt af; een double neemt de waarde exact.</summary>
    private void DealDamage(Combatant target, double amount)
    {
        bool whole = CastRules.IsWhole(target.Kind);
        double remaining = amount;

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
            if (lost > 0) Emit(new ValueTruncated(target.Id, remaining, truncated, lost, ValueSubject.Damage));
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

    private static string Name(ValueKind kind) => kind.ToString().ToLowerInvariant();

    private static CombatantView View(Combatant c, Intent? intent) => new(
        c.Id, c.Key, c.Name, c.Kind, c.Hp, c.MaxHp, c.Block, c.IsEnemy,
        intent is null ? null : new IntentView(intent.Expression, intent.Value));
}
