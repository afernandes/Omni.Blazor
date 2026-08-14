using Forneria.Demo.Pages.Localization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Forneria.Demo.Wasm.Localization;

internal sealed class WasmDemoCultureManager(IJSRuntime jsRuntime, NavigationManager navigation) : IDemoCultureManager
{
    private const string ModulePath = "./_content/Forneria.Demo.Pages/js/culture.js";

    public async ValueTask SetCultureAsync(string cultureName)
    {
        _ = DemoCultures.Get(cultureName);
        await using IJSObjectReference module =
            await jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        await module.InvokeVoidAsync("setCulture", cultureName);
        navigation.NavigateTo(navigation.Uri, forceLoad: true);
    }
}
