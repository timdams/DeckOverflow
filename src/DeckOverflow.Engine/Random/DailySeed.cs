using System.Text;

namespace DeckOverflow.Engine.Random;

/// <summary>
/// De seed van de dag: iedereen speelt dezelfde run op dezelfde UTC-datum. Afgeleid uit de datum
/// met een vaste hash (FNV-1a, 64 bit), dus zonder tabel of server. De motor kent geen klok: de
/// shell geeft de datum mee.
/// </summary>
public static class DailySeed
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;

    public static ulong For(DateOnly utcDate)
    {
        ulong hash = OffsetBasis;
        foreach (byte b in Encoding.ASCII.GetBytes(utcDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)))
        {
            hash = unchecked((hash ^ b) * Prime);
        }
        return hash;
    }
}
