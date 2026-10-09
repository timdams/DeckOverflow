using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using DeckOverflow.CardHall.Runs;

// De nachtelijke scorecontrole van de Prikklok (docs/wereld/supabase.md, stap 7). Haalt de scores op die nog
// pending zijn, speelt elke run opnieuw af met de echte motor (DailyRun.Check) en zet ok of rejected. Zo
// vertrouwt het klassement de browser niet. Draait elke nacht als GitHub Action, met de service-sleutel als
// secret; die sleutel staat nooit in de repo. Houdt meteen het gratis Supabase-project wakker.
//
//   SUPABASE_URL=https://….supabase.co SUPABASE_SERVICE_KEY=… dotnet run --project tools/DeckOverflow.ScoreCheck

const string CardHall = "card-hall";
const int Page = 100;

string? url = Environment.GetEnvironmentVariable("SUPABASE_URL");
string? key = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_KEY");
if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
{
    Console.Error.WriteLine("SUPABASE_URL en SUPABASE_SERVICE_KEY ontbreken.");
    return 1;
}

using var http = new HttpClient { BaseAddress = new Uri(url.TrimEnd('/') + "/rest/v1/") };
http.DefaultRequestHeaders.Add("apikey", key);
// Een oude service_role-sleutel is een JWT en moet ook als bearer mee; een nieuwe sb_secret_-sleutel niet
if (key.StartsWith("eyJ", StringComparison.Ordinal)) http.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

var json = new JsonSerializerOptions(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
int ok = 0, rejected = 0, skipped = 0;
long after = 0;

while (true)
{
    var rows = await http.GetFromJsonAsync<List<ScoreRow>>(
        $"scores?select=id,department,seed_date,commands,claimed_score&verified=eq.pending&id=gt.{after}&order=id&limit={Page}", json) ?? [];
    if (rows.Count == 0) break;

    foreach (var row in rows)
    {
        after = row.Id;
        if (row.Department != CardHall)
        {
            // Alleen de Kaartenhal heeft een dagelijkse run; een andere afdeling blijft pending tot ze er een heeft
            skipped++;
            continue;
        }

        var date = DateOnly.ParseExact(row.SeedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        ScoreVerdict verdict;
        try
        {
            verdict = DailyRun.Check(date, row.Commands.GetRawText(), row.ClaimedScore);
        }
        catch (Exception e)
        {
            // Een commandolijst die de motor laat crashen, is een bug in de motor: afwijzen en melden
            Console.Error.WriteLine($"Score {row.Id}: de motor crashte bij het afspelen: {e.Message}");
            verdict = ScoreVerdict.Unreadable;
        }

        string status = verdict == ScoreVerdict.Ok ? "ok" : "rejected";
        using var response = await http.PatchAsJsonAsync($"scores?id=eq.{row.Id}", new { verified = status }, json);
        response.EnsureSuccessStatusCode();

        if (verdict == ScoreVerdict.Ok) ok++;
        else
        {
            rejected++;
            Console.WriteLine($"Score {row.Id} ({row.SeedDate}, {row.ClaimedScore}) afgewezen: {verdict}.");
        }
    }
}

Console.WriteLine($"Nagekeken: {ok} ok, {rejected} afgewezen, {skipped} overgeslagen.");
return 0;

internal sealed record ScoreRow(long Id, string Department, string SeedDate, JsonElement Commands, int ClaimedScore);
