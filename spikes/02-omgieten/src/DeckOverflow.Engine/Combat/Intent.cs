namespace DeckOverflow.Engine.Combat;

/// <summary>
/// Wat een vijand van plan is. <see cref="Value"/> wordt door C# zelf uitgerekend
/// op de plek waar de intent gedefinieerd wordt, niet door een eigen parser.
/// </summary>
public sealed record Intent(string Expression, double Value);
