using System.Collections.Frozen;
using System.Globalization;

namespace Omni.Localization;

/// <summary>Immutable metadata and reference catalog for one localization resource.</summary>
/// <typeparam name="TResource">Marker type that owns the resource.</typeparam>
public sealed class OmniLocalizationResource<TResource>
{
    /// <summary>Creates a typed resource definition.</summary>
    public OmniLocalizationResource(
        string defaultCulture,
        string referenceCulture,
        IEnumerable<KeyValuePair<string, string>> referenceTranslations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultCulture);
        ArgumentException.ThrowIfNullOrWhiteSpace(referenceCulture);
        ArgumentNullException.ThrowIfNull(referenceTranslations);

        DefaultCulture = CultureInfo.GetCultureInfo(defaultCulture);
        ReferenceCulture = CultureInfo.GetCultureInfo(referenceCulture);
        KeyValuePair<string, string>[] snapshot = referenceTranslations.ToArray();
        OmniTranslationCatalogValidationResult validation =
            OmniTranslationCatalogValidator.ValidateBasic(ReferenceCulture.Name, snapshot);
        if (!validation.IsValid)
            throw new ArgumentException(OmniTranslationCatalogValidator.FormatErrors(validation), nameof(referenceTranslations));

        ReferenceTranslations = snapshot.ToFrozenDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.Ordinal);
    }

    /// <summary>Culture used after the requested culture and its parents miss.</summary>
    public CultureInfo DefaultCulture { get; }

    /// <summary>Culture used to validate placeholders and generate pseudolocales.</summary>
    public CultureInfo ReferenceCulture { get; }

    /// <summary>Authoritative reference catalog.</summary>
    public FrozenDictionary<string, string> ReferenceTranslations { get; }
}

internal interface IOmniLocalizationResolver<TResource>
{
    bool TryResolveWithoutDiagnostics(
        string key,
        CultureInfo culture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture);
}

internal interface IOmniInheritedResource<TResource>
{
    bool TryResolve(
        string key,
        CultureInfo culture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture);
}

internal sealed class OmniInheritedResource<TResource, TBaseResource>
    : IOmniInheritedResource<TResource>
{
    private readonly IOmniLocalizationResolver<TBaseResource> _resolver;

    public OmniInheritedResource(IOmniLocalizer<TBaseResource> localizer)
        => _resolver = localizer as IOmniLocalizationResolver<TBaseResource>
            ?? throw new InvalidOperationException(
                $"The registered localizer for '{typeof(TBaseResource).FullName}' does not support resource inheritance.");

    public bool TryResolve(
        string key,
        CultureInfo culture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture)
        => _resolver.TryResolveWithoutDiagnostics(
            key, culture, count, category, out translation, out resolvedCulture);
}

internal sealed class OmniLocalizationRegistrationState
{
    private readonly Dictionary<Type, HashSet<Type>> _baseResources = [];

    public void AddBaseResource(Type resource, Type baseResource)
    {
        if (resource == baseResource || HasPath(baseResource, resource))
            throw new InvalidOperationException(
                $"Adding '{baseResource.FullName}' as a base of '{resource.FullName}' creates a localization resource cycle.");
        if (!_baseResources.TryGetValue(resource, out HashSet<Type>? bases))
        {
            bases = [];
            _baseResources.Add(resource, bases);
        }
        bases.Add(baseResource);
    }

    private bool HasPath(Type from, Type searched)
    {
        if (from == searched)
            return true;
        if (!_baseResources.TryGetValue(from, out HashSet<Type>? bases))
            return false;
        foreach (Type next in bases)
        {
            if (HasPath(next, searched))
                return true;
        }
        return false;
    }
}
