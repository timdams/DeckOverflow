using System.Text.Json;
using System.Text.Json.Serialization;
using DeckOverflow.Engine.Belts;

namespace DeckOverflow.Web.Components;

/// <summary>Een stuk van het bord als JSON, om je borden in de browser te bewaren.</summary>
public static class PieceJson
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(BeltDto), "belt")]
    [JsonDerivedType(typeof(MachineDto), "machine")]
    [JsonDerivedType(typeof(GateDto), "gate")]
    [JsonDerivedType(typeof(CounterDto), "counter")]
    private abstract record Dto;
    private sealed record BeltDto(Dir Out) : Dto;
    private sealed record MachineDto(OpKind Kind, int Operand, string Text, Dir Out) : Dto;
    private sealed record GateDto(CondKind Kind, int N, Dir IfTrue, Dir IfFalse) : Dto;
    private sealed record CounterDto(int Times, Dir Loop, Dir Done) : Dto;

    public static string Write(Piece piece) => JsonSerializer.Serialize<Dto>(piece switch
    {
        Belt b => new BeltDto(b.Out),
        Machine m => new MachineDto(m.Op.Kind, m.Op.Operand, m.Op.Text, m.Out),
        Gate g => new GateDto(g.When.Kind, g.When.N, g.IfTrue, g.IfFalse),
        Counter c => new CounterDto(c.Times, c.Loop, c.Done),
        _ => new BeltDto(Dir.Right),
    });

    public static Piece? Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dto>(json) switch
            {
                BeltDto b => new Belt(b.Out),
                MachineDto m => new Machine(new Op(m.Kind, m.Operand, m.Text), m.Out),
                GateDto g => new Gate(new Condition(g.Kind, g.N), g.IfTrue, g.IfFalse),
                CounterDto c => new Counter(c.Times, c.Loop, c.Done),
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
