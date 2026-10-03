namespace DeckOverflow.Core.Values;

/// <summary>Gewoon .NET-gedrag van <c>byte</c>, geen nabootsing.</summary>
public static class ByteRules
{
    public static (byte Result, bool Overflowed) Add(byte current, int amount)
    {
        byte result = unchecked((byte)(current + amount));
        return (result, current + amount > byte.MaxValue);
    }
}
