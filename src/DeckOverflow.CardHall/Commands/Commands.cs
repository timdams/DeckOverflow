using System.Text.Json.Serialization;

namespace DeckOverflow.CardHall.Commands;

/// <summary>
/// Wat de speler doet. Serialiseerbaar zoals <see cref="Events.GameEvent"/>, zodat een run
/// bestaat uit een seed en een commandolijst die je opnieuw kan afspelen (scores controleren).
/// Een nieuw command komt ook hier in de lijst.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(PlayCard), nameof(PlayCard))]
[JsonDerivedType(typeof(EndTurn), nameof(EndTurn))]
[JsonDerivedType(typeof(ScrapModifiers), nameof(ScrapModifiers))]
[JsonDerivedType(typeof(DebugWin), nameof(DebugWin))]
[JsonDerivedType(typeof(ChooseNode), nameof(ChooseNode))]
[JsonDerivedType(typeof(TakeRewardCard), nameof(TakeRewardCard))]
[JsonDerivedType(typeof(SkipReward), nameof(SkipReward))]
[JsonDerivedType(typeof(ChooseRelic), nameof(ChooseRelic))]
[JsonDerivedType(typeof(RestHeal), nameof(RestHeal))]
[JsonDerivedType(typeof(RestUpgrade), nameof(RestUpgrade))]
[JsonDerivedType(typeof(ChooseEventOption), nameof(ChooseEventOption))]
[JsonDerivedType(typeof(BuyCard), nameof(BuyCard))]
[JsonDerivedType(typeof(BuyRelic), nameof(BuyRelic))]
[JsonDerivedType(typeof(BuyRemoval), nameof(BuyRemoval))]
[JsonDerivedType(typeof(OpenChest), nameof(OpenChest))]
[JsonDerivedType(typeof(Leave), nameof(Leave))]
public interface ICommand;

// Gevecht
public sealed record PlayCard(int HandIndex, int TargetId) : ICommand;
public sealed record EndTurn : ICommand;

/// <summary>Veeg alle wachtende modifiers weg, tegen <see cref="Combat.Combat.ScrapCost"/> energie. Zo zit je nooit vast met tekst die niets kan raken.</summary>
public sealed record ScrapModifiers : ICommand;

/// <summary>TIJDELIJK, om snel te testen: win het lopende gevecht meteen (sneltoets W). Weg voor een playtest.</summary>
public sealed record DebugWin : ICommand;

// Map
public sealed record ChooseNode(int NodeId) : ICommand;

// Beloning na een gevecht, en kiezen bij een start in een latere act
public sealed record TakeRewardCard(int Index) : ICommand;
public sealed record SkipReward : ICommand;

// Een relic kiezen: na de baas van een act, of bij een start in een latere act
public sealed record ChooseRelic(int Index) : ICommand;

// Rustvuur
public sealed record RestHeal : ICommand;
public sealed record RestUpgrade(int DeckIndex) : ICommand;

// Event: sommige keuzes vragen een kaart uit je deck
public sealed record ChooseEventOption(int Option, int DeckIndex = -1) : ICommand;

// Winkel
public sealed record BuyCard(int Index) : ICommand;
public sealed record BuyRelic : ICommand;
public sealed record BuyRemoval(int DeckIndex) : ICommand;

// Schat
public sealed record OpenChest : ICommand;

/// <summary>Verder naar de map vanuit de winkel of een geopende schat.</summary>
public sealed record Leave : ICommand;
