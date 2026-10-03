using DeckOverflow.Engine.Achievements;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Codex;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Random;
using DeckOverflow.Engine.Relics;
using DeckOverflow.Engine.Text;

namespace DeckOverflow.Engine.Runs;

/// <param name="Map">Een vaste map voor de eerste act, voor tests. Leeg: de seed bepaalt de map.</param>
/// <param name="Opening">Begin bij de Gieterij met een keuze, of meteen op de map. Alleen in act 1.</param>
/// <param name="StartAct">Een startpunt: begin in een latere act, met een deck dat je eerst draft.</param>
public sealed record RunSetup(
    int Hp = 50,
    int Gold = 60,
    IReadOnlyList<CardDefinition>? Deck = null,
    IReadOnlyList<string>? Relics = null,
    ActMap? Map = null,
    bool Opening = true,
    int StartAct = 1);

/// <summary>
/// Eén run over de acts: map, gevechten, beloningen, en na elke baas de volgende act. Zoals
/// <see cref="Combat.Combat"/>: een command gaat erin, een lijst events komt eruit.
/// Gevechtscommands gaan door naar het gevecht.
/// </summary>
public sealed class Run
{
    // Prijzen en beloningen: eerste gokken, af te stellen in de playtest
    public const int RestHealPercent = 30;
    public const int RemovalPrice = 75;
    private static readonly (int Min, int Max) FightGold = (12, 20);
    private static readonly (int Min, int Max) EliteGold = (28, 40);
    private static readonly (int Min, int Max) TreasureGold = (20, 30);
    public const int FoundryGold = 100;

    // Een start in een latere act: ongeveer wat je had gehad als je had doorgespeeld
    public const int DraftRounds = 5;
    public const int LaterActGold = 100;
    public const int RelicChoices = 3;

    private ActDefinition _act;
    private ActMap _map;
    private readonly SeededRng _loot;
    private readonly List<CardDefinition> _deck;
    private readonly List<string> _relics;
    private readonly List<int> _visited = [];
    private List<GameEvent> _events = [];
    private int _seq;

    private Combat.Combat? _combat;
    private MapNode? _node;
    private RewardView? _reward;
    private List<CardDefinition> _rewardCards = [];
    private ShopState? _shop;
    private TextRef? _outcome;
    /// <summary>Het event dat nu open staat: de Gieterij bij de start, of dat van een knoop.</summary>
    private string? _eventKey;
    private string? _foundryRelic;
    private bool _jugMet;
    private TreasureState? _treasure;
    private EndView? _end;
    private int _draftRound;
    private List<string> _relicChoice = [];
    private string? _relicReason;
    /// <summary>Codex-pagina's die in deze run al opengingen; de shell onthoudt ze over runs heen.</summary>
    private readonly HashSet<string> _codex = [];
    /// <summary>✗-panelen die in deze run al verdiend werden; de shell onthoudt ze over runs heen.</summary>
    private readonly HashSet<string> _xpanels = [];
    /// <summary>De vijand van het lopende of laatste gevecht, voor panelen die bij één vijand horen.</summary>
    private string? _enemyKey;
    private int _combatsWon;
    /// <summary>Onderdelen die in deze run uit een kist kwamen.</summary>
    private readonly List<string> _trinkets = [];

    private Run(ulong seed, RunSetup setup)
    {
        Seed = seed;
        _act = Acts.Get(setup.StartAct);
        _map = setup.Map ?? GenerateMap(seed, _act);
        // Een aparte stroom voor loot, zodat de map niet verandert als er een beloning bijkomt
        _loot = new SeededRng(Mix(seed, 0x10075));
        _deck = [.. setup.Deck ?? CardCatalog.StarterDeck()];
        _relics = [.. setup.Relics ?? []];
        Hp = setup.Hp;
        MaxHp = setup.Hp;
        Gold = setup.Gold;

        if (_act.Number > 1)
        {
            // Een startpunt: eerst een deck draften uit de kaarten van de vorige acts
            Gold = LaterActGold;
            _draftRound = 1;
            _rewardCards = RollCards(3, DraftOdds, Acts.CardPool(_act.Number - 1));
            Phase = RunPhase.Draft;
        }
        else if (setup.Opening)
        {
            _eventKey = Adventures.Foundry;
            _foundryRelic = RandomUnownedRelic();
            Phase = RunPhase.Event;
        }
    }

    /// <summary>Act 1 houdt de map van de run-seed; elke volgende act krijgt een eigen, afgeleide seed.</summary>
    private static ActMap GenerateMap(ulong seed, ActDefinition act) =>
        MapGenerator.Generate(new SeededRng(act.Number == 1 ? seed : Mix(seed, 0xAC7000UL + (ulong)act.Number)), act);

    public ulong Seed { get; }
    public RunPhase Phase { get; private set; } = RunPhase.Map;
    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public int Gold { get; private set; }
    public IReadOnlyList<CardDefinition> Deck => _deck;
    public IReadOnlyList<string> Relics => _relics;
    public ActMap Map => _map;
    public ActDefinition Act => _act;

    public static Run Start(ulong seed, RunSetup? setup = null) => new(seed, setup ?? new RunSetup());

    /// <summary>
    /// Een opgenomen run opnieuw afspelen. De motor is deterministisch, dus dezelfde seed, setup
    /// en commands geven exact dezelfde run. Zo controleren we een score zonder de speler te vertrouwen.
    /// </summary>
    public static Run Replay(ulong seed, IEnumerable<ICommand> commands, RunSetup? setup = null)
    {
        var run = Start(seed, setup);
        foreach (var command in commands) run.Handle(command);
        return run;
    }

    /// <summary>Het lopende gevecht, voor de stage. Leeg buiten een gevecht.</summary>
    public CombatSnapshot? CombatSnapshot() => Phase == RunPhase.Combat ? _combat?.Snapshot() : null;

    /// <summary>Enige manier om de status te wijzigen.</summary>
    public IReadOnlyList<GameEvent> Handle(ICommand command)
    {
        _events = [];

        switch (Phase, command)
        {
            case (RunPhase.Combat, PlayCard or EndTurn or DebugWin): HandleCombat(command); break;
            case (RunPhase.Map, ChooseNode c): Enter(c.NodeId); break;
            case (RunPhase.Reward, TakeRewardCard t): TakeReward(t.Index); break;
            case (RunPhase.Reward, SkipReward): BackToMap(); break;
            case (RunPhase.Draft, TakeRewardCard t): Draft(t.Index); break;
            case (RunPhase.Draft, SkipReward): Draft(-1); break;
            case (RunPhase.RelicChoice, ChooseRelic r): PickRelic(r.Index); break;
            case (RunPhase.Rest, RestHeal): RestAndHeal(); break;
            case (RunPhase.Rest, RestUpgrade u): RestAndUpgrade(u.DeckIndex); break;
            case (RunPhase.Event, ChooseEventOption o): ChooseOption(o.Option, o.DeckIndex); break;
            case (RunPhase.Shop, BuyCard b): Buy(b.Index); break;
            case (RunPhase.Shop, BuyRelic): BuyShopRelic(); break;
            case (RunPhase.Shop, BuyRemoval r): BuyCardRemoval(r.DeckIndex); break;
            case (RunPhase.Treasure, OpenChest): Open(); break;
            case (RunPhase.Shop or RunPhase.Treasure or RunPhase.Rest or RunPhase.Event, Leave): LeaveNode(); break;
            default: Reject("reject.not-now"); break;
        }

        // ✗-panelen: wat deze events verdienen, één keer per run
        foreach (string panel in XRegister.Earned(_events, _enemyKey).ToList())
        {
            if (_xpanels.Add(panel)) Emit(new XPanelEarned(panel));
        }

        return _events;
    }

    public RunSnapshot Snapshot()
    {
        var reachable = Reachable().Select(n => n.Id).ToHashSet();
        var mapView = new MapView(
            _map.Rows,
            _map.Nodes.Max(n => n.Column) + 1,
            [.. _map.Nodes.Select(n => new MapNodeView(n.Id, n.Row, n.Column, n.Kind,
                _visited.Contains(n.Id), n.Id == _node?.Id, Phase == RunPhase.Map && reachable.Contains(n.Id)))],
            _map.Edges);

        return new RunSnapshot(
            Seed,
            Phase,
            CurrentHp,
            MaxHp,
            Gold,
            _node is null ? 0 : _node.Row + 1,
            [.. _deck.Select(CardInfo.From)],
            [.. _relics.Select(RelicInfo.From)],
            mapView,
            Phase == RunPhase.Reward ? _reward : null,
            Phase == RunPhase.Rest ? RestView() : null,
            Phase == RunPhase.Event ? EventView() : null,
            Phase == RunPhase.Shop ? ShopView() : null,
            Phase == RunPhase.Treasure && _treasure is { } t ? new TreasureView(t.Opened, t.Opened && t.Relic is { } r ? RelicInfo.From(r) : null, t.Opened ? t.Gold : 0, t.Opened ? t.Trinket : null) : null,
            _end,
            _act.Number,
            _act.Key,
            Phase == RunPhase.Draft ? new DraftView(_draftRound, DraftRounds, [.. _rewardCards.Select(CardInfo.From)]) : null,
            Phase == RunPhase.RelicChoice ? new RelicChoiceView(_relicReason!, [.. _relicChoice.Select(RelicInfo.From)]) : null,
            [.. _trinkets]);
    }

    /// <summary>Tijdens een gevecht is de HP van de speler in het gevecht de waarheid.</summary>
    private int CurrentHp => Phase == RunPhase.Combat && _combat is not null
        ? (int)_combat.Snapshot().Combatants.Single(c => !c.IsEnemy).Hp
        : Hp;

    // ---------- Map ----------

    private IEnumerable<MapNode> Reachable() =>
        _node is null ? _map.StartNodes : _map.Children(_node.Id);

    private void Enter(int nodeId)
    {
        MapNode? node = Reachable().FirstOrDefault(n => n.Id == nodeId);
        if (node is null)
        {
            Reject("reject.no-path");
            return;
        }

        _node = node;
        _visited.Add(node.Id);
        _outcome = null;
        Emit(new NodeEntered(node.Id, node.Kind));

        switch (node.Kind)
        {
            case NodeKind.Fight or NodeKind.Elite or NodeKind.Boss:
                StartCombat(EnemyFor(node), node.Id);
                break;
            case NodeKind.Rest:
                Phase = RunPhase.Rest;
                break;
            case NodeKind.Event when node.Encounter == Bestiary.Jug && !_jugMet:
                // Een onbekende knoop vroeg in de act: het wondermoment, één keer per run
                _jugMet = true;
                StartCombat(Bestiary.Jug, node.Id);
                break;
            case NodeKind.Event:
                // Een tweede kruik wordt een gewoon event
                _eventKey = Adventures.All.Contains(node.Encounter!) ? node.Encounter : _act.Events[_loot.NextInt(_act.Events.Count)];
                Phase = RunPhase.Event;
                break;
            case NodeKind.Shop:
                _shop = StockShop();
                Phase = RunPhase.Shop;
                break;
            case NodeKind.Treasure:
                _treasure = new TreasureState(RandomUnownedRelic(), Roll(TreasureGold), RollTrinket());
                Phase = RunPhase.Treasure;
                break;
        }
    }

    private void BackToMap()
    {
        _reward = null;
        _rewardCards = [];
        _shop = null;
        _treasure = null;
        _outcome = null;
        _eventKey = null;
        Phase = RunPhase.Map;
    }

    private void LeaveNode()
    {
        if (Phase == RunPhase.Treasure && _treasure is { Opened: false })
        {
            Reject("reject.chest-closed");
            return;
        }
        if (Phase is RunPhase.Rest or RunPhase.Event && _outcome is null)
        {
            Reject("reject.choose-first");
            return;
        }
        BackToMap();
    }

    // ---------- Gevecht ----------

    /// <summary>
    /// Flight 501 is alleen te verslaan door hem naar byte om te gieten. Zonder kaart die dat kan,
    /// krijg je een andere elite: elk gevecht moet op meer dan één manier te winnen zijn.
    /// </summary>
    private string EnemyFor(MapNode node)
    {
        string enemy = node.Encounter ?? throw new InvalidOperationException($"Knoop {node.Id} heeft geen vijand.");
        // The Label is tekst: zonder kaart die tekst omzet, valt hij niet te raken
        if (enemy == Bestiary.Label && !_deck.Any(c => c.Effect is ParseEffect or ConvertEffect)) return Bestiary.Rounder;
        if (enemy != Bestiary.Colossus || _deck.Any(CastsToByte)) return enemy;
        return _act.ElitePool.FirstOrDefault(e => e != Bestiary.Colossus) ?? Bestiary.Golem;
    }

    /// <summary>Een echte cast naar byte. Convert telt niet: die crasht op 506 in plaats van om te klappen.</summary>
    private static bool CastsToByte(CardDefinition card) => card.Effect switch
    {
        CastEffect { To: Values.ValueKind.Byte } => true,
        ComboEffect { First: CastEffect { To: Values.ValueKind.Byte } } => true,
        _ => false
    };

    private void StartCombat(string enemy, int nodeId)
    {
        double block = _relics.Sum(id => RelicCatalog.Create(id).BlockAtCombatStart);
        var setup = new CombatSetup(
            Scenarios.Player(Hp, MaxHp, block),
            Bestiary.Create(enemy),
            [.. _deck],
            Relics: [.. _relics]);

        _combat = Combat.Combat.Start(setup, Mix(Seed, (ulong)nodeId + 1));
        _enemyKey = enemy;
        Phase = RunPhase.Combat;
    }

    private void HandleCombat(ICommand command)
    {
        Combat.Combat combat = _combat!;
        foreach (GameEvent e in combat.Handle(command)) Emit(e);

        if (combat.Outcome == CombatOutcome.Ongoing) return;

        var snapshot = combat.Snapshot();
        var enemy = snapshot.Combatants.Single(c => c.IsEnemy);
        Hp = (int)snapshot.Combatants.Single(c => !c.IsEnemy).Hp;
        if (combat.Outcome == CombatOutcome.Won)
        {
            _combatsWon++;
            int heal = _relics.Sum(id => RelicCatalog.Create(id).HealAfterWin(_combatsWon));
            if (heal > 0) ChangeHp(heal);
        }
        UnlockCodex(combat, enemy, combat.Outcome == CombatOutcome.Won);

        if (combat.Outcome == CombatOutcome.Lost)
        {
            End(won: false, enemy);
            return;
        }

        if (_node!.Kind == NodeKind.Boss)
        {
            if (Acts.IsLast(_act)) End(won: true, enemy);
            else CompleteAct();
            return;
        }

        bool elite = _node.Kind == NodeKind.Elite;
        int gold = Roll(elite ? EliteGold : FightGold);
        double factor = _relics.Select(RelicCatalog.Create).Aggregate(1.0, (f, r) => f * r.GoldFactor);
        // Math.Round: 22.5 wordt 22, 23.5 wordt 24
        if (factor != 1.0) gold = (int)Math.Round(gold * factor);
        GainGold(gold);

        string? relic = elite ? RandomUnownedRelic() : null;
        if (relic is not null) GainRelic(relic);

        _rewardCards = RollCards(3, elite ? EliteOdds : FightOdds, CardPool);
        _reward = new RewardView(gold, relic is null ? null : RelicInfo.From(relic), [.. _rewardCards.Select(CardInfo.From)]);
        Phase = RunPhase.Reward;
    }

    private void End(bool won, CombatantView enemy)
    {
        _combat = null;
        Phase = won ? RunPhase.Won : RunPhase.Lost;
        _end = new EndView(won, _node!.Row + 1, enemy.Key, enemy.Hp, enemy.MaxHp, Gold, _deck.Count, _relics.Count, _act.Number);
        Emit(new RunEnded(won));
    }

    /// <summary>
    /// Na een gevecht: elke regel die erin iets deed, krijgt zijn Codex-pagina, als de act ver genoeg is.
    /// Eerst ervaren, dan benoemen: Omgieten in act 1 opent "Casting" nog niet.
    /// </summary>
    private void UnlockCodex(Combat.Combat combat, CombatantView enemy, bool won)
    {
        var moments = combat.Moments.ToList();
        // De Rekenmeester verslaan is operatorvoorrang doorhebben
        if (won && enemy.Key == Bestiary.Reckoner)
        {
            moments.Add(new CodexMoment(CodexCatalog.OperatorPrecedence, new Dictionary<string, string>
            {
                ["expression"] = "3 + 2 * 4", ["value"] = (3 + 2 * 4).ToString(System.Globalization.CultureInfo.InvariantCulture),
            }));
        }

        foreach (var moment in moments)
        {
            if (CodexCatalog.Get(moment.Key).MinAct > _act.Number || !_codex.Add(moment.Key)) continue;
            Emit(new CodexUnlocked(moment.Key, moment.Values));
        }
    }

    // ---------- Tussen de acts ----------

    /// <summary>Na de baas: volledig helen en een relic kiezen. Daarna begint de volgende act.</summary>
    private void CompleteAct()
    {
        _combat = null;
        Emit(new ActCompleted(_act.Number));
        ChangeHp(MaxHp - Hp);
        OfferRelics("boss");
    }

    private void StartNextAct()
    {
        _act = Acts.Get(_act.Number + 1);
        _map = GenerateMap(Seed, _act);
        _node = null;
        _visited.Clear();
        BackToMap();
        Emit(new ActStarted(_act.Number));
    }

    /// <summary>Drie relics om uit te kiezen. Zijn er geen meer over, dan gaat het meteen verder.</summary>
    private void OfferRelics(string reason)
    {
        _relicReason = reason;
        _relicChoice = [];
        var pool = RelicCatalog.All.Where(id => !_relics.Contains(id)).ToList();
        _loot.Shuffle(pool);
        _relicChoice = [.. pool.Take(RelicChoices)];

        if (_relicChoice.Count == 0) AfterRelicChoice();
        else Phase = RunPhase.RelicChoice;
    }

    private void PickRelic(int index)
    {
        if (index < 0 || index >= _relicChoice.Count)
        {
            Reject("reject.relic-not-offered");
            return;
        }
        GainRelic(_relicChoice[index]);
        AfterRelicChoice();
    }

    private void AfterRelicChoice()
    {
        _relicChoice = [];
        if (_relicReason == "boss") StartNextAct();
        else BackToMap();
    }

    /// <summary>Een kaart uit de draft nemen (of -1 om over te slaan), tot alle rondes voorbij zijn.</summary>
    private void Draft(int index)
    {
        if (index >= _rewardCards.Count)
        {
            Reject("reject.card-not-offered");
            return;
        }
        if (index >= 0) AddCard(_rewardCards[index]);

        if (_draftRound < DraftRounds)
        {
            _draftRound++;
            _rewardCards = RollCards(3, DraftOdds, Acts.CardPool(_act.Number - 1));
            return;
        }

        _rewardCards = [];
        OfferRelics("start");
    }

    private void TakeReward(int index)
    {
        if (index < 0 || index >= _rewardCards.Count)
        {
            Reject("reject.card-not-offered");
            return;
        }
        AddCard(_rewardCards[index]);
        BackToMap();
    }

    // ---------- Rustvuur ----------

    private int RestHealAmount => MaxHp * RestHealPercent / 100;

    private RestView RestView() => new(
        RestHealAmount,
        [.. _deck.Select((c, i) => (c, i)).Where(x => CardCatalog.Upgrade(x.c) is not null).Select(x => x.i)],
        _outcome);

    private void RestAndHeal()
    {
        if (_outcome is not null) { Reject("reject.already-rested"); return; }
        int healed = ChangeHp(RestHealAmount);
        _outcome = TextRef.Of("rest.healed", ("amount", healed));
    }

    private void RestAndUpgrade(int deckIndex)
    {
        if (_outcome is not null) { Reject("reject.already-rested"); return; }
        if (!InDeck(deckIndex) || CardCatalog.Upgrade(_deck[deckIndex]) is not { } better)
        {
            Reject("reject.cannot-upgrade");
            return;
        }
        string before = _deck[deckIndex].Id;
        Transform(deckIndex, better);
        _outcome = TextRef.Of("rest.upgraded", ("from", before), ("to", better.Id));
    }

    // ---------- Events ----------

    private EventView EventView()
    {
        string key = _eventKey!;
        return new EventView(key, _outcome is null ? EventOptions(key) : [], _outcome);
    }

    /// <summary>Elke keuze heeft een label en een uitleg in <c>en.json</c>, met de getallen die erbij horen.</summary>
    private List<EventOptionView> EventOptions(string key)
    {
        int[] all = [.. Enumerable.Range(0, _deck.Count)];

        switch (key)
        {
            case Adventures.Foundry:
                return
                [
                    Option(TextRef.Of("event.foundry.card"), TextRef.Of("event.foundry.card.detail"), null, []),
                    _foundryRelic is { } relic
                        ? Option(TextRef.Of("event.foundry.relic", ("relic", relic)), RelicCatalog.Create(relic).Text, null, [])
                        : Option(TextRef.Of("event.foundry.no-relic"), TextRef.Of("event.foundry.no-relic.detail"), "reject.no-relic-left", []),
                    Option(TextRef.Of("event.foundry.gold", ("amount", FoundryGold)), TextRef.Of("event.foundry.gold.detail"), null, []),
                ];

            case Adventures.Crucible:
            {
                int[] pourable = [.. all.Where(i => CardCatalog.CanPour(_deck[i]))];
                return
                [
                    Option(TextRef.Of("event.crucible.pour"), TextRef.Of("event.crucible.pour.detail"),
                        pourable.Length > 0 ? null : "reject.no-floating-card", pourable),
                    Option(TextRef.Of("event.crucible.melt"), TextRef.Of("event.crucible.melt.detail", ("amount", Adventures.MeltHpCost)),
                        Hp > Adventures.MeltHpCost ? null : "reject.not-enough-hp", all),
                    Option(TextRef.Of("event.crucible.leave"), TextRef.Of("event.crucible.leave.detail"), null, []),
                ];
            }

            case Adventures.CopyMachine:
                return
                [
                    Option(TextRef.Of("event.copy-machine.copy"), TextRef.Of("event.copy-machine.copy.detail", ("amount", Adventures.CopyHpCost)),
                        Hp > Adventures.CopyHpCost ? null : "reject.not-enough-hp", all),
                    Option(TextRef.Of("event.copy-machine.leave"), TextRef.Of("event.copy-machine.leave.detail"), null, []),
                ];

            case Adventures.ScrapBin:
                return
                [
                    Option(TextRef.Of("event.scrap-bin.toss"), TextRef.Of("event.scrap-bin.toss.detail"), _deck.Count > 1 ? null : "reject.deck-too-small", all),
                    Option(TextRef.Of("event.scrap-bin.dig"), TextRef.Of("event.scrap-bin.dig.detail", ("amount", Adventures.ScrapGold)), null, []),
                ];

            case Adventures.RoundingDesk:
                return
                [
                    Option(TextRef.Of("event.rounding-desk.round"),
                        TextRef.Of("event.rounding-desk.round.detail", ("gold", Gold), ("rounded", Adventures.RoundGold(Gold))), null, []),
                    Option(TextRef.Of("event.rounding-desk.leave"), TextRef.Of("event.rounding-desk.leave.detail"), null, []),
                ];

            case Adventures.LeakingBarrel:
                return
                [
                    Option(TextRef.Of("event.leaking-barrel.catch"),
                        TextRef.Of("event.leaking-barrel.catch.detail", ("gold", Adventures.BarrelGold), ("hp", Adventures.BarrelHpCost)),
                        Hp > Adventures.BarrelHpCost ? null : "reject.not-enough-hp", []),
                    Option(TextRef.Of("event.leaking-barrel.leave"), TextRef.Of("event.leaking-barrel.leave.detail"), null, []),
                ];

            default:
                return [];
        }

        static EventOptionView Option(TextRef label, TextRef detail, string? disabled, int[] cards) =>
            new(label, detail, disabled is null, disabled, cards);
    }

    private void ChooseOption(int option, int deckIndex)
    {
        if (_outcome is not null) { Reject("reject.already-chosen"); return; }

        var options = EventOptions(_eventKey!);
        if (option < 0 || option >= options.Count)
        {
            Reject("reject.no-such-option");
            return;
        }
        if (!options[option].Enabled)
        {
            Reject(options[option].DisabledReason!);
            return;
        }
        if (options[option].EligibleCards.Count > 0 && !options[option].EligibleCards.Contains(deckIndex))
        {
            Reject("reject.pick-card");
            return;
        }

        switch (_eventKey, option)
        {
            case (Adventures.Foundry, 0):
                // Meteen naar een beloningsscherm: 1 kaart uit 3, zonder gewone kaarten
                _rewardCards = RollCards(3, FoundryOdds, CardPool);
                _reward = new RewardView(0, null, [.. _rewardCards.Select(CardInfo.From)]);
                _eventKey = null;
                Phase = RunPhase.Reward;
                break;
            case (Adventures.Foundry, 1):
                GainRelic(_foundryRelic!);
                _outcome = TextRef.Of("event.foundry.relic.outcome", ("relic", _foundryRelic!));
                break;
            case (Adventures.Foundry, 2):
                GainGold(FoundryGold);
                _outcome = TextRef.Of("event.foundry.gold.outcome");
                break;

            case (Adventures.Crucible, 0):
            {
                CardDefinition before = _deck[deckIndex];
                CardDefinition poured = CardCatalog.Pour(before);
                Transform(deckIndex, poured);
                _outcome = TextRef.Of("event.crucible.pour.outcome", ("from", before.Id), ("to", poured.Id));
                break;
            }
            case (Adventures.Crucible, 1):
            {
                string id = _deck[deckIndex].Id;
                RemoveCard(deckIndex);
                ChangeHp(-Adventures.MeltHpCost);
                _outcome = TextRef.Of("event.crucible.melt.outcome", ("card", id));
                break;
            }
            case (Adventures.CopyMachine, 0):
            {
                CardDefinition copy = _deck[deckIndex];
                ChangeHp(-Adventures.CopyHpCost);
                AddCard(copy);
                _outcome = TextRef.Of("event.copy-machine.copy.outcome", ("card", copy.Id));
                break;
            }
            case (Adventures.ScrapBin, 0):
            {
                string id = _deck[deckIndex].Id;
                RemoveCard(deckIndex);
                _outcome = TextRef.Of("event.scrap-bin.toss.outcome", ("card", id));
                break;
            }
            case (Adventures.ScrapBin, 1):
                GainGold(Adventures.ScrapGold);
                _outcome = TextRef.Of("event.scrap-bin.dig.outcome");
                break;
            case (Adventures.RoundingDesk, 0):
            {
                int before = Gold;
                int rounded = Adventures.RoundGold(Gold);
                GainGold(rounded - before);
                _outcome = TextRef.Of("event.rounding-desk.round.outcome", ("gold", before), ("rounded", rounded));
                break;
            }
            case (Adventures.LeakingBarrel, 0):
                GainGold(Adventures.BarrelGold);
                ChangeHp(-Adventures.BarrelHpCost);
                _outcome = TextRef.Of("event.leaking-barrel.catch.outcome");
                break;
            default:
                _outcome = TextRef.Of("event.leave.outcome");
                break;
        }
    }

    // ---------- Winkel ----------

    private sealed class ShopState
    {
        public required List<(CardDefinition Card, int Price, bool Sold)> Cards { get; init; }
        public (string Id, int Price, bool Sold)? Relic { get; set; }
        public bool RemovalUsed { get; set; }
    }

    private ShopState StockShop()
    {
        var cards = new List<(CardDefinition, int, bool)>();
        foreach ((Rarity rarity, int count) in new[] { (Rarity.Common, 2), (Rarity.Uncommon, 2), (Rarity.Rare, 1) })
        {
            var pool = CardPool.Where(c => c.Rarity == rarity).ToList();
            _loot.Shuffle(pool);
            foreach (var card in pool.Take(count)) cards.Add((card, PriceOf(rarity), false));
        }

        string? relic = RandomUnownedRelic();
        return new ShopState
        {
            Cards = cards,
            Relic = relic is null ? null : (relic, 140 + _loot.NextInt(21), false),
        };
    }

    private int PriceOf(Rarity rarity) => rarity switch
    {
        Rarity.Common => 45 + _loot.NextInt(11),
        Rarity.Uncommon => 70 + _loot.NextInt(16),
        _ => 120 + _loot.NextInt(21),
    };

    private ShopView ShopView() => new(
        [.. _shop!.Cards.Select(c => new ShopCardView(CardInfo.From(c.Card), c.Price, c.Sold))],
        _shop.Relic is { } r ? new ShopRelicView(RelicInfo.From(r.Id), r.Price, r.Sold) : null,
        RemovalPrice,
        _shop.RemovalUsed);

    private void Buy(int index)
    {
        if (index < 0 || index >= _shop!.Cards.Count || _shop.Cards[index].Sold)
        {
            Reject("reject.card-sold");
            return;
        }
        var (card, price, _) = _shop.Cards[index];
        if (!Pay(price)) return;
        _shop.Cards[index] = (card, price, true);
        AddCard(card);
    }

    private void BuyShopRelic()
    {
        if (_shop!.Relic is not { Sold: false } offer)
        {
            Reject("reject.relic-sold");
            return;
        }
        if (!Pay(offer.Price)) return;
        _shop.Relic = offer with { Sold = true };
        GainRelic(offer.Id);
    }

    private void BuyCardRemoval(int deckIndex)
    {
        if (_shop!.RemovalUsed) { Reject("reject.one-removal"); return; }
        if (!InDeck(deckIndex)) { Reject("reject.not-in-deck"); return; }
        if (!Pay(RemovalPrice)) return;
        _shop.RemovalUsed = true;
        RemoveCard(deckIndex);
    }

    private bool Pay(int price)
    {
        if (price > Gold)
        {
            Reject("reject.not-enough-gold");
            return false;
        }
        GainGold(-price);
        return true;
    }

    // ---------- Schat ----------

    private sealed class TreasureState(string? relic, int gold, string? trinket)
    {
        public string? Relic { get; } = relic;
        public int Gold { get; } = gold;
        public string? Trinket { get; } = trinket;
        public bool Opened { get; set; }
    }

    /// <summary>Soms ligt er in een kist ook een onderdeel dat nergens voor dient, één van elk per run.</summary>
    private string? RollTrinket()
    {
        if (_loot.NextInt(100) >= Trinkets.ChestChance) return null;
        var left = Trinkets.All.Where(x => !_trinkets.Contains(x.Key)).ToList();
        return left.Count == 0 ? null : left[_loot.NextInt(left.Count)].Key;
    }

    private void Open()
    {
        var t = _treasure!;
        if (t.Opened) { Reject("reject.chest-open"); return; }
        t.Opened = true;
        GainGold(t.Gold);
        if (t.Relic is not null) GainRelic(t.Relic);
        if (t.Trinket is { } key)
        {
            _trinkets.Add(key);
            Emit(new TrinketFound(key, Trinkets.All.First(x => x.Key == key).Step));
        }
    }

    // ---------- Loot ----------

    /// <summary>Kans in procent op gewoon en ongewoon; de rest is zeldzaam.</summary>
    private readonly record struct CardOdds(int Common, int Uncommon);

    private static readonly CardOdds FightOdds = new(60, 33);
    private static readonly CardOdds EliteOdds = new(40, 45);
    private static readonly CardOdds FoundryOdds = new(0, 80);
    private static readonly CardOdds DraftOdds = new(40, 45);

    /// <summary>De beloningskaarten van deze act en alle acts ervoor.</summary>
    private IReadOnlyList<CardDefinition> CardPool => Acts.CardPool(_act.Number);

    /// <summary>Kaarten zonder dubbels. Een elite geeft meer kans op zeldzaam.</summary>
    private List<CardDefinition> RollCards(int count, CardOdds odds, IReadOnlyList<CardDefinition> source)
    {
        var picked = new List<CardDefinition>();
        for (int attempt = 0; picked.Count < count && attempt < 50; attempt++)
        {
            int roll = _loot.NextInt(100);
            Rarity rarity = roll < odds.Common ? Rarity.Common
                : roll < odds.Common + odds.Uncommon ? Rarity.Uncommon
                : Rarity.Rare;

            var pool = source.Where(c => c.Rarity == rarity && !picked.Contains(c)).ToList();
            if (pool.Count == 0) continue;
            picked.Add(pool[_loot.NextInt(pool.Count)]);
        }
        return picked;
    }

    private string? RandomUnownedRelic()
    {
        var pool = RelicCatalog.All.Where(id => !_relics.Contains(id)).ToList();
        return pool.Count == 0 ? null : pool[_loot.NextInt(pool.Count)];
    }

    private int Roll((int Min, int Max) range) => range.Min + _loot.NextInt(range.Max - range.Min + 1);

    // ---------- Status ----------

    private void GainGold(int amount)
    {
        Gold += amount;
        Emit(new GoldChanged(amount, Gold));
    }

    /// <summary>Verandert de HP buiten een gevecht, begrensd door 1 en max HP. Een event doodt je nooit.</summary>
    private int ChangeHp(int amount)
    {
        int before = Hp;
        Hp = Math.Clamp(Hp + amount, 1, MaxHp);
        Emit(new RunHpChanged(Hp - before, Hp, MaxHp));
        return Hp - before;
    }

    private void GainRelic(string id)
    {
        _relics.Add(id);
        Emit(new RelicGained(id));

        int bonus = RelicCatalog.Create(id).MaxHpOnGain;
        if (bonus > 0)
        {
            MaxHp += bonus;
            ChangeHp(bonus);
        }
    }

    private void AddCard(CardDefinition card)
    {
        _deck.Add(card);
        Emit(new CardAdded(card.Id));
    }

    private void RemoveCard(int deckIndex)
    {
        string id = _deck[deckIndex].Id;
        _deck.RemoveAt(deckIndex);
        Emit(new CardRemoved(id));
    }

    private void Transform(int deckIndex, CardDefinition to)
    {
        string from = _deck[deckIndex].Id;
        _deck[deckIndex] = to;
        Emit(new CardTransformed(from, to.Id));
    }

    private bool InDeck(int index) => index >= 0 && index < _deck.Count;

    private void Reject(string reason) => Emit(new RunRejected(reason));

    private void Emit(GameEvent e) => _events.Add(e with { Seq = ++_seq });

    /// <summary>SplitMix64: een vaste, goed gespreide seed per gevecht, afgeleid van de run-seed.</summary>
    private static ulong Mix(ulong seed, ulong salt)
    {
        ulong z = unchecked(seed + salt * 0x9E3779B97F4A7C15UL);
        z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
        z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
        return z ^ (z >> 31);
    }
}
