using DeckOverflow.Core.Values;

namespace DeckOverflow.Tests;

/// <summary>De regels voor een naam in C#, zoals de compiler ze toepast.</summary>
public class IdentifierRulesTests
{
    [Theory]
    [InlineData("shadow", IdentifierProblem.None)]
    [InlineData("_shadow", IdentifierProblem.None)]
    [InlineData("shadow2", IdentifierProblem.None)]
    [InlineData("@class", IdentifierProblem.None)]
    [InlineData("var", IdentifierProblem.None)]
    [InlineData("2shadow", IdentifierProblem.StartsWithDigit)]
    [InlineData("class", IdentifierProblem.Keyword)]
    [InlineData("null", IdentifierProblem.Keyword)]
    [InlineData("sha-dow", IdentifierProblem.BadCharacter)]
    [InlineData("sha dow", IdentifierProblem.BadCharacter)]
    [InlineData("", IdentifierProblem.BadCharacter)]
    public void Check_volgt_de_compiler(string name, IdentifierProblem expected)
    {
        Assert.Equal(expected, IdentifierRules.Check(name));
    }

    [Theory]
    [InlineData("shadow", "shadow", true)]
    [InlineData("@shadow", "shadow", true)]
    [InlineData("Shadow", "shadow", false)]
    [InlineData("_shadow", "shadow", false)]
    [InlineData("2shadow", "shadow", false)]
    public void Hoofdletters_tellen_een_apenstaartje_niet(string a, string b, bool same)
    {
        Assert.Equal(same, IdentifierRules.SameName(a, b));
    }
}
