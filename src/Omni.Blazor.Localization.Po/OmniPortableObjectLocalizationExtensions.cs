using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Localization.Po;

/// <summary>Registers the optional Orchard Core PO/Gettext localization pipeline.</summary>
public static class OmniPortableObjectLocalizationExtensions
{
    /// <summary>
    /// Adds PO/Gettext localization and exposes the host's resource marker to
    /// Omni.Blazor. PO files are read from <paramref name="resourcesPath"/>.
    /// </summary>
    public static IServiceCollection AddOmniPortableObjectLocalization<TResource>(
        this IServiceCollection services,
        string resourcesPath = "Localization")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcesPath);
        services.AddMemoryCache();
        services.AddPortableObjectLocalization(options => options.ResourcesPath = resourcesPath);
        services.AddOmniStringLocalizer<TResource>();
        return services;
    }
}
