using DeckOverflow.Engine.Text;

namespace DeckOverflow.Engine.Cards;

/// <summary>
/// De tekst van een kaart volgt uit haar effect. Zo klopt het getal op de kaart altijd,
/// ook na verbeteren of omgieten, zonder dat iemand het twee keer typt.
/// </summary>
public static class CardText
{
    public static TextRef Of(CardDefinition card) => Of(card.Effect);

    public static TextRef Of(Effect effect) => effect switch
    {
        DamageEffect { Hits: 1 } d => TextRef.Of("effect.damage", ("amount", d.Amount)),
        DamageEffect d => TextRef.Of("effect.damage-hits", ("amount", d.Amount), ("hits", d.Hits)),
        BlockEffect b => TextRef.Of("effect.block", ("amount", b.Amount)),
        HealEffect h => TextRef.Of("effect.heal", ("amount", h.Amount)),
        CastEffect => TextRef.Of("effect.cast"),
        ConvertEffect => TextRef.Of("effect.convert"),
        SetAttackEffect s => TextRef.Of("effect.set-attack", ("value", s.Value)),
        ModifierEffect { Op: ModifierOp.Add } m => TextRef.Of("effect.add", ("amount", m.Operand.Literal)),
        ModifierEffect { Op: ModifierOp.Multiply } m => TextRef.Of("effect.multiply", ("amount", m.Operand.Literal)),
        ModifierEffect { Op: ModifierOp.Divide, DoubleHits: true } m => TextRef.Of("effect.split", ("amount", m.Operand.Literal)),
        ModifierEffect { Op: ModifierOp.Divide } m => TextRef.Of("effect.divide", ("amount", m.Operand.Literal)),
        ModifierEffect { Op: ModifierOp.Parse } => TextRef.Of("effect.parse"),
        ComboEffect { First: CastEffect, Then: HealEffect h } => TextRef.Of("effect.cast-heal", ("amount", h.Amount)),
        _ => throw new NotSupportedException($"Geen tekst voor {effect.GetType().Name}.")
    };
}
