using Omni.Templates.Host.Components;
using Omni.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Server host — fixed InteractiveServer render mode (no InteractiveAuto/WASM),
// matching Forneria.Demo's choice to dodge the .NET 10 WASM hot-reload crash.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registers every Omni service (ThemeService, DialogService, etc.).
builder.Services.AddOmniComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// SSR fallback for unmatched routes → render the NotFound page.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Pulls the RCL's @page template components into routing.
    .AddAdditionalAssemblies(typeof(Omni.Templates._Imports).Assembly);

app.Run();
