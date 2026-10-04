using System.Globalization;
using DeckOverflow.Core.Values;

namespace DeckOverflow.ConveyorBelt.Belts;

/// <summary>Het C#-type van wat in een kist zit.</summary>
public enum Kind { Int, Byte, Double, String }

/// <summary>
/// De inhoud van een kist: een waarde met een type. Rekenen gebeurt met echt .NET-gedrag, geen nabootsing:
/// een <c>byte</c> loopt over, een deling van gehele getallen kapt af, tekst plakt.
/// </summary>
public readonly record struct Value(Kind Kind, double Number, string Text = "")
{
    public static Value Int(int value) => new(Kind.Int, value);
    public static Value Byte(byte value) => new(Kind.Byte, value);
    public static Value Double(double value) => new(Kind.Double, value);
    public static Value String(string value) => new(Kind.String, 0, value);

    /// <summary>Zoals C# het zou tonen: <c>12</c>, <c>3.5</c>, <c>"abc"</c>. Getallen altijd in codenotatie.</summary>
    public override string ToString() => Kind switch
    {
        Kind.String => $"\"{Text}\"",
        Kind.Double => Number.ToString(CultureInfo.InvariantCulture),
        _ => ((long)Number).ToString(CultureInfo.InvariantCulture),
    };
}

/// <summary>Wat er bijzonders gebeurde bij een bewerking: de haak voor de stage en de uitleg.</summary>
public enum Note { None, Overflow, Truncated }

public enum OpKind { Add, Subtract, Multiply, Divide, Append }

/// <summary>Wat een machine met de inhoud van een kist doet.</summary>
/// <param name="Operand">Het getal waarmee gerekend wordt.</param>
/// <param name="Text">De tekst die <see cref="OpKind.Append"/> erachter plakt.</param>
public sealed record Op(OpKind Kind, int Operand = 0, string Text = "")
{
    public (Value Result, Note Note) Apply(Value v) => v.Kind switch
    {
        Belts.Kind.Int => OnInt((int)v.Number),
        Belts.Kind.Byte => OnByte((byte)v.Number),
        Belts.Kind.Double => (Value.Double(OnDouble(v.Number)), Note.None),
        Belts.Kind.String => (Value.String(OnString(v.Text)), Note.None),
        _ => throw new ArgumentOutOfRangeException(nameof(v)),
    };

    private (Value, Note) OnInt(int x)
    {
        int result = Kind switch
        {
            OpKind.Add => unchecked(x + Operand),
            OpKind.Subtract => unchecked(x - Operand),
            OpKind.Multiply => unchecked(x * Operand),
            OpKind.Divide => x / Operand,
            _ => x,
        };
        bool truncated = Kind == OpKind.Divide && x % Operand != 0;
        return (Value.Int(result), truncated ? Note.Truncated : Note.None);
    }

    private (Value, Note) OnByte(byte b)
    {
        // Optellen gaat langs ByteRules uit Core, zoals in de Card Hall
        if (Kind == OpKind.Add)
        {
            (byte sum, bool overflowed) = ByteRules.Add(b, Operand);
            return (Value.Byte(sum), overflowed ? Note.Overflow : Note.None);
        }
        // Rekenen met een byte gebeurt in int; terug in een byte loopt het over, zoals unchecked((byte)...)
        int wide = Kind switch
        {
            OpKind.Add => b + Operand,
            OpKind.Subtract => b - Operand,
            OpKind.Multiply => b * Operand,
            OpKind.Divide => b / Operand,
            _ => b,
        };
        byte result = unchecked((byte)wide);
        Note note = wide != result ? Note.Overflow : Kind == OpKind.Divide && b % Operand != 0 ? Note.Truncated : Note.None;
        return (Value.Byte(result), note);
    }

    private double OnDouble(double d) => Kind switch
    {
        OpKind.Add => d + Operand,
        OpKind.Subtract => d - Operand,
        OpKind.Multiply => d * Operand,
        OpKind.Divide => d / Operand,
        _ => d,
    };

    /// <summary>Met tekst plakt <c>+</c>, ook een getal: <c>"ab" + 4</c> is <c>"ab4"</c>.</summary>
    private string OnString(string s) => Kind switch
    {
        OpKind.Append => s + Text,
        OpKind.Add => s + Operand.ToString(CultureInfo.InvariantCulture),
        _ => s,
    };

    /// <summary>De bewerking als C#: <c>v += 4</c>, <c>v /= 2</c>, <c>s += "-"</c>.</summary>
    public string CSharp() => Kind switch
    {
        OpKind.Add => $"v += {Operand}",
        OpKind.Subtract => $"v -= {Operand}",
        OpKind.Multiply => $"v *= {Operand}",
        OpKind.Divide => $"v /= {Operand}",
        OpKind.Append => $"v += \"{Text}\"",
        _ => "?",
    };
}

public enum CondKind { Less, Greater, Equal, ModZero, LengthLess }

/// <summary>Waar een poort naar kijkt.</summary>
public sealed record Condition(CondKind Kind, int N)
{
    public bool Holds(Value v) => Kind switch
    {
        CondKind.Less => v.Number < N,
        CondKind.Greater => v.Number > N,
        CondKind.Equal => v.Number == N,
        CondKind.ModZero => N != 0 && (long)v.Number % N == 0,
        CondKind.LengthLess => v.Text.Length < N,
        _ => false,
    };

    public string CSharp() => Kind switch
    {
        CondKind.Less => $"v < {N}",
        CondKind.Greater => $"v > {N}",
        CondKind.Equal => $"v == {N}",
        CondKind.ModZero => $"v % {N} == 0",
        CondKind.LengthLess => $"v.Length < {N}",
        _ => "?",
    };
}
