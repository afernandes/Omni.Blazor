using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Omni.Localization;

namespace Omni.Blazor.Localization;

/// <summary>Convenience registrations that target the built-in Omni.Blazor resource.</summary>
public static class OmniLocalizationServiceCollectionExtensions
{
    /// <summary>Adds an application-owned catalog that overrides Omni.Blazor UI strings.</summary>
    public static IServiceCollection AddOmniTranslations(
        this IServiceCollection services,
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations,
        int priority = 0)
        => Omni.Localization.OmniLocalizationServiceCollectionExtensions
            .AddOmniTranslations<OmniBlazorResource>(services, cultureName, translations, priority);

    /// <summary>Adds a custom provider for the built-in Omni.Blazor resource.</summary>
    public static IServiceCollection AddOmniTranslationProvider<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        this IServiceCollection services)
        where TProvider : class, IOmniTranslationProvider<OmniBlazorResource>
        => Omni.Localization.OmniLocalizationServiceCollectionExtensions
            .AddOmniTranslationProvider<OmniBlazorResource, TProvider>(services);

    /// <summary>Uses the host's standard localizer as a source for Omni.Blazor UI strings.</summary>
    public static IServiceCollection AddOmniStringLocalizer<TStringResource>(
        this IServiceCollection services)
        => Omni.Localization.OmniLocalizationServiceCollectionExtensions
            .AddOmniStringLocalizer<OmniBlazorResource, TStringResource>(services);

    /// <summary>Adds expanded LTR and RTL pseudolocales for Omni.Blazor.</summary>
    public static IServiceCollection AddOmniPseudoLocalization(this IServiceCollection services)
        => Omni.Localization.OmniLocalizationServiceCollectionExtensions
            .AddOmniPseudoLocalization<OmniBlazorResource>(services);

    /// <summary>Adds a custom plural rule shared by resources.</summary>
    public static IServiceCollection AddOmniPluralRule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRule>(
        this IServiceCollection services)
        where TRule : class, IOmniPluralRule
        => Omni.Localization.OmniLocalizationServiceCollectionExtensions
            .AddOmniPluralRule<TRule>(services);

    internal static IServiceCollection AddOmniBlazorLocalization(
        this IServiceCollection services,
        OmniLocalizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        services.AddOmniLocalization();
        services.ConfigureOmniLocalization<OmniBlazorResource>(configured =>
        {
            configured.MissingTranslationBehavior = options.MissingTranslationBehavior;
            configured.MaximumTrackedMissingTranslations = options.MaximumTrackedMissingTranslations;
        });

        if (!services.Any(static descriptor =>
            descriptor.ServiceType == typeof(OmniLocalizationResource<OmniBlazorResource>)))
        {
            OmniLocalizationResource<OmniBlazorResource> resource = OmniBlazorResources.Definition;
            services.AddOmniLocalizationResource<OmniBlazorResource>(
                resource.DefaultCulture.Name,
                resource.ReferenceCulture.Name,
                resource.ReferenceTranslations);
        }

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOmniTranslationProvider<OmniBlazorResource>,
                OmniBlazorEmbeddedTranslationProvider>());
        return services;
    }
}
