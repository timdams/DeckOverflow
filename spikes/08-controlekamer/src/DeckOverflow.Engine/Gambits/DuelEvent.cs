namespace DeckOverflow.Engine.Gambits;

public enum Side { Player, Enemy }

public enum Outcome { PlayerWon, EnemyWon, ShiftOver }

/// <summary>Wat er in een duel gebeurt. Klein en plat, zoals de events van de deckbuilder.</summary>
public abstract record DuelEvent;

public sealed record TurnStarted(int Turn) : DuelEvent;
/// <summary>De regel met deze index klopte als eerste. De regels erboven werden bekeken, die eronder niet.</summary>
public sealed record RuleFired(Side Side, int RuleIndex, Move Move) : DuelEvent;
/// <summary>Geen enkele regel klopte: de automaat doet niets.</summary>
public sealed record NoRuleMatched(Side Side) : DuelEvent;
public sealed record BlockExpired(Side Side, int Amount) : DuelEvent;
public sealed record Whacked(Side Side, int Damage, int Absorbed, int HpAfter, bool WasCharged) : DuelEvent;
public sealed record Braced(Side Side, int Amount, int Total) : DuelEvent;
public sealed record Repaired(Side Side, int Amount, int HpAfter, int RepairsLeft) : DuelEvent;
/// <summary>De regel klopte, maar er zijn geen herstellingen meer: de beurt is weg.</summary>
public sealed record RepairEmpty(Side Side) : DuelEvent;
/// <summary><paramref name="WasCharged"/>: al opgeladen, dus deze beurt deed niets extra.</summary>
public sealed record WoundUp(Side Side, bool WasCharged) : DuelEvent;
public sealed record DuelEnded(Outcome Outcome, int Turn) : DuelEvent;
