namespace DeckOverflow.Engine.Codex;

/// <summary>
/// Eén pagina in de Codex: een C#-regel die je in het spel voelde, met haar echte naam.
/// Naam, uitleg en code staan in <c>en.json</c> onder <c>codex.&lt;key&gt;</c>.
/// </summary>
/// <param name="Chapter">Het hoofdstuk in Zie Scherp Scherper. Leeg: een later hoofdstuk.</param>
/// <param name="MinAct">
/// Vanaf welke act de pagina mag opengaan. Eerst ervaren, dan benoemen: Omgieten voel je in act 1,
/// maar de pagina "Casting" opent pas in act 2, bij hoofdstuk 4.
/// </param>
public sealed record CodexEntry(string Key, int? Chapter, int MinAct = 1);

public static class CodexCatalog
{
    public const string Variables = "variables";
    public const string IntTruncation = "int-truncation";
    public const string IntegerDivision = "integer-division";
    public const string Overflow = "overflow";
    public const string OperatorPrecedence = "operator-precedence";
    public const string StringConcat = "string-concat";
    public const string Casting = "casting";
    public const string Convert = "convert";
    public const string Rounding = "rounding";
    public const string Parse = "parse";
    public const string Exceptions = "exceptions";

    /// <summary>In de volgorde van het boek.</summary>
    public static readonly IReadOnlyList<CodexEntry> All =
    [
        new(Variables, 2),
        new(IntTruncation, 2),
        new(IntegerDivision, 2),
        new(Overflow, 2),
        new(OperatorPrecedence, 2),
        new(StringConcat, 3),
        new(Casting, 4, MinAct: 2),
        new(Convert, 4, MinAct: 2),
        new(Rounding, 4, MinAct: 2),
        new(Parse, 4, MinAct: 2),
        new(Exceptions, null),
    ];

    public static CodexEntry Get(string key) => All.First(e => e.Key == key);
}

/// <summary>
/// Het eerste moment in een gevecht waarop een regel iets deed, met de getallen erbij.
/// Die getallen vullen "wat er gebeurde" en de code onder de motorkap, zodat de pagina
/// precies jouw moment naspeelt.
/// </summary>
public sealed record CodexMoment(string Key, IReadOnlyDictionary<string, string> Values);
