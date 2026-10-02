using System.Text.Json.Serialization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Relics;

namespace DeckOverflow.Engine.Runs;

[JsonConverter(typeof(JsonStringEnumConverter<RunPhase>))]
public enum RunPhase { Map, Combat, Reward, Rest, Event, Shop, Treasure, Won, Lost }

/// <summary>Alleen-lezen beeld van de hele run voor de shell. Het gevecht zelf zit in <see cref="Combat.CombatSnapshot"/>.</summary>
/// <param name="Floor">Hoeveel rijen je al geklommen bent, de huidige inbegrepen.</param>
public sealed record RunSnapshot(
    ulong Seed,
    RunPhase Phase,
    int Hp,
    int MaxHp,
    int Gold,
    int Floor,
    IReadOnlyList<CardInfo> Deck,
    IReadOnlyList<RelicInfo> Relics,
    MapView Map,
    RewardView? Reward,
    RestView? Rest,
    EventView? Event,
    ShopView? Shop,
    TreasureView? Treasure,
    EndView? End);

public sealed record RelicInfo(string Id, string Name, string Text)
{
    public static RelicInfo From(string id)
    {
        RelicDefinition r = RelicCatalog.Get(id);
        return new(r.Id, r.Name, r.Text);
    }
}

public sealed record MapView(int Rows, int Columns, IReadOnlyList<MapNodeView> Nodes, IReadOnlyList<MapEdge> Edges);

/// <param name="Reachable">Hier kan je nu naartoe.</param>
public sealed record MapNodeView(int Id, int Row, int Column, NodeKind Kind, bool Visited, bool Current, bool Reachable);

/// <summary>Goud en een eventuele relic zijn al binnen; de kaart kies je nog.</summary>
public sealed record RewardView(int Gold, RelicInfo? Relic, IReadOnlyList<CardInfo> Cards);

/// <param name="Outcome">Wat er gebeurde, zodra je gekozen hebt.</param>
public sealed record RestView(int HealAmount, IReadOnlyList<int> UpgradableCards, string? Outcome);

public sealed record EventView(string Key, string Title, string Text, IReadOnlyList<EventOptionView> Options, string? Outcome);

/// <param name="EligibleCards">Indexen in je deck waaruit je moet kiezen. Leeg als de keuze geen kaart vraagt.</param>
public sealed record EventOptionView(string Label, string Detail, bool Enabled, string? DisabledReason, IReadOnlyList<int> EligibleCards);

public sealed record ShopView(IReadOnlyList<ShopCardView> Cards, ShopRelicView? Relic, int RemovalPrice, bool RemovalUsed);
public sealed record ShopCardView(CardInfo Card, int Price, bool Sold);
public sealed record ShopRelicView(RelicInfo Relic, int Price, bool Sold);

/// <param name="Relic">Leeg tot de kist open is, of als er geen relics meer over zijn.</param>
public sealed record TreasureView(bool Opened, RelicInfo? Relic, int Gold);

/// <summary>Het eindscherm. Bij verlies: hoe dicht je erbij was.</summary>
public sealed record EndView(bool Won, int Floor, string? EnemyName, double EnemyHp, double EnemyMaxHp, int Gold, int DeckSize, int RelicCount);
