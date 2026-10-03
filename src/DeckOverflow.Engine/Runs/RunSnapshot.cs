using System.Text.Json.Serialization;
using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Maps;
using DeckOverflow.Engine.Relics;
using DeckOverflow.Engine.Text;

namespace DeckOverflow.Engine.Runs;

[JsonConverter(typeof(JsonStringEnumConverter<RunPhase>))]
public enum RunPhase { Map, Combat, Reward, Rest, Event, Shop, Treasure, Draft, RelicChoice, Won, Lost }

/// <summary>Alleen-lezen beeld van de hele run voor de shell. Het gevecht zelf zit in <see cref="Combat.CombatSnapshot"/>.</summary>
/// <param name="Floor">Hoeveel rijen je in deze act al geklommen bent, de huidige inbegrepen.</param>
/// <param name="Act">Het nummer van de act; de naam staat in <c>en.json</c> onder <c>act.&lt;ActKey&gt;</c>.</param>
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
    EndView? End,
    int Act,
    string ActKey,
    DraftView? Draft,
    RelicChoiceView? RelicChoice,
    IReadOnlyList<string> Trinkets);

/// <summary>Een relic buiten het gevecht. De naam staat in <c>en.json</c> onder <c>relic.&lt;id&gt;</c>.</summary>
public sealed record RelicInfo(string Id, TextRef Text)
{
    public static RelicInfo From(string id) => new(id, RelicCatalog.Create(id).Text);
}

public sealed record MapView(int Rows, int Columns, IReadOnlyList<MapNodeView> Nodes, IReadOnlyList<MapEdge> Edges);

/// <param name="Reachable">Hier kan je nu naartoe.</param>
public sealed record MapNodeView(int Id, int Row, int Column, NodeKind Kind, bool Visited, bool Current, bool Reachable);

/// <summary>Goud en een eventuele relic zijn al binnen; de kaart kies je nog.</summary>
public sealed record RewardView(int Gold, RelicInfo? Relic, IReadOnlyList<CardInfo> Cards);

/// <param name="Outcome">Wat er gebeurde, zodra je gekozen hebt.</param>
public sealed record RestView(int HealAmount, IReadOnlyList<int> UpgradableCards, TextRef? Outcome);

/// <summary>Titel en verhaal staan in <c>en.json</c> onder <c>event.&lt;key&gt;.title</c> en <c>.text</c>.</summary>
public sealed record EventView(string Key, IReadOnlyList<EventOptionView> Options, TextRef? Outcome);

/// <param name="DisabledReason">Sleutel in <c>en.json</c> als de keuze niet kan.</param>
/// <param name="EligibleCards">Indexen in je deck waaruit je moet kiezen. Leeg als de keuze geen kaart vraagt.</param>
public sealed record EventOptionView(TextRef Label, TextRef Detail, bool Enabled, string? DisabledReason, IReadOnlyList<int> EligibleCards);

public sealed record ShopView(IReadOnlyList<ShopCardView> Cards, ShopRelicView? Relic, int RemovalPrice, bool RemovalUsed);
public sealed record ShopCardView(CardInfo Card, int Price, bool Sold);
public sealed record ShopRelicView(RelicInfo Relic, int Price, bool Sold);

/// <param name="Relic">Leeg tot de kist open is, of als er geen relics meer over zijn.</param>
/// <param name="Trinket">Een onderdeel dat nergens voor dient (<see cref="Trinkets"/>), of leeg.</param>
public sealed record TreasureView(bool Opened, RelicInfo? Relic, int Gold, string? Trinket = null);

/// <summary>Het eindscherm. Bij verlies: hoe dicht je erbij was.</summary>
public sealed record EndView(bool Won, int Floor, string? EnemyKey, double EnemyHp, double EnemyMaxHp, int Gold, int DeckSize, int RelicCount, int Act = 1, RunScore? Score = null);

/// <summary>Een start in een latere act: kies <paramref name="Rounds"/> keer 1 kaart uit 3.</summary>
public sealed record DraftView(int Round, int Rounds, IReadOnlyList<CardInfo> Cards);

/// <param name="Reason"><c>boss</c> na de baas van een act, <c>start</c> bij een start in een latere act.</param>
public sealed record RelicChoiceView(string Reason, IReadOnlyList<RelicInfo> Relics);
