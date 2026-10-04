namespace DeckOverflow.Engine.Belts;

public enum Dir { Up, Right, Down, Left }

/// <summary>Een vakje op het rooster.</summary>
public readonly record struct Cell(int X, int Y)
{
    public Cell Step(Dir dir) => dir switch
    {
        Dir.Up => new(X, Y - 1),
        Dir.Right => new(X + 1, Y),
        Dir.Down => new(X, Y + 1),
        Dir.Left => new(X - 1, Y),
        _ => this,
    };

    public override string ToString() => $"{X},{Y}";
}

/// <summary>Wat op een vakje ligt. Een kist die erop komt, gaat er in één richting weer af.</summary>
public abstract record Piece
{
    /// <summary>Telt mee als machine in de score: alles behalve band.</summary>
    public virtual bool IsMachine => true;
}

/// <summary>Gewone band: de kist rijdt verder in deze richting.</summary>
public sealed record Belt(Dir Out) : Piece
{
    public override bool IsMachine => false;
}

/// <summary>Een machine die de inhoud van de kist bewerkt en hem dan verder stuurt.</summary>
public sealed record Machine(Op Op, Dir Out) : Piece;

/// <summary>
/// Een poort met een voorwaarde: klopt ze, dan gaat de kist naar <see cref="IfTrue"/>, anders naar <see cref="IfFalse"/>.
/// Een band die terugloopt naar de poort, maakt er een <c>while</c> van; staat de poort achter het werk, een <c>do while</c>.
/// </summary>
public sealed record Gate(Condition When, Dir IfTrue, Dir IfFalse) : Piece;

/// <summary>
/// Een teller: de eerste <see cref="Times"/> keer stuurt hij de kist de lus in, daarna naar buiten, en dan begint
/// hij opnieuw bij 0. Dat is een <c>for</c>: <c>for (int i = 0; i &lt; Times; i++)</c>.
/// </summary>
public sealed record Counter(int Times, Dir Loop, Dir Done) : Piece;

/// <summary>Waar de kisten vandaan komen. Ligt vast.</summary>
public sealed record Source(Dir Out) : Piece
{
    public override bool IsMachine => false;
}

/// <summary>Waar de kisten naartoe moeten. Ligt vast.</summary>
public sealed record Output : Piece
{
    public override bool IsMachine => false;
}
