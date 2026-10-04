using DeckOverflow.Engine.Belts;

namespace DeckOverflow.Web.Components;

/// <summary>Hoe een stuk op het rooster eruitziet: korte tekens, geen woorden. Alleen beeld, geen regels.</summary>
public static class PieceText
{
    public static string Arrow(Dir dir) => dir switch
    {
        Dir.Up => "↑",
        Dir.Right => "→",
        Dir.Down => "↓",
        Dir.Left => "←",
        _ => "?",
    };

    public static string Op(Op op) => op.Kind switch
    {
        OpKind.Add => $"+{op.Operand}",
        OpKind.Subtract => $"−{op.Operand}",
        OpKind.Multiply => $"×{op.Operand}",
        OpKind.Divide => $"/{op.Operand}",
        OpKind.Append => $"+\"{op.Text}\"",
        _ => "?",
    };

    public static string Condition(Condition c) => c.Kind switch
    {
        CondKind.Less => $"< {c.N}",
        CondKind.Greater => $"> {c.N}",
        CondKind.Equal => $"== {c.N}",
        CondKind.ModZero => $"% {c.N} == 0",
        CondKind.LengthLess => $".Length < {c.N}",
        _ => "?",
    };

    /// <summary>Het label van iets uit de gereedschapsbak.</summary>
    public static string Offer(Offer offer) => offer.Kind switch
    {
        OfferKind.Machine => offer.Op switch
        {
            OpKind.Append => $"+\"{offer.Text}\"",
            _ => Op(new Op(offer.Op, offer.Choices.Count > 0 ? offer.Choices[0] : 0)),
        },
        OfferKind.Gate => offer.Choices.Count > 1 ? "poort" : Condition(new Condition((offer.Conditions ?? [])[0], offer.Choices[0])),
        OfferKind.Counter => "teller",
        _ => "?",
    };

    /// <summary>Het type van een kist als CSS-klasse: kleur is voorbehouden aan types.</summary>
    public static string KindClass(Kind kind) => kind.ToString().ToLowerInvariant();
}
