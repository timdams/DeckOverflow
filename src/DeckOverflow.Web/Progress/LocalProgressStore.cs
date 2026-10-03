using System.Text.Json;
using DeckOverflow.Web.World;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Progress;

/// <summary>
/// Voortgang in <c>localStorage</c>, als één JSON-blok. Opslag kan leeg terugkomen of falen
/// (privévenster): dan begin je gewoon opnieuw, het spel loopt door.
/// </summary>
public sealed class LocalProgressStore(IJSRuntime js) : IProgressStore
{
    private const string Key = "deckoverflow.progress";

    /// <summary>Lokaal komt er nooit iets van elders binnen.</summary>
    public event Action? Changed { add { } remove { } }

    public async Task<PlayerProgress> LoadAsync()
    {
        string? json = await js.InvokeAsync<string?>("deckOverflow.load", Key);
        if (!string.IsNullOrEmpty(json))
        {
            try { return JsonSerializer.Deserialize<PlayerProgress>(json) ?? new PlayerProgress(); }
            catch (JsonException) { return new PlayerProgress(); /* kapotte opslag: met een lege fabriek verder */ }
        }

        var migrated = await MigrateAsync();
        await SaveAsync(migrated);
        return migrated;
    }

    public async Task SaveAsync(PlayerProgress progress) =>
        await js.InvokeVoidAsync("deckOverflow.save", Key, JsonSerializer.Serialize(progress));

    public Task RefreshAsync() => Task.CompletedTask;

    /// <summary>Een lege voortgang bewaren, niet de sleutel wissen: anders komen de oude sleutels terug via de migratie.</summary>
    public Task ForgetAsync() => SaveAsync(new PlayerProgress());

    /// <summary>
    /// Voor 3 oktober 2026 stond elk stuk voortgang onder een eigen sleutel. Wie toen al speelde,
    /// houdt zijn Codex, panelen en onthulling. De oude sleutels blijven staan maar worden niet meer gelezen.
    /// </summary>
    private async Task<PlayerProgress> MigrateAsync()
    {
        var progress = new PlayerProgress
        {
            Codex = await LoadJsonAsync<Dictionary<string, IReadOnlyDictionary<string, string>>>("deckoverflow.codex") ?? [],
            CodexRead = await LoadJsonAsync<Dictionary<string, int>>("deckoverflow.codex-read") ?? [],
            XPanels = await LoadJsonAsync<HashSet<string>>("deckoverflow.xpanels") ?? [],
            ReachedAct = int.TryParse(await LoadAsync("deckoverflow.reached-act"), out int act) && act > 1 ? act : 1,
            RunsStarted = int.TryParse(await LoadAsync("deckoverflow.runs"), out int runs) ? runs : 0,
            IntroSeen = await LoadAsync("deckoverflow.intro-seen") is not null,
            RevealedBy = await LoadAsync("deckoverflow.revealed"),
        };
        if (await LoadAsync("deckoverflow.card-hall-cleared") is not null)
        {
            progress.TryUnlock(Departments.ControlRoom, UnlockHow.Boss, DateTimeOffset.UtcNow);
        }
        return progress;
    }

    private ValueTask<string?> LoadAsync(string key) => js.InvokeAsync<string?>("deckOverflow.load", key);

    private async Task<T?> LoadJsonAsync<T>(string key)
    {
        string? json = await LoadAsync(key);
        if (string.IsNullOrEmpty(json)) return default;
        try { return JsonSerializer.Deserialize<T>(json); }
        catch (JsonException) { return default; }
    }
}
