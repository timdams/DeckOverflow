using DeckOverflow.Engine.Values;

namespace DeckOverflow.Tests;

public class ValueRulesTests
{
    [Theory]
    [InlineData(250, 5, 255, false)]
    [InlineData(250, 6, 0, true)]
    [InlineData(252, 6, 2, true)]
    [InlineData(255, 2, 1, true)]
    public void ByteAdd_volgt_dotnet(int current, int amount, int expected, bool overflowed)
    {
        var (result, didOverflow) = ByteRules.Add((byte)current, amount);

        Assert.Equal(expected, result);
        Assert.Equal(overflowed, didOverflow);
    }

    [Theory]
    [InlineData(2.5, 2, 0.5)]
    [InlineData(7.0, 7, 0.0)]
    [InlineData(0.5, 0, 0.5)]
    public void Truncate_kapt_af_naar_nul(double incoming, int expected, double lost)
    {
        var (result, actualLost) = IntRules.Truncate(incoming);

        Assert.Equal(expected, result);
        Assert.Equal(lost, actualLost, precision: 10);
    }
}
