using Microsoft.Extensions.DependencyInjection;

namespace Omni.Localization.Po;

/// <summary>Registers the optional Orchard Core PO/Gettext pipeline.</summary>
public static class OmniPortableObjectLocalizationExtensions
{
    /// <summary>Uses the same marker for the Omni resource and Orchard resource context.</summary>
    public static IServiceCollection AddOmniPortableObjectLocalization<TResource>(
        this IServiceCollection services,
        string resourcesPath = "Localization")
        => AddOmniPortableObjectLocalization<TResource, TResource>(services, resourcesPath);

    /// <summary>
    /// Adds PO/Gettext localization and maps an Orchard resource marker to a typed Omni resource.
    /// </summary>
    public static IServiceCollection AddOmniPortableObjectLocalization<TResource, TStringResource>(
        this IServiceCollection services,
        string resourcesPath = "Localization")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourcesPath);
        services.AddMemoryCache();
        services.AddPortableObjectLocalization(options => options.ResourcesPath = resourcesPath);
        services.AddOmniStringLocalizer<TResource, TStringResource>();
        return services;
    }
}
