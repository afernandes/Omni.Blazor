using FoodService.Components;
using FoodService.Pages.Pages.CardapioFeature;
using FoodService.Pages.Pages.PdvFeature;
using Omni.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Host Server puro — render mode fixo InteractiveServer.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Design system + service registration.
builder.Services.AddOmniComponents();
builder.Services.AddScoped<PdvOrderService>();
builder.Services.AddScoped<CardapioDigitalState>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// SSR fallback for unmatched routes.
app.UseStatusCodePagesWithReExecute("/", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(FoodService.Pages._Imports).Assembly);

app.Run();
