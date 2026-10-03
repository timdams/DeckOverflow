using System.Globalization;

namespace DeckOverflow.Core.Values;

/// <summary>
/// Een waarde met haar C#-type, voor de getypeerde aanval. Elke operator volgt echte C#:
/// <c>7 / 2</c> is 3, <c>7.0 / 2</c> is 3.5, <c>7 + "1"</c> is <c>"71"</c>.
/// Wat in C# niet compileert, gooit <see cref="CompileError"/>; wat in C# crasht,
/// gooit de echte .NET-exception (<see cref="FormatException"/>, <see cref="OverflowException"/>).
/// </summary>
public readonly record struct TypedValue(ValueKind Kind, double Number, string Text)
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static TypedValue Int(int value) => new(ValueKind.Int, value, "");
    public static TypedValue Double(double value) => new(ValueKind.Double, value, "");
    public static TypedValue String(string value) => new(ValueKind.String, 0, value);
    public static TypedValue Char(char value) => new(ValueKind.Char, value, "");

    /// <summary>Het getal op een kaart: een Vlottende kaart vuurt een double af, de rest een int.</summary>
    public static TypedValue OfCard(double amount, ValueKind? kind) =>
        kind == ValueKind.Double || amount % 1 != 0 ? Double(amount) : Int((int)amount);

    public bool IsText => Kind == ValueKind.String;

    /// <summary>Zoals je het in C# zou schrijven: <c>7</c>, <c>7.0</c>, <c>2.5</c>, <c>"71"</c>.</summary>
    public string Literal => Kind switch
    {
        ValueKind.String => $"\"{Text}\"",
        ValueKind.Char => $"'{(char)Number}'",
        ValueKind.Double => Number % 1 == 0 ? Number.ToString("0.0", Inv) : Number.ToString(Inv),
        _ => ((int)Number).ToString(Inv)
    };

    /// <summary>Wat concatenatie ervan maakt. C# schrijft 7.0 als "7".</summary>
    private string Concat => Kind switch
    {
        ValueKind.String => Text,
        ValueKind.Char => ((char)Number).ToString(),
        ValueKind.Double => Number.ToString(Inv),
        _ => ((int)Number).ToString(Inv)
    };

    /// <summary>Een <c>char</c> rekent mee als geheel getal: <c>'A' + 1</c> is 66, een <c>int</c>.</summary>
    private bool BothInt(TypedValue other) => IsWholeNumber && other.IsWholeNumber;

    private bool IsWholeNumber => Kind is ValueKind.Int or ValueKind.Char;

    /// <summary><c>+</c>: tekst plakt, twee ints blijven een int (en lopen unchecked over), anders een double.</summary>
    public TypedValue Plus(TypedValue other)
    {
        if (IsText || other.IsText) return String(Concat + other.Concat);
        if (BothInt(other)) return Int(unchecked((int)Number + (int)other.Number));
        return Double(Number + other.Number);
    }

    public TypedValue Times(TypedValue other)
    {
        if (IsText || other.IsText) throw new CompileError("operator * on string");
        if (BothInt(other)) return Int(unchecked((int)Number * (int)other.Number));
        return Double(Number * other.Number);
    }

    /// <summary><c>/</c>: twee ints geven een deling van gehele getallen, naar nul afgekapt.</summary>
    public TypedValue DividedBy(TypedValue other)
    {
        if (IsText || other.IsText) throw new CompileError("operator / on string");
        if (BothInt(other)) return Int((int)Number / (int)other.Number);
        return Double(Number / other.Number);
    }

    /// <summary><c>int.Parse</c>: alleen op tekst, en crasht op tekst die geen geheel getal is.</summary>
    public TypedValue Parse()
    {
        if (!IsText) throw new CompileError("int.Parse needs a string");
        return Int(int.Parse(Text, NumberStyles.Integer, Inv));
    }

    /// <summary><c>int.TryParse</c>: geen exception, maar 0 als de tekst geen <c>int</c> is.</summary>
    public TypedValue TryParse()
    {
        if (!IsText) throw new CompileError("int.TryParse needs a string");
        return Int(int.TryParse(Text, NumberStyles.Integer, Inv, out int n) ? n : 0);
    }
}

/// <summary>Code die in C# niet zou compileren. In het spel weigert de kaart dan, en kost ze niets.</summary>
public sealed class CompileError(string message) : Exception(message);
