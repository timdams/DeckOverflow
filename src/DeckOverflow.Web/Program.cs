using DeckOverflow.Web;
using DeckOverflow.Web.Art;
using DeckOverflow.Web.Backend;
using DeckOverflow.Web.Interop;
using DeckOverflow.Web.Progress;
using DeckOverflow.Web.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Strings>();
builder.Services.AddSingleton<ArtStyle>();
builder.Services.AddTransient<StageBridge>();

// Met Supabase in appsettings.json synchroniseert de voortgang; zonder blijft alles in de browser.
string? supabaseUrl = builder.Configuration["Supabase:Url"];
string? supabaseKey = builder.Configuration["Supabase:PublishableKey"];
if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
{
    builder.Services.AddScoped<LocalProgressStore>();
    builder.Services.AddScoped(sp => new SupabaseClient(
        new HttpClient { BaseAddress = new Uri(supabaseUrl.TrimEnd('/') + "/") },
        sp.GetRequiredService<IJSRuntime>(),
        supabaseKey));
    builder.Services.AddScoped<IProgressStore, SyncedProgressStore>();
}
else
{
    builder.Services.AddScoped<IProgressStore, LocalProgressStore>();
}

await builder.Build().RunAsync();
