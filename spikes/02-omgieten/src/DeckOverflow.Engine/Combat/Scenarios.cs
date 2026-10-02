using DeckOverflow.Engine.Cards;
using DeckOverflow.Engine.Values;

namespace DeckOverflow.Engine.Combat;

/// <summary>
/// De gevechten van spike 2. De getallen zijn eerste gokken en worden in de playtest afgesteld.
/// </summary>
public static class Scenarios
{
    public const ulong DefaultSeed = 255;

    /// <summary>Volgorde waarin een tester ze speelt.</summary>
    public static readonly IReadOnlyList<string> Order = ["geest", "kolos"];

    public static CombatSetup Create(string key) => key switch
    {
        "geest" => Geest(),
        "kolos" => Kolos(),
        _ => throw new ArgumentException($"Onbekend gevecht: {key}", nameof(key))
    };

    public static bool Exists(string? key) => key is not null && Order.Contains(key);

    /// <summary>Iedereen speelt met hetzelfde deck, zodat alleen de vijand verschilt.</summary>
    public static IReadOnlyList<CardDefinition> StarterDeck() =>
    [
        CardCatalog.Slag, CardCatalog.Slag, CardCatalog.Slag,
        CardCatalog.VlottendeSlag, CardCatalog.VlottendeSlag,
        CardCatalog.Schild, CardCatalog.Schild,
        CardCatalog.Herstel,
        CardCatalog.GietOmInt, CardCatalog.GietOmByte
    ];

    private static CombatantSetup Player() => new("jij", "Jij", ValueKind.Int, Hp: 50, MaxHp: 50);

    /// <summary>
    /// Een double met een decimaal schild. Als double vangt het schild exact op.
    /// Naar int omgegoten verliest hij de restjes, en kost elke halve treffer hem een hele schildpunt.
    /// </summary>
    public static CombatSetup Geest() => new(
        Player(),
        new EnemySetup(
            new CombatantSetup("geest", "Vlottende Geest", ValueKind.Double, Hp: 24.5, MaxHp: 24.5, Block: 12.5),
            Attack: new Intent("9 / 2.0", 9 / 2.0),
            BlockAfterAttack: 12.5),
        StarterDeck());

    /// <summary>
    /// Te groot om plat te slaan. Naar byte omgegoten wordt (byte)506 gelijk aan 250,
    /// en dan werkt de golem-truc: Herstel laat hem overlopen naar 0.
    /// </summary>
    public static CombatSetup Kolos() => new(
        Player(),
        new EnemySetup(
            new CombatantSetup("kolos", "Tinnen Kolos", ValueKind.Int, Hp: 506, MaxHp: 506),
            Attack: new Intent("12 + 3 * 2", 12 + 3 * 2)),
        StarterDeck());
}
