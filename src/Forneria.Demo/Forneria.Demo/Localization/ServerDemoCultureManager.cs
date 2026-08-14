using Forneria.Demo.Pages.Localization;
using Microsoft.AspNetCore.Components;

namespace Forneria.Demo.Localization;

internal sealed class ServerDemoCultureManager(NavigationManager navigation) : IDemoCultureManager
{
    public ValueTask SetCultureAsync(string cultureName)
    {
        _ = DemoCultures.Get(cultureName);
        string redirectUri = navigation.ToBaseRelativePath(navigation.Uri);
        string target = string.Concat(
            "culture/set?culture=",
            Uri.EscapeDataString(cultureName),
            "&redirectUri=/",
            Uri.EscapeDataString(redirectUri));
        navigation.NavigateTo(target, forceLoad: true);
        return ValueTask.CompletedTask;
    }
}
