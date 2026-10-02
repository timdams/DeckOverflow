using DeckOverflow.Web;
using DeckOverflow.Web.Interop;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.Services.AddTransient<StageBridge>();

await builder.Build().RunAsync();
