using Microsoft.AspNetCore.Components;

namespace DeckOverflow.Web.Backend;

/// <summary>
/// De superuser (de maker): wint een gevecht met W, speelt elke gebouwde afdeling en elk level, ook wat
/// voor spelers nog dicht is, ziet altijd de plattegrond en mag de testroutes <c>?level=</c>, <c>?fight=</c> en <c>?seed=</c>.
/// Er wordt niets in de voortgang geschreven: Codex, panelen en sleutels blijven echt.
/// Wie dat is, staat alleen in Supabase (<c>app_metadata.role = 'superuser'</c>), nooit in de code.
/// Lokaal (<c>localhost</c>) is iedereen superuser, om te ontwikkelen zonder in te loggen.
/// Een controle in de browser: wie de app patcht, komt erlangs. Er staat niets op het spel dat dat de moeite waard maakt.
/// </summary>
public sealed class Superuser(NavigationManager nav, IServiceProvider services)
{
    public const string Role = "superuser";

    private bool? _active;

    public async Task<bool> IsActiveAsync()
    {
        if (_active is { } known) return known;
        if (IsLocal(nav.BaseUri)) return (_active = true).Value;
        if (services.GetService<SupabaseClient>() is not { } supabase) return (_active = false).Value;
        return (_active = await supabase.RoleAsync() == Role).Value;
    }

    /// <summary>Opnieuw kijken, na inloggen of afmelden.</summary>
    public void Forget() => _active = null;

    internal static bool IsLocal(string baseUri) =>
        new Uri(baseUri).Host is "localhost" or "127.0.0.1" or "[::1]";
}
