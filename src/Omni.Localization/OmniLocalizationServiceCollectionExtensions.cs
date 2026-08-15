using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace Omni.Localization;

/// <summary>Dependency-injection registration for the framework-independent localization engine.</summary>
public static class OmniLocalizationServiceCollectionExtensions
{
    /// <summary>Adds the generic localization runtime.</summary>
    public static IServiceCollection AddOmniLocalization(
        this IServiceCollection services,
        Action<OmniLocalizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        OmniLocalizationOptions options = GetOrAddOptions(services);
        configure?.Invoke(options);
        ValidateOptions(options);

        services.TryAddScoped(typeof(IOmniLocalizer<>), typeof(OmniLocalizer<>));
        GetOrAddRegistrationState(services);
        return services;
    }

    /// <summary>Overrides diagnostics for one resource without affecting other consumers.</summary>
    public static IServiceCollection ConfigureOmniLocalization<TResource>(
        this IServiceCollection services,
        Action<OmniLocalizationResourceOptions<TResource>> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOmniLocalization();
        var options = new OmniLocalizationResourceOptions<TResource>();
        configure(options);
        if (options.MaximumTrackedMissingTranslations is < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaximumTrackedMissingTranslations));
        if (options.MissingTranslationBehavior is { } behavior && !Enum.IsDefined(behavior))
            throw new ArgumentOutOfRangeException(nameof(options.MissingTranslationBehavior));
        services.AddSingleton(options);
        return services;
    }

    /// <summary>
    /// Adds a typed resource and its reference catalog. The reference catalog is also
    /// registered as the lowest-priority translation source for its culture.
    /// </summary>
    public static IServiceCollection AddOmniLocalizationResource<TResource>(
        this IServiceCollection services,
        string defaultCulture,
        string referenceCulture,
        IEnumerable<KeyValuePair<string, string>> referenceTranslations)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        if (services.Any(static descriptor =>
            descriptor.ServiceType == typeof(OmniLocalizationResource<TResource>)))
            throw new InvalidOperationException(
                $"Localization resource '{typeof(TResource).FullName}' is already registered.");

        var resource = new OmniLocalizationResource<TResource>(
            defaultCulture,
            referenceCulture,
            referenceTranslations);
        services.AddSingleton(resource);
        services.AddSingleton(new OmniTranslationCatalog<TResource>(
            resource.ReferenceCulture.Name,
            resource.ReferenceTranslations,
            priority: -10_000));
        services.AddSingleton<IOmniTranslationProvider<TResource>>(static provider =>
            provider.GetRequiredService<OmniTranslationCatalog<TResource>>());
        services.AddSingleton<OmniTranslationCatalogValidator<TResource>>();
        return services;
    }

    /// <summary>Adds an immutable exact-culture catalog for a typed resource.</summary>
    public static IServiceCollection AddOmniTranslations<TResource>(
        this IServiceCollection services,
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations,
        int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.AddSingleton<IOmniTranslationProvider<TResource>>(
            new OmniTranslationCatalog<TResource>(cultureName, translations, priority));
        return services;
    }

    /// <summary>
    /// Adds an ordered typed base resource. It is consulted only after the resource's
    /// requested, parent and default culture chains miss.
    /// </summary>
    public static IServiceCollection AddOmniBaseResource<TResource, TBaseResource>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        GetOrAddRegistrationState(services).AddBaseResource(
            typeof(TResource), typeof(TBaseResource));
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IOmniInheritedResource<TResource>,
                OmniInheritedResource<TResource, TBaseResource>>());
        return services;
    }

    /// <summary>Adds a custom scoped translation provider for one typed resource.</summary>
    public static IServiceCollection AddOmniTranslationProvider<TResource,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(
        this IServiceCollection services)
        where TProvider : class, IOmniTranslationProvider<TResource>
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IOmniTranslationProvider<TResource>, TProvider>());
        return services;
    }

    /// <summary>Uses a standard .NET localizer as a translation source.</summary>
    public static IServiceCollection AddOmniStringLocalizer<TResource, TStringResource>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IOmniTranslationProvider<TResource>,
                StringLocalizerOmniTranslationProvider<TResource, TStringResource>>());
        return services;
    }

    /// <summary>
    /// Exposes an Omni resource through the standard <see cref="IStringLocalizer{T}"/> contract.
    /// Do not combine this with an Omni provider that consumes the same marker's standard localizer.
    /// </summary>
    public static IServiceCollection AddOmniStringLocalizerAdapter<TResource>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.TryAddScoped<IStringLocalizer<TResource>, OmniStringLocalizerAdapter<TResource>>();
        return services;
    }

    /// <summary>Adds expanded LTR and RTL pseudolocales for one typed resource.</summary>
    public static IServiceCollection AddOmniPseudoLocalization<TResource>(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IOmniTranslationProvider<TResource>,
                OmniPseudoTranslationProvider<TResource>>());
        return services;
    }

    /// <summary>Adds a custom plural rule shared by resources.</summary>
    public static IServiceCollection AddOmniPluralRule<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TRule>(
        this IServiceCollection services)
        where TRule : class, IOmniPluralRule
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOmniLocalization();
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IOmniPluralRule, TRule>());
        return services;
    }

    private static OmniLocalizationOptions GetOrAddOptions(IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(OmniLocalizationOptions) &&
                descriptor.ImplementationInstance is OmniLocalizationOptions existing)
                return existing;
        }

        var created = new OmniLocalizationOptions();
        services.AddSingleton(created);
        return created;
    }

    private static OmniLocalizationRegistrationState GetOrAddRegistrationState(
        IServiceCollection services)
    {
        foreach (ServiceDescriptor descriptor in services)
        {
            if (descriptor.ServiceType == typeof(OmniLocalizationRegistrationState) &&
                descriptor.ImplementationInstance is OmniLocalizationRegistrationState existing)
                return existing;
        }
        var created = new OmniLocalizationRegistrationState();
        services.AddSingleton(created);
        return created;
    }

    private static void ValidateOptions(OmniLocalizationOptions options)
    {
        if (options.MaximumTrackedMissingTranslations < 0)
            throw new ArgumentOutOfRangeException(nameof(options.MaximumTrackedMissingTranslations));
        if (!Enum.IsDefined(options.MissingTranslationBehavior))
            throw new ArgumentOutOfRangeException(nameof(options.MissingTranslationBehavior));
    }
}
