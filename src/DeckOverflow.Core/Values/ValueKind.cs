using System.Text.Json.Serialization;

namespace DeckOverflow.Core.Values;

/// <summary>Het C#-type dat een waarde in de spelwereld draagt.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ValueKind>))]
public enum ValueKind { Int, Byte, Double, Bool, String, Char }
