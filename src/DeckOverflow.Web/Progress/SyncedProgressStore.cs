using System.Text.Json;
using DeckOverflow.Core.Codex;
using DeckOverflow.Web.Backend;
using DeckOverflow.Web.World;

namespace DeckOverflow.Web.Progress;

/// <summary>
/// Lokaal eerst, Supabase op de achtergrond. Laden en bewaren gaan altijd meteen naar
/// <c>localStorage</c>; de synchronisatie volgt en faalt stil. Een haperend schoolnetwerk breekt
/// nooit een run: wat niet lukte, gaat mee met de volgende keer bewaren.
/// </summary>
/// <remarks>
/// Gesynchroniseerd: Codex-pagina's (met de getallen van het eerste moment), gemonteerde onderdelen
/// en ontgrendelde afdelingen. De rest (leesstand, panelen, startpunt, onthulling) blijft in de browser.
/// Samenvoegen is altijd een unie: niets gaat ooit terug, ook niet in de database.
/// </remarks>
public sealed class SyncedProgressStore(LocalProgressStore local, SupabaseClient supabase) : IProgressStore
{
    public event Action? Changed;

    private PlayerProgress? _progress;
    private bool _pulled;
    private bool _running;
    private bool _again;

    /// <summary>Wat al in de database staat, zodat we alleen nieuwe rijen sturen.</summary>
    private readonly HashSet<string> _synced = [];

    public async Task<PlayerProgress> LoadAsync()
    {
        _progress = await local.LoadAsync();
        _ = SyncAsync();
        return _progress;
    }

    public async Task SaveAsync(PlayerProgress progress)
    {
        _progress = progress;
        await local.SaveAsync(progress);
        _ = SyncAsync();
    }

    public Task RefreshAsync()
    {
        _pulled = false;
        _synced.Clear();
        return SyncAsync();
    }

    public async Task ForgetAsync()
    {
        await local.ForgetAsync();
        _progress = null;
        _pulled = false;
        _synced.Clear();
    }

    /// <summary>Eén synchronisatie tegelijk; wie tijdens een lopende bewaart, krijgt er meteen nog een.</summary>
    private async Task SyncAsync()
    {
        if (_running)
        {
            _again = true;
            return;
        }
        _running = true;
        try
        {
            do
            {
                _again = false;
                await SyncOnceAsync();
            } while (_again);
        }
        catch (Exception e)
        {
            // Geen netwerk, Supabase gepauzeerd, limiet bereikt: het spel speelt lokaal verder
            Console.WriteLine($"Synchronisatie lukte niet, de volgende keer opnieuw: {e.Message}");
        }
        finally
        {
            _running = false;
        }
    }

    private async Task SyncOnceAsync()
    {
        if (_progress is null) return;
        await supabase.EnsureSessionAsync();
        string userId = supabase.UserId!;

        if (!_pulled && await PullAsync(userId))
        {
            await local.SaveAsync(_progress);
            Changed?.Invoke();
        }
        _pulled = true;
        await PushAsync(userId);
    }

    /// <summary>Voortgang van een ander toestel binnenhalen. Waar als er iets nieuws bij kwam.</summary>
    private async Task<bool> PullAsync(string userId)
    {
        var progress = _progress!;
        bool changed = false;

        var parts = await supabase.SelectAsync<ProgressRow>("progress", $"select=item,state,values&user_id=eq.{userId}");
        foreach (var row in parts)
        {
            _synced.Add(PartKey(row.Item, row.State));
            // Een pagina die deze versie van het spel niet kent, laten we liggen
            if (!CodexCatalog.All.Any(e => e.Key == row.Item)) continue;
            if (row.State is "unpacked" or "assembled" && !progress.Codex.ContainsKey(row.Item))
            {
                progress.Codex[row.Item] = ToValues(row.Values);
                changed = true;
            }
            if (row.State == "assembled") changed |= progress.Assembled.Add(row.Item);
        }

        var unlocks = await supabase.SelectAsync<UnlockRow>("unlocks", $"select=department,how,unlocked_at&user_id=eq.{userId}");
        foreach (var row in unlocks)
        {
            _synced.Add(UnlockKey(row.Department));
            if (ParseHow(row.How) is { } how) changed |= progress.TryUnlock(row.Department, how, row.UnlockedAt);
        }

        // Wat de docent van een van je klassen vrijgaf, gaat open. Alleen klassen waar je lid van
        // bent: een docent ontgrendelt niets voor zichzelf (de database zou dat ook weigeren).
        var memberships = await supabase.SelectAsync<MembershipRow>("class_members", $"select=classes(released_departments)&user_id=eq.{userId}");
        foreach (string department in memberships.SelectMany(m => m.Classes?.ReleasedDepartments ?? []))
        {
            if (department == Departments.CardHall || !Departments.All.Any(d => d.Key == department)) continue;
            changed |= progress.TryUnlock(department, UnlockHow.Teacher, DateTimeOffset.UtcNow);
        }

        // Een fabriek met een open afdeling heeft de onthulling al achter de rug, ook al gebeurde
        // die op een ander toestel
        if (changed && !progress.Revealed && progress.Unlocks.Count > 0) progress.RevealedBy = "synced";
        return changed;
    }

    private async Task PushAsync(string userId)
    {
        var progress = _progress!;
        var now = DateTimeOffset.UtcNow;

        var parts = progress.Codex.Keys.Concat(progress.Assembled).Distinct()
            .Select(item => new ProgressRow(item, Name(progress.StateOf(item)), progress.Codex.GetValueOrDefault(item)) { UserId = userId, UpdatedAt = now })
            .Where(row => !_synced.Contains(PartKey(row.Item, row.State)))
            .ToList();
        await supabase.UpsertAsync("progress", parts);
        foreach (var row in parts) _synced.Add(PartKey(row.Item, row.State));

        var unlocks = progress.Unlocks
            .Where(u => !_synced.Contains(UnlockKey(u.Key)))
            .Select(u => new UnlockRow(u.Key, Name(u.Value.How), u.Value.At) { UserId = userId })
            .ToList();
        await supabase.UpsertAsync("unlocks", unlocks, ignoreDuplicates: true);
        foreach (var row in unlocks) _synced.Add(UnlockKey(row.Department));
    }

    private static string PartKey(string item, string state) => $"part:{item}:{state}";
    private static string UnlockKey(string department) => $"unlock:{department}";

    private static string Name(PartState state) => state switch
    {
        PartState.Assembled => "assembled",
        PartState.Unpacked => "unpacked",
        _ => "in_bag",
    };

    private static string Name(UnlockHow how) => how switch
    {
        UnlockHow.Boss => "boss",
        UnlockHow.SafetyNet => "safety_net",
        _ => "teacher",
    };

    private static UnlockHow? ParseHow(string how) => how switch
    {
        "boss" => UnlockHow.Boss,
        "safety_net" => UnlockHow.SafetyNet,
        "teacher" => UnlockHow.Teacher,
        _ => null,
    };

    private static IReadOnlyDictionary<string, string> ToValues(object? values) =>
        values is JsonElement { ValueKind: JsonValueKind.Object } v
            ? v.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.ValueKind == JsonValueKind.String ? p.Value.GetString()! : p.Value.GetRawText())
            : new Dictionary<string, string>();

    /// <summary>Een rij uit <c>progress</c>. Bij het lezen is <see cref="Values"/> een JSON-object.</summary>
    private sealed record ProgressRow(string Item, string State, object? Values)
    {
        public string? UserId { get; init; }
        public DateTimeOffset? UpdatedAt { get; init; }
    }

    private sealed record MembershipRow(ClassRow? Classes);
    private sealed record ClassRow(IReadOnlyList<string>? ReleasedDepartments);

    private sealed record UnlockRow(string Department, string How, DateTimeOffset UnlockedAt)
    {
        public string? UserId { get; init; }
    }
}
