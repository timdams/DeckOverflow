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
}
