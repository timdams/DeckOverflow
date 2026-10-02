using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Combat;
using DeckOverflow.Engine.Commands;
using DeckOverflow.Engine.Events;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Random;
using DeckOverflow.Engine.Relics;

namespace DeckOverflow.Engine.Runs;

/// <param name="Map">Een vaste map, voor tests. Leeg: de seed bepaalt de map.</param>
public sealed record RunSetup(
    int Hp = 50,
    int Gold = 60,
    IReadOnlyList<CardDefinition>? Deck = null,
    IReadOnlyList<string>? Relics = null,
    ActMap? Map = null);

/// <summary>
/// Eén run door een act: map, gevechten, beloningen. Zoals <see cref="Combat.Combat"/>:
/// een command gaat erin, een lijst events komt eruit. Gevechtscommands gaan door naar het gevecht.
/// </summary>
public sealed class Run
{
    // Prijzen en beloningen: eerste gokken, af te stellen in de playtest
    public const int RestHealPercent = 30;
    public const int RemovalPrice = 75;
    private static readonly (int Min, int Max) FightGold = (12, 20);
    private static readonly (int Min, int Max) EliteGold = (28, 40);
    private static readonly (int Min, int Max) TreasureGold = (20, 30);

    private readonly ActMap _map;
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
    private string? _outcome;
    private TreasureState? _treasure;
    private EndView? _end;

    private Run(ulong seed, RunSetup setup)
    {
        Seed = seed;
        _map = setup.Map ?? MapGenerator.Generate(new SeededRng(seed));
        // Een aparte stroom voor loot, zodat de map niet verandert als er een beloning bijkomt
        _loot = new SeededRng(Mix(seed, 0x10075));
        _deck = [.. setup.Deck ?? CardCatalog.StarterDeck()];
        _relics = [.. setup.Relics ?? []];
        Hp = setup.Hp;
        MaxHp = setup.Hp;
        Gold = setup.Gold;
    }

    public ulong Seed { get; }
    public RunPhase Phase { get; private set; } = RunPhase.Map;
    public int Hp { get; private set; }
    public int MaxHp { get; private set; }
    public int Gold { get; private set; }
    public IReadOnlyList<CardDefinition> Deck => _deck;
    public IReadOnlyList<string> Relics => _relics;
    public ActMap Map => _map;

    public static Run Start(ulong seed, RunSetup? setup = null) => new(seed, setup ?? new RunSetup());

    /// <summary>Het lopende gevecht, voor de stage. Leeg buiten een gevecht.</summary>
    public CombatSnapshot? CombatSnapshot() => Phase == RunPhase.Combat ? _combat?.Snapshot() : null;

    /// <summary>Enige manier om de status te wijzigen.</summary>
    public IReadOnlyList<GameEvent> Handle(ICommand command)
    {
        _events = [];

        switch (Phase, command)
        {
            case (RunPhase.Combat, PlayCard or EndTurn): HandleCombat(command); break;
            case (RunPhase.Map, ChooseNode c): Enter(c.NodeId); break;
            case (RunPhase.Reward, TakeRewardCard t): TakeReward(t.Index); break;
            case (RunPhase.Reward, SkipReward): BackToMap(); break;
            case (RunPhase.Rest, RestHeal): RestAndHeal(); break;
            case (RunPhase.Rest, RestUpgrade u): RestAndUpgrade(u.DeckIndex); break;
            case (RunPhase.Event, ChooseEventOption o): ChooseOption(o.Option, o.DeckIndex); break;
            case (RunPhase.Shop, BuyCard b): Buy(b.Index); break;
            case (RunPhase.Shop, BuyRelic): BuyShopRelic(); break;
            case (RunPhase.Shop, BuyRemoval r): BuyCardRemoval(r.DeckIndex); break;
            case (RunPhase.Treasure, OpenChest): Open(); break;
            case (RunPhase.Shop or RunPhase.Treasure or RunPhase.Rest or RunPhase.Event, Leave): LeaveNode(); break;
            default: Reject("Dat kan nu niet."); break;
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
            Phase == RunPhase.Treasure && _treasure is { } t ? new TreasureView(t.Opened, t.Opened && t.Relic is { } r ? RelicInfo.From(r) : null, t.Opened ? t.Gold : 0) : null,
            _end);
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
            Reject("Daar loopt geen pad naartoe.");
            return;
        }

        _node = node;
        _visited.Add(node.Id);
        _outcome = null;
        Emit(new NodeEntered(node.Id, node.Kind));

        switch (node.Kind)
        {
            case NodeKind.Fight or NodeKind.Elite or NodeKind.Boss:
                StartCombat(node);
                break;
            case NodeKind.Rest:
                Phase = RunPhase.Rest;
                break;
            case NodeKind.Event:
                Phase = RunPhase.Event;
                break;
            case NodeKind.Shop:
                _shop = StockShop();
                Phase = RunPhase.Shop;
                break;
            case NodeKind.Treasure:
                _treasure = new TreasureState(RandomUnownedRelic(), Roll(TreasureGold));
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
        Phase = RunPhase.Map;
    }

    private void LeaveNode()
    {
        if (Phase == RunPhase.Treasure && _treasure is { Opened: false })
        {
            Reject("De kist is nog dicht.");
            return;
        }
        if (Phase is RunPhase.Rest or RunPhase.Event && _outcome is null)
        {
            Reject("Kies eerst.");
            return;
        }
        BackToMap();
    }

    // ---------- Gevecht ----------

    private void StartCombat(MapNode node)
    {
        string enemy = node.Encounter ?? throw new InvalidOperationException($"Knoop {node.Id} heeft geen vijand.");
        double block = _relics.Contains(RelicCatalog.Ankervat) ? RelicCatalog.AnkervatBlock : 0;
        var setup = new CombatSetup(
            Scenarios.Player(Hp, MaxHp, block),
            Bestiary.Create(enemy),
            [.. _deck],
            Relics: [.. _relics]);

        _combat = Combat.Combat.Start(setup, Mix(Seed, (ulong)node.Id + 1));
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

        if (combat.Outcome == CombatOutcome.Lost)
        {
            End(won: false, enemy);
            return;
        }

        if (_node!.Kind == NodeKind.Boss)
        {
            End(won: true, enemy);
            return;
        }

        bool elite = _node.Kind == NodeKind.Elite;
        int gold = Roll(elite ? EliteGold : FightGold);
        GainGold(gold);

        string? relic = elite ? RandomUnownedRelic() : null;
        if (relic is not null) GainRelic(relic);

        _rewardCards = RollCards(3, elite);
        _reward = new RewardView(gold, relic is null ? null : RelicInfo.From(relic), [.. _rewardCards.Select(CardInfo.From)]);
        Phase = RunPhase.Reward;
    }

    private void End(bool won, CombatantView enemy)
    {
        _combat = null;
        Phase = won ? RunPhase.Won : RunPhase.Lost;
        _end = new EndView(won, _node!.Row + 1, enemy.Name, enemy.Hp, enemy.MaxHp, Gold, _deck.Count, _relics.Count);
        Emit(new RunEnded(won));
    }

    private void TakeReward(int index)
    {
        if (index < 0 || index >= _rewardCards.Count)
        {
            Reject("Die kaart ligt er niet.");
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
        if (_outcome is not null) { Reject("Je hebt al gerust."); return; }
        int healed = ChangeHp(RestHealAmount);
        _outcome = $"Je rust bij het vuur en herstelt {healed} HP.";
    }

    private void RestAndUpgrade(int deckIndex)
    {
        if (_outcome is not null) { Reject("Je hebt al gerust."); return; }
        if (!InDeck(deckIndex) || CardCatalog.Upgrade(_deck[deckIndex]) is not { } better)
        {
            Reject("Die kaart kan niet beter.");
            return;
        }
        string before = _deck[deckIndex].Name;
        Transform(deckIndex, better);
        _outcome = $"Je smeedt {before} om tot {better.Name}.";
    }

    // ---------- Events ----------

    private EventView EventView()
    {
        string key = _node!.Encounter!;
        bool chosen = _outcome is not null;
        IReadOnlyList<EventOptionView> options = chosen ? [] : EventOptions(key);

        return key switch
        {
            Adventures.Smeltkroes => new(key, "De Smeltkroes",
                "Boven een vuur borrelt een kroes vol gesmolten metaal. Wat erin gaat, komt er in een andere vorm uit.",
                options, _outcome),
            Adventures.LekkendVat => new(key, "Het Lekkende Vat",
                "Uit een barst in een reusachtig vat druppelt goud. Het vat kraakt gevaarlijk.",
                options, _outcome),
            _ => throw new InvalidOperationException($"Onbekend event: {key}")
        };
    }

    private List<EventOptionView> EventOptions(string key)
    {
        int[] all = [.. Enumerable.Range(0, _deck.Count)];

        switch (key)
        {
            case Adventures.Smeltkroes:
            {
                int[] pourable = [.. all.Where(i => CardCatalog.CanPour(_deck[i]))];
                return
                [
                    Option("Giet een Vlottende kaart om", "Hij kost voortaan 0 energie, maar verliest zijn decimalen.",
                        pourable.Length > 0 ? null : "Je hebt geen Vlottende kaart.", pourable),
                    Option("Smelt een kaart weg", $"Verwijder een kaart uit je deck. Verlies {Adventures.SmeltHpCost} HP.",
                        Hp > Adventures.SmeltHpCost ? null : "Te weinig HP.", all),
                    Option("Vertrek", "Laat de kroes borrelen.", null, []),
                ];
            }
            case Adventures.LekkendVat:
                return
                [
                    Option("Vang het goud op", $"+{Adventures.VatGold} goud. Verlies {Adventures.VatHpCost} HP.",
                        Hp > Adventures.VatHpCost ? null : "Te weinig HP.", []),
                    Option("Vertrek", "Het vat kraakt nog na als je wegloopt.", null, []),
                ];
            default:
                return [];
        }

        static EventOptionView Option(string label, string detail, string? disabled, int[] cards) =>
            new(label, detail, disabled is null, disabled, cards);
    }

    private void ChooseOption(int option, int deckIndex)
    {
        if (_outcome is not null) { Reject("Je hebt al gekozen."); return; }

        var options = EventOptions(_node!.Encounter!);
        if (option < 0 || option >= options.Count || !options[option].Enabled)
        {
            Reject(option >= 0 && option < options.Count ? options[option].DisabledReason ?? "Dat kan niet." : "Die keuze bestaat niet.");
            return;
        }
        if (options[option].EligibleCards.Count > 0 && !options[option].EligibleCards.Contains(deckIndex))
        {
            Reject("Kies een kaart.");
            return;
        }

        switch (_node.Encounter, option)
        {
            case (Adventures.Smeltkroes, 0):
            {
                CardDefinition before = _deck[deckIndex];
                CardDefinition poured = CardCatalog.Pour(before);
                Transform(deckIndex, poured);
                _outcome = $"Je giet {before.Name} in de kroes. Er komt {poured.Name} uit: gratis, maar zonder decimalen.";
                break;
            }
            case (Adventures.Smeltkroes, 1):
            {
                string name = _deck[deckIndex].Name;
                RemoveCard(deckIndex);
                ChangeHp(-Adventures.SmeltHpCost);
                _outcome = $"De kroes slokt {name} op. Het vuur schroeit je hand.";
                break;
            }
            case (Adventures.LekkendVat, 0):
                GainGold(Adventures.VatGold);
                ChangeHp(-Adventures.VatHpCost);
                _outcome = "Je vangt het goud op. Dan barst het vat open en spoelt je omver.";
                break;
            default:
                _outcome = "Je loopt verder.";
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
            var pool = CardCatalog.RewardPool.Where(c => c.Rarity == rarity).ToList();
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
            Reject("Die kaart ligt er niet meer.");
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
            Reject("Er ligt geen relic meer.");
            return;
        }
        if (!Pay(offer.Price)) return;
        _shop.Relic = offer with { Sold = true };
        GainRelic(offer.Id);
    }

    private void BuyCardRemoval(int deckIndex)
    {
        if (_shop!.RemovalUsed) { Reject("De smid verwijdert maar één kaart per bezoek."); return; }
        if (!InDeck(deckIndex)) { Reject("Die kaart zit niet in je deck."); return; }
        if (!Pay(RemovalPrice)) return;
        _shop.RemovalUsed = true;
        RemoveCard(deckIndex);
    }

    private bool Pay(int price)
    {
        if (price > Gold)
        {
            Reject("Te weinig goud.");
            return false;
        }
        GainGold(-price);
        return true;
    }

    // ---------- Schat ----------

    private sealed class TreasureState(string? relic, int gold)
    {
        public string? Relic { get; } = relic;
        public int Gold { get; } = gold;
        public bool Opened { get; set; }
    }

    private void Open()
    {
        var t = _treasure!;
        if (t.Opened) { Reject("De kist is al open."); return; }
        t.Opened = true;
        GainGold(t.Gold);
        if (t.Relic is not null) GainRelic(t.Relic);
    }

    // ---------- Loot ----------

    /// <summary>Kaarten zonder dubbels. Een elite geeft meer kans op zeldzaam.</summary>
    private List<CardDefinition> RollCards(int count, bool elite)
    {
        var picked = new List<CardDefinition>();
        for (int attempt = 0; picked.Count < count && attempt < 50; attempt++)
        {
            int roll = _loot.NextInt(100);
            Rarity rarity = elite
                ? roll < 40 ? Rarity.Common : roll < 85 ? Rarity.Uncommon : Rarity.Rare
                : roll < 60 ? Rarity.Common : roll < 93 ? Rarity.Uncommon : Rarity.Rare;

            var pool = CardCatalog.RewardPool.Where(c => c.Rarity == rarity && !picked.Contains(c)).ToList();
            if (pool.Count == 0) continue;
            picked.Add(pool[_loot.NextInt(pool.Count)]);
        }
        return picked;
    }

    private string? RandomUnownedRelic()
    {
        var pool = RelicCatalog.All.Select(r => r.Id).Where(id => !_relics.Contains(id)).ToList();
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

        if (id == RelicCatalog.GrotePot)
        {
            MaxHp += RelicCatalog.GrotePotMaxHp;
            ChangeHp(RelicCatalog.GrotePotMaxHp);
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
