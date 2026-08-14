using Forneria.Demo.Pages.Localization;
using Forneria.Demo.Pages.Pages.PdvFeature;
using Forneria.Demo.Pages.Services;
using Forneria.Demo.Wasm;
using Forneria.Demo.Wasm.Localization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Omni.Blazor;
using Omni.Blazor.Localization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddLocalization();
builder.Services.AddOmniPseudoLocalization();
builder.Services.AddDemoOmniTranslations();
builder.Services.AddOmniComponents();
builder.Services.AddScoped<IDemoCultureManager, WasmDemoCultureManager>();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<FakeOrderService>();
builder.Services.AddScoped<PdvOrderService>();

WebAssemblyHost host = builder.Build();
await DemoCultureBootstrap.RestoreAsync(host.Services.GetRequiredService<Microsoft.JSInterop.IJSRuntime>());
await host.RunAsync();
