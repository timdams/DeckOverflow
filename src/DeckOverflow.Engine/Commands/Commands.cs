namespace DeckOverflow.Engine.Commands;

public interface ICommand;

// Gevecht
public sealed record PlayCard(int HandIndex, int TargetId) : ICommand;
public sealed record EndTurn : ICommand;

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
