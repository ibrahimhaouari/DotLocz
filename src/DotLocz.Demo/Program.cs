using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DotLocz.Demo;
using System.Globalization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register Localization
builder.Services.AddLocalization(options => options.ResourcesPath = "");

// Change the default culture for demo purposes
var language = "de-DE";
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo(language);
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(language);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
