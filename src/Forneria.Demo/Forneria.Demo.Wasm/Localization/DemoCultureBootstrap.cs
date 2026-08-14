using System.Globalization;
using Forneria.Demo.Pages.Localization;
using Microsoft.JSInterop;

namespace Forneria.Demo.Wasm.Localization;

internal static class DemoCultureBootstrap
{
    private const string ModulePath = "./_content/Forneria.Demo.Pages/js/culture.js";

    public static async ValueTask RestoreAsync(IJSRuntime jsRuntime)
    {
        await using IJSObjectReference module =
            await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        string? stored = await module.InvokeAsync<string?>("getCulture");
        string cultureName = DemoCultures.IsSupported(stored) ? stored! : "pt-BR";
        CultureInfo culture = DemoCultures.Get(cultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        await module.InvokeVoidAsync("applyDocumentCulture", cultureName, culture.TextInfo.IsRightToLeft);
    }
}
