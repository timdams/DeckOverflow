using DeckOverflow.Web;
using DeckOverflow.Web.Interop;
using DeckOverflow.Web.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<Strings>();
builder.Services.AddTransient<StageBridge>();

await builder.Build().RunAsync();
