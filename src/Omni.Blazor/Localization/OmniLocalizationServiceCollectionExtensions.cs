using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Omni.Blazor.Localization;

/// <summary>Dependency-injection helpers for Omni.Blazor localization.</summary>
public static class OmniLocalizationServiceCollectionExtensions
{
    /// <summary>Adds an immutable translation catalog for a culture.</summary>
    public static IServiceCollection AddOmniTranslations(
        this IServiceCollection services,
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IOmniTranslationProvider>(
            new OmniTranslationCatalog(cultureName, translations));
        return services;
    }

    /// <summary>Adds a custom translation provider with a scoped lifetime.</summary>
    public static IServiceCollection AddOmniTranslationProvider<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        this IServiceCollection services)
        where TProvider : class, IOmniTranslationProvider
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOmniTranslationProvider, TProvider>());
        return services;
    }

    /// <summary>
    /// Uses the host's standard <c>IStringLocalizer&lt;TResource&gt;</c> as an
    /// Omni translation source. Call <c>AddLocalization</c> (or the equivalent
    /// PO provider registration) in the host first.
    /// </summary>
    public static IServiceCollection AddOmniStringLocalizer<TResource>(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IOmniTranslationProvider,
                StringLocalizerOmniTranslationProvider<TResource>>());
        return services;
    }

    /// <summary>Adds a custom plural rule with a scoped lifetime.</summary>
    public static IServiceCollection AddOmniPluralRule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRule>(
        this IServiceCollection services)
        where TRule : class, IOmniPluralRule
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOmniPluralRule, TRule>());
        return services;
    }
}
