namespace DeckOverflow.Engine.Values;

/// <summary>Gewoon .NET-gedrag bij de conversie van <c>double</c> naar een geheel type.</summary>
public static class IntRules
{
    public static (int Result, double Lost) Truncate(double incoming)
    {
        int result = (int)incoming;
        return (result, incoming - result);
    }
}
