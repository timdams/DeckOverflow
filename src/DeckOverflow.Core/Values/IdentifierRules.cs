namespace DeckOverflow.Core.Values;

/// <summary>Waarom een naam geen geldige C#-naam is, of <see cref="None"/> als hij dat wel is.</summary>
public enum IdentifierProblem { None, StartsWithDigit, Keyword, BadCharacter }

/// <summary>
/// De regels voor een naam (identifier) in C#: een letter of <c>_</c> vooraan, daarna alleen letters,
/// cijfers en <c>_</c>, en geen gereserveerd woord, tenzij met <c>@</c> ervoor. Hoofdletters tellen:
/// <c>Shadow</c> is een andere naam dan <c>shadow</c>.
/// </summary>
public static class IdentifierRules
{
    /// <summary>De gereserveerde woorden van C#. Contextuele woorden als <c>var</c> mogen wel een naam zijn.</summary>
    private static readonly HashSet<string> Keywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
        "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint",
        "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while",
    ];

    public static IdentifierProblem Check(string name)
    {
        bool verbatim = name.StartsWith('@');
        string body = verbatim ? name[1..] : name;

        if (body.Length == 0) return IdentifierProblem.BadCharacter;
        if (char.IsDigit(body[0])) return IdentifierProblem.StartsWithDigit;
        if (!(char.IsLetter(body[0]) || body[0] == '_')) return IdentifierProblem.BadCharacter;
        if (body.Any(c => !(char.IsLetterOrDigit(c) || c == '_'))) return IdentifierProblem.BadCharacter;
        if (!verbatim && Keywords.Contains(body)) return IdentifierProblem.Keyword;
        return IdentifierProblem.None;
    }

    public static bool IsValid(string name) => Check(name) == IdentifierProblem.None;

    /// <summary>Wijzen twee geldige namen naar dezelfde variabele? <c>@shadow</c> is <c>shadow</c>, <c>Shadow</c> niet.</summary>
    public static bool SameName(string a, string b) =>
        IsValid(a) && IsValid(b) && string.Equals(a.TrimStart('@'), b.TrimStart('@'), StringComparison.Ordinal);
}
