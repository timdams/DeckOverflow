namespace DeckOverflow.Engine.Values;

/// <summary>
/// Omgieten volgt de expliciete conversies van C#. Van <c>double</c> naar <c>byte</c>
/// loopt via <c>int</c>, omdat een rechtstreekse conversie van een te grote <c>double</c>
/// naar een geheel type geen vastgelegd resultaat heeft.
/// </summary>
public static class CastRules
{
    /// <param name="Lost">Wat na de komma verdween.</param>
    /// <param name="Wrapped">Of de waarde omklapte omdat ze niet in het nieuwe type paste.</param>
    public readonly record struct CastResult(double Result, double Lost, bool Wrapped);

    public static CastResult Convert(double value, ValueKind to)
    {
        switch (to)
        {
            case ValueKind.Double:
                return new(value, 0, false);

            case ValueKind.Int:
            {
                (int result, double lost) = IntRules.Truncate(value);
                return new(result, lost, false);
            }

            case ValueKind.Byte:
            {
                (int whole, double lost) = IntRules.Truncate(value);
                byte result = unchecked((byte)whole);
                return new(result, lost, result != whole);
            }

            default:
                throw new NotSupportedException($"Omgieten naar {to} bestaat nog niet.");
        }
    }

    /// <summary>
    /// <c>Convert.ToInt32</c> en <c>Convert.ToByte</c>: afronden naar het dichtste even getal bij .5,
    /// en een <c>OverflowException</c> als het resultaat niet past. Leeg betekent: de conversie crasht.
    /// </summary>
    public static double? ConvertChecked(double value, ValueKind to)
    {
        try
        {
            return to switch
            {
                ValueKind.Double => value,
                ValueKind.Int => System.Convert.ToInt32(value),
                ValueKind.Byte => System.Convert.ToByte(value),
                _ => throw new NotSupportedException($"Omzetten naar {to} bestaat nog niet.")
            };
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    /// <summary>Het grootste getal dat een type kan dragen, begrensd door de basiswaarde.</summary>
    public static double MaxFor(ValueKind kind, double baseMax) => kind switch
    {
        ValueKind.Byte => byte.MaxValue,
        ValueKind.Int => (int)baseMax,
        _ => baseMax
    };

    public static bool IsWhole(ValueKind kind) => kind is ValueKind.Int or ValueKind.Byte;
}
