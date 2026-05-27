using Forneria.Demo.Components;
using Forneria.Demo.Pages.Pages.PdvFeature;
using Forneria.Demo.Pages.Services;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Omni.Blazor;

var builder = WebApplication.CreateBuilder(args);

// Host Server puro — render mode fixo InteractiveServer (sem InteractiveAuto).
// Evita o crash do Hot Reload agent do WebAssembly (.NET 10) ao recarregar.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOmniComponents();
builder.Services.AddScoped<FakeOrderService>();
builder.Services.AddScoped<PdvOrderService>();

// HttpClient for server-side prerender of interactive components that may
// hit our own API.
builder.Services.AddHttpClient("self", (sp, c) =>
{
    var server = sp.GetRequiredService<IServer>();
    var addr = server.Features.Get<IServerAddressesFeature>()?.Addresses.FirstOrDefault()
               ?? "https://localhost:7290";
    c.BaseAddress = new Uri(addr);
});
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("self"));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

// SSR fallback: when no route matches, ASP.NET returns 404 with empty body.
// Re-execute the pipeline at /not-found so the user sees our NotFound page.
// On the interactive side, <Router NotFoundPage=…> handles it client-side.
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

// ---- Demo upload — serve saved files at /uploads/* ------------------------
var uploadDir = Path.Combine(app.Environment.WebRootPath, "uploads");
Directory.CreateDirectory(uploadDir);
app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadDir),
    RequestPath = "/uploads"
});

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(Forneria.Demo.Pages._Imports).Assembly);

// Multipart receiver: saves to wwwroot/uploads/, returns metadata.
// Demo only — production must authenticate and reinstate antiforgery tokens.
app.MapPost("/api/uploads", async (HttpRequest req) =>
{
    if (!req.HasFormContentType) return Results.BadRequest("multipart/form-data esperado.");
    var form = await req.ReadFormAsync();
    var saved = new List<object>();
    foreach (var file in form.Files)
    {
        if (file.Length == 0) continue;
        if (file.Length > 10L * 1024 * 1024) return Results.BadRequest($"\"{file.FileName}\" excede 10 MB.");
        var safeName = Path.GetFileNameWithoutExtension(file.FileName);
        safeName = string.Concat(safeName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrEmpty(safeName)) safeName = "file";
        var ext = Path.GetExtension(file.FileName);
        var unique = $"{safeName}-{Guid.NewGuid():N}{ext}".ToLowerInvariant();
        var fullPath = Path.Combine(uploadDir, unique);
        await using (var fs = File.Create(fullPath))
        {
            await file.CopyToAsync(fs);
        }
        saved.Add(new
        {
            name = file.FileName,
            size = file.Length,
            contentType = file.ContentType,
            url = $"/uploads/{unique}"
        });
    }
    return Results.Ok(saved);
}).DisableAntiforgery();   // demo only — production should authenticate / token

app.Run();
