namespace DeckOverflow.Web.Backend;

/// <summary>Hoe het lezen afliep: alles binnen, geen toegang (je account is niet de superuser), of iets anders liep mis (bv. geen verbinding).</summary>
public enum AdminStatus { Loaded, Forbidden, Failed }

public sealed record FeedbackSummary(string Department, string Subject, long Likes, long Dislikes, long Players);

public sealed record FeedbackComment(DateTimeOffset CreatedAt, string Department, string Subject, string? Verdict, string Comment);

public sealed record AdminScore(DateTimeOffset CreatedAt, string? Nickname, string Department, DateOnly SeedDate, int ClaimedScore, string Verified);

public sealed record AdminData(
    AdminStatus Status,
    IReadOnlyList<FeedbackSummary> Feedback,
    IReadOnlyList<FeedbackComment> Comments,
    IReadOnlyList<AdminScore> Scores);

/// <summary>
/// Het beheerpaneel van de maker (D-013): feedback en scores lezen. Wie het mag, beslist de database
/// (<c>app_metadata.role = 'superuser'</c>, zie de migratie <c>admin.sql</c>), niet deze klasse: wie de app
/// patcht, krijgt hooguit een weigering. Lokaal toont de browser de knop, maar data krijg je alleen met je echte account.
/// </summary>
public sealed class Admin(SupabaseClient supabase)
{
    public const int MaxRows = 200;

    /// <summary>Postgres' <c>insufficient_privilege</c>, zoals de functies weigeren.</summary>
    private const string Forbidden = "42501";

    public async Task<AdminData> LoadAsync()
    {
        try
        {
            await supabase.EnsureSessionAsync();
            var feedback = await supabase.RpcAsync<List<FeedbackSummary>>("admin_feedback_summary", new { });
            var comments = await supabase.RpcAsync<List<FeedbackComment>>("admin_feedback_comments", new { max_rows = MaxRows });
            var scores = await supabase.RpcAsync<List<AdminScore>>("admin_scores", new { max_rows = MaxRows });
            return new AdminData(AdminStatus.Loaded, feedback ?? [], comments ?? [], scores ?? []);
        }
        catch (SupabaseException e) when (e.ErrorCode == Forbidden) { return Empty(AdminStatus.Forbidden); }
        catch (SupabaseException e)
        {
            Console.WriteLine(e.Message);
            return Empty(AdminStatus.Failed);
        }
        catch (HttpRequestException) { return Empty(AdminStatus.Failed); }
        catch (TaskCanceledException) { return Empty(AdminStatus.Failed); }
    }

    private static AdminData Empty(AdminStatus status) => new(status, [], [], []);
}
