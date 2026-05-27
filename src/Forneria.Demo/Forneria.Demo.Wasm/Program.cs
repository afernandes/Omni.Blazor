using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Omni.Blazor;
using Forneria.Demo.Pages.Pages.PdvFeature;
using Forneria.Demo.Pages.Services;
using Forneria.Demo.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddOmniComponents();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<FakeOrderService>();
builder.Services.AddScoped<PdvOrderService>();

await builder.Build().RunAsync();
