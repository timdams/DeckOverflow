namespace DeckOverflow.Core.Codex;

/// <summary>
/// Eén pagina in de Codex: een C#-regel die je in het spel voelde, met haar echte naam.
/// Naam, uitleg en code staan in <c>en.json</c> onder <c>codex.&lt;key&gt;</c>.
/// </summary>
/// <param name="Chapter">Het hoofdstuk in Zie Scherp Scherper. Leeg: een later hoofdstuk.</param>
/// <param name="BookPage">De pagina in de online versie van het boek, relatief tot <c>content/</c>, eventueel met anker.</param>
/// <param name="MinAct">
/// Vanaf welke act de pagina mag opengaan. Eerst ervaren, dan benoemen: Omgieten voel je in act 1,
/// maar de pagina "Casting" opent pas in act 3, bij hoofdstuk 4.
/// </param>
public sealed record CodexEntry(string Key, int? Chapter, string BookPage, int MinAct = 1);

public static class CodexCatalog
{
    public const string Variables = "variables";
    public const string Identifiers = "identifiers";
    public const string IntTruncation = "int-truncation";
    public const string IntegerDivision = "integer-division";
    public const string Modulo = "modulo";
    public const string Increment = "increment";
    public const string Booleans = "booleans";
    public const string Overflow = "overflow";
    public const string OperatorPrecedence = "operator-precedence";
    public const string Constants = "constants";
    public const string StringConcat = "string-concat";
    public const string StringLength = "string-length";
    public const string CharIsNumber = "char-is-number";
    public const string Casting = "casting";
    public const string Convert = "convert";
    public const string Rounding = "rounding";
    public const string Parse = "parse";
    public const string IfElse = "if-else";
    public const string RelationalOperators = "relational-operators";
    public const string LogicalOperators = "logical-operators";
    public const string WhileLoop = "while-loop";
    public const string ForLoop = "for-loop";
    public const string NestedLoops = "nested-loops";
    public const string InfiniteLoop = "infinite-loop";
    public const string Exceptions = "exceptions";

    /// <summary>In de volgorde van het boek.</summary>
    public static readonly IReadOnlyList<CodexEntry> All =
    [
        new(Variables, 2, "1_csharpbasics/1b_variabelen.html"),
        new(Identifiers, 2, "1_csharpbasics/1b_variabelen.html"),
        new(IntTruncation, 2, "1_csharpbasics/1_datatypes.html"),
        new(IntegerDivision, 2, "1_csharpbasics/2_expressies.html"),
        new(Modulo, 2, "1_csharpbasics/2_expressies.html"),
        new(Increment, 2, "1_csharpbasics/2_expressies.html"),
        new(Booleans, 2, "1_csharpbasics/1_datatypes.html"),
        new(Overflow, 2, "1_csharpbasics/1_datatypes.html"),
        new(OperatorPrecedence, 2, "1_csharpbasics/2_expressies.html"),
        new(Constants, 2, "1_csharpbasics/1b_variabelen.html"),
        new(StringConcat, 3, "2_tekst/5_chars_strings.html", MinAct: 2),
        new(StringLength, 3, "2_tekst/5_chars_strings.html", MinAct: 2),
        new(CharIsNumber, 3, "2_tekst/5_chars_strings.html", MinAct: 2),
        new(Casting, 4, "3_data/4_converteren_casting.html#casting", MinAct: 3),
        new(Convert, 4, "3_data/4_converteren_casting.html#conversie", MinAct: 3),
        new(Rounding, 4, "3_data/4d_afronden.html", MinAct: 3),
        new(Parse, 4, "3_data/4_converteren_casting.html#parsing-en-.tostring", MinAct: 3),
        // H5, uit de Controlekamer: de eerste regel die klopt, wint
        new(IfElse, 5, "4_beslissingen/0_if.html#if---else-if"),
        new(RelationalOperators, 5, "4_beslissingen/1_logic_and_relationsoperator.html#relationele-operators"),
        new(LogicalOperators, 5, "4_beslissingen/1_logic_and_relationsoperator.html#logische-operators"),
        // H6, uit de Lopende Band: een band die terugkomt, is een loop
        new(WhileLoop, 6, "5_herhalingen/1_while_dowhile.html"),
        new(ForLoop, 6, "5_herhalingen/2_for.html"),
        new(NestedLoops, 6, "5_herhalingen/3_nesting.html"),
        new(InfiniteLoop, 6, "5_herhalingen/0_loops_intro.html"),
        new(Exceptions, 10, "20_exceptions/0_exceptionhandling.html"),
    ];

    public static CodexEntry Get(string key) => All.First(e => e.Key == key);
}

/// <summary>
/// Het eerste moment in een gevecht waarop een regel iets deed, met de getallen erbij.
/// Die getallen vullen "wat er gebeurde" en de code onder de motorkap, zodat de pagina
/// precies jouw moment naspeelt.
/// </summary>
public sealed record CodexMoment(string Key, IReadOnlyDictionary<string, string> Values);
