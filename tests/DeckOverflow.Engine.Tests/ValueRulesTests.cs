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

    [Theory]
    [InlineData(24.5, 24, 0.5)]
    [InlineData(506.0, 506, 0.0)]
    public void Naar_int_kapt_af(double value, double expected, double lost)
    {
        var cast = CastRules.Convert(value, ValueKind.Int);

        Assert.Equal(expected, cast.Result);
        Assert.Equal(lost, cast.Lost, precision: 10);
        Assert.False(cast.Wrapped);
    }

    [Theory]
    [InlineData(506.0, 250, true)]
    [InlineData(300.0, 44, true)]
    [InlineData(512.0, 0, true)]
    [InlineData(250.0, 250, false)]
    [InlineData(260.7, 4, true)]
    public void Naar_byte_klapt_om_zoals_unchecked_cast(double value, double expected, bool wrapped)
    {
        var cast = CastRules.Convert(value, ValueKind.Byte);

        // Dezelfde uitkomst als C# zelf, via int
        Assert.Equal(unchecked((byte)(int)value), cast.Result);
        Assert.Equal(expected, cast.Result);
        Assert.Equal(wrapped, cast.Wrapped);
    }

    [Fact]
    public void Max_hangt_af_van_het_type()
    {
        Assert.Equal(255, CastRules.MaxFor(ValueKind.Byte, 506));
        Assert.Equal(24, CastRules.MaxFor(ValueKind.Int, 24.5));
        Assert.Equal(24.5, CastRules.MaxFor(ValueKind.Double, 24.5));
    }
}
