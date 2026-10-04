using System.Text.RegularExpressions;
using DeckOverflow.Web.Progress;

namespace DeckOverflow.Web.Backend;

/// <summary>Wat er misging, als sleutel voor een tekst in <c>en.json</c> (<c>ui.account.error.&lt;naam&gt;</c>).</summary>
public enum AccountError { None, Offline, Taken, WrongLogin, BadName, ShortPassword, UnknownCode, BadClassName, Failed }

/// <summary>Een klas zoals de speler ze ziet. <see cref="Code"/> en <see cref="Players"/> alleen voor de eigenaar.</summary>
public sealed record ClassView(string Id, string Name, bool Owner, string? Code, int Players, IReadOnlyList<string> Released);

/// <summary>
/// Accounts en klassen voor de schermen. Inloggen gebeurt met een gebruikersnaam of een e-mailadres:
/// Supabase kent alleen e-mail, dus een gebruikersnaam wordt <c>naam@users.deckoverflow.invalid</c>.
/// Een gebruikersnaam is nooit zichtbaar voor anderen; die zien alleen de bijnaam.
/// </summary>
public sealed partial class Account(SupabaseClient supabase, IProgressStore store)
{
    public const string UsernameDomain = "users.deckoverflow.invalid";
    public const int MinPasswordLength = 8;

    public bool IsGuest => supabase.IsGuest;

    /// <summary>De gebruikersnaam of het e-mailadres waarmee je inlogt, of leeg voor een gast.</summary>
    public string? Login => supabase.IsGuest || string.IsNullOrEmpty(supabase.Email) ? null
        : supabase.Email.EndsWith("@" + UsernameDomain, StringComparison.Ordinal) ? supabase.Email[..^(UsernameDomain.Length + 1)]
        : supabase.Email;

    /// <summary>
    /// Van wat de speler intypt naar een auth-adres. Met een <c>@</c> een e-mailadres, anders een
    /// gebruikersnaam: kleine letters, cijfers, <c>-</c> en <c>_</c>, 3 tot 20 tekens. Leeg als het niet kan.
    /// </summary>
    public static string? ToAuthAddress(string? login)
    {
        string value = (login ?? "").Trim().ToLowerInvariant();
        if (value.Contains('@')) return EmailPattern().IsMatch(value) && !value.EndsWith("@" + UsernameDomain, StringComparison.Ordinal) ? value : null;
        return UsernamePattern().IsMatch(value) ? $"{value}@{UsernameDomain}" : null;
    }

    public static bool HasEmail(string? login) => (login ?? "").Contains('@');

    /// <summary>De bijnaam die anderen zien. Leeg zonder verbinding.</summary>
    public async Task<string?> NicknameAsync()
    {
        try
        {
            await supabase.EnsureSessionAsync();
            var rows = await supabase.SelectAsync<NicknameRow>("profiles", $"select=nickname&user_id=eq.{supabase.UserId}");
            return rows.FirstOrDefault()?.Nickname;
        }
        catch (HttpRequestException) { return null; }
    }

    /// <summary>"Bewaar je fabriek": de gast krijgt een naam en een wachtwoord, en houdt alles.</summary>
    public Task<AccountError> KeepFactoryAsync(string login, string password) => RunAsync(login, password, async address =>
    {
        await supabase.ConvertGuestAsync(address, password);
    });

    /// <summary>Inloggen op dit toestel. Wat je hier als gast al deed, gaat mee naar je fabriek.</summary>
    public Task<AccountError> LogInAsync(string login, string password) => RunAsync(login, password, async address =>
    {
        await supabase.SignInWithPasswordAsync(address, password);
        await store.RefreshAsync();
    });

    /// <summary>Afmelden en alles op dit toestel vergeten. Daarna begint hier een nieuwe gast.</summary>
    public async Task LogOutAsync()
    {
        await supabase.SignOutAsync();
        await store.ForgetAsync();
    }

    /// <summary>Het account en alles wat eraan hangt verwijderen, ook op dit toestel.</summary>
    public Task<AccountError> DeleteAsync() => TryAsync(async () =>
    {
        await supabase.EnsureSessionAsync();
        await supabase.RpcAsync("delete_my_account", new { });
        await supabase.ForgetSessionAsync();
        await store.ForgetAsync();
    });

    /// <summary>
    /// Feedback voor de maker: een hartje of niet (<paramref name="liked"/>), en/of een kort bericht.
    /// Alleen schrijven: niemand leest het via de API, ook de speler zelf niet.
    /// </summary>
    public Task<AccountError> SendFeedbackAsync(string department, string subject, bool? liked, string? comment) => TryAsync(async () =>
    {
        await supabase.EnsureSessionAsync();
        string? text = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim()[..Math.Min(comment.Trim().Length, MaxFeedbackLength)];
        await supabase.InsertAsync("feedback", [new FeedbackRow(department, subject, liked is null ? null : liked.Value ? "like" : "dislike", text)]);
    });

    public const int MaxFeedbackLength = 500;

    public Task<AccountError> JoinClassAsync(string code) => TryAsync(async () =>
    {
        await supabase.EnsureSessionAsync();
        await supabase.RpcAsync<string>("join_class", new { class_code = code });
        await store.RefreshAsync();
    });

    /// <summary>Een klas maken; alleen met een account. Geeft de code terug.</summary>
    public async Task<(AccountError Error, string? Code)> CreateClassAsync(string name)
    {
        string? code = null;
        var error = (name ?? "").Trim().Length is < 1 or > 60 ? AccountError.BadClassName : await TryAsync(async () =>
        {
            await supabase.EnsureSessionAsync();
            code = await supabase.RpcAsync<string>("create_class", new { class_name = name });
        });
        return (error, code);
    }

    public Task<AccountError> ReleaseAsync(string classId, string department) => TryAsync(async () =>
    {
        await supabase.EnsureSessionAsync();
        await supabase.RpcAsync("release_department", new { @class = classId, department });
    });

    /// <summary>Je klassen: waar je eigenaar van bent, en waar je lid van bent.</summary>
    public async Task<IReadOnlyList<ClassView>> ClassesAsync()
    {
        try
        {
            await supabase.EnsureSessionAsync();
            string me = supabase.UserId!;
            var owned = await supabase.SelectAsync<OwnedRow>("classes", $"select=id,code,name,released_departments,class_members(count)&owner_id=eq.{me}&order=created_at");
            var joined = await supabase.SelectAsync<MembershipRow>("class_members", $"select=classes(id,name,released_departments)&user_id=eq.{me}");
            return
            [
                .. owned.Select(c => new ClassView(c.Id, c.Name, true, c.Code, c.ClassMembers.FirstOrDefault()?.Count ?? 0, c.ReleasedDepartments)),
                .. joined.Where(m => m.Classes is not null && owned.All(o => o.Id != m.Classes.Id))
                    .Select(m => new ClassView(m.Classes!.Id, m.Classes.Name, false, null, 0, m.Classes.ReleasedDepartments)),
            ];
        }
        catch (HttpRequestException) { return []; }
    }

    private async Task<AccountError> RunAsync(string login, string password, Func<string, Task> action)
    {
        if (ToAuthAddress(login) is not { } address) return AccountError.BadName;
        if ((password ?? "").Length < MinPasswordLength) return AccountError.ShortPassword;
        return await TryAsync(() => action(address));
    }

    private static async Task<AccountError> TryAsync(Func<Task> action)
    {
        try
        {
            await action();
            return AccountError.None;
        }
        catch (SupabaseException e)
        {
            Console.WriteLine(e.Message);
            return e.ErrorCode switch
            {
                "user_already_exists" or "email_exists" or "conflict" => AccountError.Taken,
                "invalid_credentials" => AccountError.WrongLogin,
                "weak_password" => AccountError.ShortPassword,
                "email_address_invalid" or "validation_failed" => AccountError.BadName,
                "P0002" => AccountError.UnknownCode,
                "22023" => AccountError.BadClassName,
                _ => AccountError.Failed,
            };
        }
        catch (HttpRequestException) { return AccountError.Offline; }
        catch (TaskCanceledException) { return AccountError.Offline; }
    }

    [GeneratedRegex("^[a-z0-9_-]{3,20}$")]
    private static partial Regex UsernamePattern();

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    private sealed record NicknameRow(string Nickname);
    private sealed record CountRow(int Count);
    private sealed record OwnedRow(string Id, string Code, string Name, IReadOnlyList<string> ReleasedDepartments, IReadOnlyList<CountRow> ClassMembers);
    private sealed record MembershipRow(ClassRow? Classes);
    private sealed record ClassRow(string Id, string Name, IReadOnlyList<string> ReleasedDepartments);

    private sealed record FeedbackRow(string Department, string Subject, string? Verdict, string? Comment);
}
