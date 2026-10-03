namespace DeckOverflow.Web.Progress;

/// <summary>
/// Waar de voortgang bewaard wordt. Lokaal eerst: een implementatie mag nooit een run breken,
/// ook niet zonder netwerk of opslag. Later synchroniseert een tweede implementatie met Supabase.
/// </summary>
public interface IProgressStore
{
    /// <summary>De bewaarde voortgang, of een lege als er niets (bruikbaars) is.</summary>
    Task<PlayerProgress> LoadAsync();

    Task SaveAsync(PlayerProgress progress);

    /// <summary>
    /// Voortgang van elders (een ander toestel) is samengevoegd in het object dat <see cref="LoadAsync"/>
    /// teruggaf. Tijd om opnieuw te tekenen.
    /// </summary>
    event Action? Changed;

    /// <summary>Voortgang van elders opnieuw ophalen: na inloggen of aansluiten bij een klas.</summary>
    Task RefreshAsync();

    /// <summary>
    /// Alles op dit toestel vergeten: na afmelden of het verwijderen van het account. Zo erft
    /// wie na jou op een gedeelde schoolcomputer speelt, niets van jouw fabriek.
    /// </summary>
    Task ForgetAsync();
}
