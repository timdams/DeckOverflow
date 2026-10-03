namespace DeckOverflow.Engine.Gambits;

/// <summary>De vaste getallen van een automaat.</summary>
/// <param name="Repairs">Hoe vaak Patch Up werkt. Daarna doet de zet niets meer, maar de regel klopt nog wel.</param>
public sealed record BotSpec(string Key, int MaxHp, int WhackDamage, int BlockAmount, int RepairAmount, int Repairs);

/// <summary>Een automaat tijdens een duel.</summary>
public sealed class Bot
{
    public Bot(BotSpec spec)
    {
        Spec = spec;
        Hp = spec.MaxHp;
        RepairsLeft = spec.Repairs;
    }

    public BotSpec Spec { get; }
    public int Hp { get; internal set; }
    public int Block { get; internal set; }
    public bool Charged { get; internal set; }
    public int RepairsLeft { get; internal set; }
    public bool Dead => Hp <= 0;

    public BotState State() => new(Spec.Key, Hp, Spec.MaxHp, Block, Charged, RepairsLeft);
}

public sealed record BotState(string Key, int Hp, int MaxHp, int Block, bool Charged, int RepairsLeft);
