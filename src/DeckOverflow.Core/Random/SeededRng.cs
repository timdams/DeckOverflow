namespace DeckOverflow.Core.Random;

/// <summary>
/// PCG32 (O'Neill). Eigen implementatie zodat een seed over .NET-versies heen
/// exact hetzelfde gevecht oplevert, wat System.Random niet garandeert.
/// </summary>
public sealed class SeededRng
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong Increment = 1442695040888963407UL;
    private ulong _state;

    public SeededRng(ulong seed)
    {
        _state = 0;
        NextUInt();
        _state = unchecked(_state + seed);
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong old = _state;
        _state = unchecked(old * Multiplier + Increment);
        uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
        int rot = (int)(old >> 59);
        return (xorShifted >> rot) | (xorShifted << (-rot & 31));
    }

    /// <summary>Uniform getal in [0, maxExclusive), zonder modulo-bias.</summary>
    public int NextInt(int maxExclusive)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
        uint bound = (uint)maxExclusive;
        uint threshold = unchecked((uint)-bound) % bound;
        while (true)
        {
            uint r = NextUInt();
            if (r >= threshold) return (int)(r % bound);
        }
    }

    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = NextInt(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
