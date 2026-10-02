namespace DeckOverflow.Engine.Commands;

public interface ICommand;
public sealed record PlayCard(int HandIndex, int TargetId) : ICommand;
public sealed record EndTurn : ICommand;
