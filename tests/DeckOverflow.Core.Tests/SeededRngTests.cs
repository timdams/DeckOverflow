using DeckOverflow.Core.Random;

namespace DeckOverflow.Tests;

public class SeededRngTests
{
    [Fact]
    public void Zelfde_seed_geeft_zelfde_reeks()
    {
        var a = new SeededRng(42);
        var b = new SeededRng(42);

        for (int i = 0; i < 1000; i++)
        {
            Assert.Equal(a.NextUInt(), b.NextUInt());
        }
    }

    [Fact]
    public void Andere_seed_geeft_andere_reeks()
    {
        var a = new SeededRng(1);
        var b = new SeededRng(2);

        uint[] fromA = [.. Enumerable.Range(0, 8).Select(_ => a.NextUInt())];
        uint[] fromB = [.. Enumerable.Range(0, 8).Select(_ => b.NextUInt())];

        Assert.NotEqual(fromA, fromB);
    }

    [Fact]
    public void Reeks_is_vastgepind()
    {
        // Pint het algoritme vast: faalt dit, dan veranderen alle bestaande seeds van gevecht.
        var rng = new SeededRng(255);

        uint[] actual = [rng.NextUInt(), rng.NextUInt(), rng.NextUInt()];

        Assert.Equal(PinnedSequence, actual);
    }

    [Fact]
    public void NextInt_blijft_binnen_grenzen()
    {
        var rng = new SeededRng(7);

        for (int i = 0; i < 10_000; i++)
        {
            int value = rng.NextInt(10);
            Assert.InRange(value, 0, 9);
        }
    }

    private static readonly uint[] PinnedSequence = [492379856u, 676516221u, 3304076896u];
}
