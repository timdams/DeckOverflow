namespace DeckOverflow.CardHall.Runs;

/// <summary>
/// De score van een run, voor de Prikklok. Eenvoudig genoeg om aan een student uit te leggen:
/// 100 per verdieping die je achter je liet, over alle acts heen. Wie uitspeelt, krijgt er 10 per
/// resterende HP bij, en 5 per beurt onder de <see cref="TurnPar"/>. Verliezen geeft geen bonus,
/// dus snel sterven loont nooit. Een gewonnen run scoort altijd meer dan een verloren run.
/// De motor rekent hem uit; <c>Run.Replay</c> rekent hem na.
/// </summary>
/// <param name="Floors">Verdiepingen voorbij, baas inbegrepen. Een volledige run is 3 × 7 = 21.</param>
/// <param name="Turns">Beurten in alle gevechten samen.</param>
public sealed record RunScore(int Floors, bool Won, int HpLeft, int Turns)
{
    public const int PerFloor = 100;
    public const int PerHp = 10;
    public const int PerTurnSaved = 5;
    public const int TurnPar = 150;

    public int FloorPoints => Floors * PerFloor;
    public int HpPoints => Won ? HpLeft * PerHp : 0;
    public int TurnPoints => Won ? Math.Max(0, TurnPar - Turns) * PerTurnSaved : 0;
    public int Total => FloorPoints + HpPoints + TurnPoints;
}
