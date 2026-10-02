using System.Text.Json.Serialization;

namespace DeckOverflow.Engine.Cards;

[JsonConverter(typeof(JsonStringEnumConverter<TargetMode>))]
public enum TargetMode
{
    /// <summary>Een vijand.</summary>
    Enemy,
    /// <summary>Altijd de speler zelf.</summary>
    Self,
    /// <summary>Elk levend doelwit, vriend of vijand.</summary>
    Any
}
