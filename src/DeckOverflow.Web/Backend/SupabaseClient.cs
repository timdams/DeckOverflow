using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace DeckOverflow.Web.Backend;

/// <summary>Een fout van Supabase, met de code die Supabase meegeeft (bv. <c>invalid_credentials</c>).</summary>
public sealed class SupabaseException(HttpStatusCode status, string? errorCode, string message)
    : HttpRequestException(message, null, status)
{
    public string? ErrorCode { get; } = errorCode;
}

/// <summary>
/// Dunne client op de REST- en Auth-API van Supabase, met <see cref="HttpClient"/> en zonder
/// library. De sessie staat in <c>localStorage</c>. Elke fout komt als exception terug; wie
/// belt, beslist dat het spel gewoon doorspeelt.
/// </summary>
public sealed class SupabaseClient(HttpClient http, IJSRuntime js, string publishableKey)
{
    private const string SessionKey = "deckoverflow.session";

    /// <summary>Zo lang voor het verloopt, vernieuwen we de sessie al.</summary>
    private const int RefreshMarginSeconds = 60;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    private Session? _session;
    private bool _sessionLoaded;

    public string? UserId => _session?.User.Id;

    /// <summary>Een gast: aangemeld zonder adres en wachtwoord. Ook waar zonder sessie.</summary>
    public bool IsGuest => _session is null || _session.User.IsAnonymous;

    /// <summary>Het auth-adres: een echt e-mailadres of <c>naam@users.deckoverflow.invalid</c>.</summary>
    public string? Email => _session?.User.Email;

    /// <summary>
    /// Zorgt voor een geldige sessie: de bewaarde, vernieuwd als ze bijna verloopt, of anders
    /// een nieuw gastaccount. Nooit een scherm ervoor.
    /// </summary>
    public async Task EnsureSessionAsync()
    {
        await LoadOnceAsync();
        if (_session is not null && _session.ExpiresAt - RefreshMarginSeconds > DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return;
        if (_session is not null && await RefreshAsync()) return;
        await SignInAnonymouslyAsync();
    }

    /// <summary>
    /// Inloggen met adres en wachtwoord. Het gastaccount van dit toestel is daarna overbodig: de
    /// lokale voortgang gaat mee naar je fabriek. We ruimen het op, zo goed als het lukt.
    /// </summary>
    public async Task SignInWithPasswordAsync(string email, string password)
    {
        await LoadOnceAsync();
        string? leftoverGuest = _session is { User.IsAnonymous: true } ? _session.AccessToken : null;

        using var response = await http.SendAsync(Request(HttpMethod.Post, "auth/v1/token?grant_type=password",
            new { email, password }, withSession: false));
        await ThrowIfFailedAsync(response);
        await StoreSessionAsync(await response.Content.ReadFromJsonAsync<Session>(Json));

        if (leftoverGuest is null) return;
        try
        {
            var request = Request(HttpMethod.Post, "rest/v1/rpc/delete_my_account", new { }, withSession: false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", leftoverGuest);
            using var _ = await http.SendAsync(request);
        }
        catch (HttpRequestException) { /* een achtergebleven gast schaadt niemand */ }
    }

    /// <summary>
    /// Een gast wordt een account: adres en wachtwoord op de anonieme sessie. De <c>user_id</c>
    /// blijft, en dus alle voortgang. Daarna een nieuwe sessie, want de oude zegt nog "anoniem".
    /// </summary>
    public async Task ConvertGuestAsync(string email, string password)
    {
        await EnsureSessionAsync();
        using var response = await http.SendAsync(Request(HttpMethod.Put, "auth/v1/user", new { email, password }));
        await ThrowIfFailedAsync(response);
        if (!await RefreshAsync()) throw new SupabaseException(HttpStatusCode.Unauthorized, "session_lost", "Sessie kwijt na het omzetten.");
    }

    /// <summary>Afmelden en de sessie vergeten. Lukt het afmelden bij Supabase niet, dan vergeten we ze toch.</summary>
    public async Task SignOutAsync()
    {
        await LoadOnceAsync();
        if (_session is not null)
        {
            try
            {
                using var response = await http.SendAsync(Request(HttpMethod.Post, "auth/v1/logout"));
            }
            catch (HttpRequestException) { /* zonder netwerk: lokaal vergeten volstaat */ }
        }
        await ForgetSessionAsync();
    }

    /// <summary>Vergeet de sessie op dit toestel, zonder Supabase te vragen (na het verwijderen van het account).</summary>
    public async Task ForgetSessionAsync()
    {
        _session = null;
        _sessionLoaded = true;
        await js.InvokeVoidAsync("deckOverflow.save", SessionKey, "");
    }

    /// <summary>Rijen lezen, bv. <c>SelectAsync&lt;Row&gt;("progress", "select=item,state&amp;user_id=eq.…")</c>.</summary>
    public async Task<List<T>> SelectAsync<T>(string table, string query)
    {
        using var response = await http.SendAsync(Request(HttpMethod.Get, $"rest/v1/{table}?{query}"));
        await ThrowIfFailedAsync(response);
        return await response.Content.ReadFromJsonAsync<List<T>>(Json) ?? [];
    }

    /// <summary>
    /// Rijen schrijven. Bestaat de rij al: bijwerken, of met <paramref name="ignoreDuplicates"/>
    /// laten staan (voor tabellen waar je niet mag bijwerken, zoals <c>unlocks</c>).
    /// </summary>
    public async Task UpsertAsync<T>(string table, IReadOnlyList<T> rows, bool ignoreDuplicates = false)
    {
        if (rows.Count == 0) return;
        var request = Request(HttpMethod.Post, $"rest/v1/{table}", rows);
        request.Headers.Add("Prefer", $"resolution={(ignoreDuplicates ? "ignore" : "merge")}-duplicates,return=minimal");
        using var response = await http.SendAsync(request);
        await ThrowIfFailedAsync(response);
    }

    /// <summary>Rijen toevoegen zonder ze terug te lezen, voor tabellen waar je alleen mag schrijven (<c>feedback</c>).</summary>
    public async Task InsertAsync<T>(string table, IReadOnlyList<T> rows)
    {
        if (rows.Count == 0) return;
        var request = Request(HttpMethod.Post, $"rest/v1/{table}", rows);
        request.Headers.Add("Prefer", "return=minimal");
        using var response = await http.SendAsync(request);
        await ThrowIfFailedAsync(response);
    }

    /// <summary>Een databasefunctie aanroepen, bv. <c>join_class</c>.</summary>
    public async Task<T?> RpcAsync<T>(string function, object args)
    {
        using var response = await http.SendAsync(Request(HttpMethod.Post, $"rest/v1/rpc/{function}", args));
        await ThrowIfFailedAsync(response);
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    public async Task RpcAsync(string function, object args)
    {
        using var response = await http.SendAsync(Request(HttpMethod.Post, $"rest/v1/rpc/{function}", args));
        await ThrowIfFailedAsync(response);
    }

    private async Task SignInAnonymouslyAsync()
    {
        using var response = await http.SendAsync(Request(HttpMethod.Post, "auth/v1/signup", new { data = new { } }, withSession: false));
        await ThrowIfFailedAsync(response);
        await StoreSessionAsync(await response.Content.ReadFromJsonAsync<Session>(Json));
    }

    /// <summary>
    /// Vernieuwt de sessie. Onwaar als Supabase de sessie niet meer kent: dan begint de speler
    /// opnieuw als gast, de lokale voortgang gaat mee naar dat account. Zonder netwerk een exception,
    /// zodat een haperende verbinding nooit een nieuw account maakt.
    /// </summary>
    private async Task<bool> RefreshAsync()
    {
        using var response = await http.SendAsync(Request(HttpMethod.Post, "auth/v1/token?grant_type=refresh_token",
            new { refresh_token = _session!.RefreshToken }, withSession: false));
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            _session = null;
            return false;
        }
        await ThrowIfFailedAsync(response);
        await StoreSessionAsync(await response.Content.ReadFromJsonAsync<Session>(Json));
        return true;
    }

    private HttpRequestMessage Request(HttpMethod method, string path, object? body = null, bool withSession = true)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add("apikey", publishableKey);
        if (withSession && _session is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.AccessToken);
        if (body is not null) request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        return request;
    }

    /// <summary>Auth geeft <c>error_code</c> en <c>msg</c>, de REST-API <c>code</c> en <c>message</c>.</summary>
    private static async Task ThrowIfFailedAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        string detail = await response.Content.ReadAsStringAsync();
        string? code = null;
        try
        {
            using var doc = JsonDocument.Parse(detail);
            var root = doc.RootElement;
            if (root.TryGetProperty("error_code", out var e) && e.ValueKind == JsonValueKind.String) code = e.GetString();
            else if (root.TryGetProperty("code", out var c) && c.ValueKind == JsonValueKind.String) code = c.GetString();
        }
        catch (JsonException) { }
        throw new SupabaseException(response.StatusCode, code, $"Supabase {(int)response.StatusCode}: {detail}");
    }

    private async Task LoadOnceAsync()
    {
        if (_sessionLoaded) return;
        _session = await LoadSessionAsync();
        _sessionLoaded = true;
    }

    private async Task StoreSessionAsync(Session? session)
    {
        _session = session ?? throw new HttpRequestException("Supabase gaf geen sessie terug.");
        _sessionLoaded = true;
        await js.InvokeVoidAsync("deckOverflow.save", SessionKey, JsonSerializer.Serialize(session, Json));
    }

    private async Task<Session?> LoadSessionAsync()
    {
        string? json = await js.InvokeAsync<string?>("deckOverflow.load", SessionKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonSerializer.Deserialize<Session>(json, Json); }
        catch (JsonException) { return null; }
    }

    private sealed record Session(string AccessToken, string RefreshToken, long ExpiresAt, SessionUser User);

    private sealed record SessionUser(string Id, bool IsAnonymous, string? Email);
}
