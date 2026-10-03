using DeckOverflow.Web;
using DeckOverflow.Web.Art;
using DeckOverflow.Web.Interop;
using DeckOverflow.Web.Progress;
using DeckOverflow.Web.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Strings>();
builder.Services.AddSingleton<ArtStyle>();
builder.Services.AddTransient<StageBridge>();
builder.Services.AddScoped<IProgressStore, LocalProgressStore>();

await builder.Build().RunAsync();
