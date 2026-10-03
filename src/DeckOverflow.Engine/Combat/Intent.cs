using System.Globalization;
using System.Text.RegularExpressions;

namespace DeckOverflow.Engine.Combat;

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

    /// <summary>Een bewuste intent. De variabelen in de expressie heten <c>block</c>, <c>cards</c>, <c>energy</c> en <c>hp</c>.</summary>
    public static Intent Live(string expression, Func<IntentContext, double> formula) =>
        new(expression, formula(default)) { Formula = formula };

    public double ValueIn(IntentContext context) => Formula?.Invoke(context) ?? Value;

    /// <summary>De expressie met de getallen ingevuld: <c>30 / (5 + 1)</c>. Voor het logboek en de Codex.</summary>
    public string FilledIn(IntentContext context) => !IsLive ? Expression : Variable().Replace(Expression, m => m.Value switch
    {
        "block" => Num(context.Block),
        "cards" => Num(context.Cards),
        "energy" => Num(context.Energy),
        "hp" => Num(context.Hp),
        _ => m.Value
    });

    private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

    [GeneratedRegex(@"\b(block|cards|energy|hp)\b")]
    private static partial Regex Variable();
}

/// <summary>Wat een bewuste intent van de speler mag zien: blok, gespeelde kaarten deze beurt, overgebleven energie, HP.</summary>
public readonly record struct IntentContext(int Block, int Cards, int Energy, int Hp);
