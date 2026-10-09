using System.Globalization;
using System.Text.Json;
using DeckOverflow.CardHall.Runs;
using DeckOverflow.Web.Progress;
using DeckOverflow.Web.World;

namespace DeckOverflow.Web.Backend;

/// <summary>Eén emmer van het histogram: zoveel spelers scoorden vanaf <paramref name="From"/>.</summary>
public sealed record PunchBucket(int From, int Players);

/// <summary>Eén regel in het klassement van je klas: een bijnaam, nooit een gebruikersnaam.</summary>
public sealed record PunchRow(string Nickname, int Score, bool You);

/// <summary>Hoe de dag ervoor staat: het histogram van iedereen en, als je in een klas zit, je klas.</summary>
public sealed record PunchStandings(IReadOnlyList<PunchBucket> Histogram, IReadOnlyList<PunchRow> Class)
{
    public int Players => Histogram.Sum(b => b.Players);
}

/// <summary>
/// De Prikklok tegenover Supabase (docs/wereld/README.md, Prikklok): een geprikte dagelijkse run insturen
/// en de stand van de dag lezen. Per speler telt de eerste voltooide run die niet afgewezen werd (D-007); de
/// nachtelijke scorecontrole speelt elke run opnieuw af. Faalt altijd stil: zonder netwerk speel je gewoon.
/// Voorlopig alleen de Kaartenhal: de andere afdelingen hebben nog geen dagelijkse run.
/// </summary>
public sealed class PunchClock(SupabaseClient supabase, IProgressStore store)
{
    public const string Department = Departments.CardHall;

    /// <summary>Eén emmer per verdieping.</summary>
    public const int BucketWidth = RunScore.PerFloor;

    public const int ClassTop = 10;

    /// <summary>
    /// Ver boven wat een run kan halen (21 verdiepingen, je HP, beurten onder de 150). Wat hoger is, kan alleen via
    /// de REST-API ingestuurd zijn: het blijft uit het histogram en de klas tot de nachtelijke controle het afwijst.
    /// </summary>
    public const int MaxScore = 10_000;

    /// <summary>De dag van de Prikklok is de UTC-datum, voor iedereen dezelfde.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Een geprikte run die nog niet verstuurd is, insturen. De database aanvaardt alleen vandaag of gisteren
    /// (een run die over middernacht liep); een oudere wacht niet langer.
    /// </summary>
    public async Task SendAsync(PlayerProgress progress)
    {
        if (progress.Punch is not { Sent: false, Commands: { } commands } punch) return;
        if (punch.Date < Today.AddDays(-1)) return;
        try
        {
            await supabase.EnsureSessionAsync();
            using var parsed = JsonDocument.Parse(commands);
            await supabase.InsertAsync("scores", [new ScoreInsert(Department, Day(punch.Date), parsed.RootElement.Clone(), punch.Score)]);
            punch.Sent = true;
            punch.Commands = null;
            await store.SaveAsync(progress);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>De stand van een dag, of leeg zonder verbinding.</summary>
    public async Task<PunchStandings?> StandingsAsync(DateOnly date)
    {
        try
        {
            await supabase.EnsureSessionAsync();
            string day = Day(date);
            var buckets = await supabase.RpcAsync<List<BucketRow>>("score_histogram",
                new { department = Department, seed_date = day, bucket_width = BucketWidth }) ?? [];

            // Wat je mag lezen: jezelf, je klasgenoten en, als docent, je leerlingen. Per speler de eerste run.
            var scores = await supabase.SelectAsync<ScoreRow>("scores",
                $"select=user_id,claimed_score&department=eq.{Department}&seed_date=eq.{day}&verified=neq.rejected&order=created_at,id");
            var first = scores.GroupBy(s => s.UserId).Select(g => g.First()).Where(s => s.ClaimedScore <= MaxScore).ToList();

            IReadOnlyList<PunchRow> board = [];
            if (first.Count > 1)
            {
                string ids = string.Join(",", first.Select(s => s.UserId));
                var names = (await supabase.SelectAsync<NicknameRow>("profiles", $"select=user_id,nickname&user_id=in.({ids})"))
                    .ToDictionary(p => p.UserId, p => p.Nickname);
                var rows = first
                    .Select(s => new PunchRow(names.GetValueOrDefault(s.UserId, "?"), s.ClaimedScore, s.UserId == supabase.UserId))
                    .OrderByDescending(r => r.Score)
                    .ToList();
                // De top van de klas, en jezelf eronder als je er niet in staat
                board = [.. rows.Take(ClassTop), .. rows.Skip(ClassTop).Where(r => r.You)];
            }

            return new PunchStandings([.. buckets.Where(b => b.Bucket is >= 0 and <= MaxScore).Select(b => new PunchBucket(b.Bucket, (int)b.Players))], board);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Console.WriteLine(e.Message);
            return null;
        }
    }

    /// <summary>De emmer van een score.</summary>
    public static int BucketOf(int score) => score / BucketWidth * BucketWidth;

    /// <summary>
    /// Elke emmer van de laagste tot de hoogste, ook de lege ertussen, zodat de afstand klopt. Nooit voorbij
    /// <see cref="MaxScore"/>, hoe de emmers ook binnenkomen.
    /// </summary>
    public static IReadOnlyList<PunchBucket> Bars(IReadOnlyList<PunchBucket> histogram)
    {
        var counts = histogram.Where(b => b.From is >= 0 and <= MaxScore)
            .GroupBy(b => BucketOf(b.From)).ToDictionary(g => g.Key, g => g.Sum(b => b.Players));
        if (counts.Count == 0) return [];
        int low = counts.Keys.Min(), high = counts.Keys.Max();
        return [.. Enumerable.Range(0, (high - low) / BucketWidth + 1)
            .Select(i => low + i * BucketWidth)
            .Select(from => new PunchBucket(from, counts.GetValueOrDefault(from)))];
    }

    private static string Day(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private sealed record ScoreInsert(string Department, string SeedDate, JsonElement Commands, int ClaimedScore);
    private sealed record ScoreRow(string UserId, int ClaimedScore);
    private sealed record NicknameRow(string UserId, string Nickname);
    private sealed record BucketRow(int Bucket, long Players);
}
