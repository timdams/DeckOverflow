using System.Globalization;
using System.Text.RegularExpressions;

namespace DeckOverflow.CardHall.Combat;

/// <summary>
/// Wat een vijand van plan is. <see cref="Value"/> wordt door C# zelf uitgerekend
/// op de plek waar de intent gedefinieerd wordt, niet door een eigen parser.
/// Een bewuste intent heeft een <see cref="Formula"/> met variabelen uit jouw toestand:
/// <c>30 / (block + 1)</c> wordt kleiner naarmate je meer blokt, met een deling van gehele getallen.
/// </summary>
/// <param name="Hidden">Het totaal blijft verborgen tot de aanval valt. Alleen voor de Rekenmeester.</param>
public sealed partial record Intent(string Expression, double Value, bool Hidden = false)
{
    /// <summary>Rekent de aanval uit met wat de speler nu heeft. Leeg voor een vaste intent.</summary>
    public Func<IntentContext, double>? Formula { get; init; }

    public bool IsLive => Formula is not null;

    /// <summary>
    /// Dezelfde aanval met de haakjes elders, voor de kaart Move the Brackets. Leeg als er niets te verschuiven valt.
    /// Ook hier rekent C# zelf: <c>30 / block + 1</c> deelt echt door nul als je geen blok hebt.
    /// </summary>
    public Intent? Regrouped { get; init; }

    /// <summary>
    /// Een eigen invulling voor het logboek, als vervangen per variabele geen geldige expressie geeft:
    /// <c>shots++ + ++shots</c> wordt <c>1 + 3</c>, niet <c>1++ + ++1</c>.
    /// </summary>
    public Func<IntentContext, string>? Fill { get; init; }

    /// <summary>
    /// Een bewuste intent. De variabelen in de expressie heten <c>block</c>, <c>cards</c>, <c>energy</c> en <c>hp</c>
    /// (van jou), en <c>isSolid</c>, <c>turn</c> en <c>shots</c> (van het gevecht).
    /// </summary>
    public static Intent Live(string expression, Func<IntentContext, double> formula) =>
        new(expression, SafeDefault(formula)) { Formula = formula };

    public double ValueIn(IntentContext context) => Formula?.Invoke(context) ?? Value;

    /// <summary>De waarde, of leeg als het uitrekenen crasht: <c>30 / 0</c> is een <c>DivideByZeroException</c>.</summary>
    public double? TryValueIn(IntentContext context)
    {
        try { return ValueIn(context); }
        catch (DivideByZeroException) { return null; }
    }

    /// <summary>De expressie met de getallen ingevuld: <c>30 / (5 + 1)</c>. Voor het logboek en de Codex.</summary>
    public string FilledIn(IntentContext context) => !IsLive ? Expression : Fill?.Invoke(context) ?? Variable().Replace(Expression, m => m.Value switch
    {
        "block" => Num(context.Block),
        "cards" => Num(context.Cards),
        "energy" => Num(context.Energy),
        "hp" => Num(context.Hp),
        "isSolid" => context.Solid ? "true" : "false",
        "turn" => Num(context.Turn),
        "shots" => Num(context.Shots),
        _ => m.Value
    });

    private static double SafeDefault(Func<IntentContext, double> formula)
    {
        try { return formula(default); }
        catch (DivideByZeroException) { return 0; }
    }

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\b(block|cards|energy|hp|isSolid|turn|shots)\b")]
    private static partial Regex Variable();
}

/// <summary>
/// Wat een bewuste intent mag zien: jouw blok, gespeelde kaarten deze beurt, overgebleven energie en HP,
/// plus de <c>bool</c> van de Bool Ghost, het nummer van de beurt en de teller van de Twin Shooters.
/// </summary>
public readonly record struct IntentContext(int Block, int Cards, int Energy, int Hp, bool Solid = false, int Turn = 0, int Shots = 0);
