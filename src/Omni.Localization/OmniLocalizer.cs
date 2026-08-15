using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Omni.Localization;

internal sealed class OmniLocalizer<TResource>
    : IOmniLocalizer<TResource>, IOmniLocalizationResolver<TResource>
{
    private readonly IOmniTranslationProvider<TResource>[] _providers;
    private readonly IOmniPluralRule[] _pluralRules;
    private readonly IOmniInheritedResource<TResource>[] _inheritedResources;
    private readonly OmniLocalizationResource<TResource> _resource;
    private readonly OmniMissingTranslationBehavior _missingBehavior;
    private readonly int _maximumTrackedMissingTranslations;
    private readonly ILogger<OmniLocalizer<TResource>> _logger;
    private readonly HashSet<string>? _reportedMisses;
    private readonly Lock? _reportedMissesLock;

    public OmniLocalizer(
        IEnumerable<IOmniTranslationProvider<TResource>> providers,
        IEnumerable<IOmniPluralRule> pluralRules,
        IEnumerable<IOmniInheritedResource<TResource>> inheritedResources,
        IEnumerable<OmniLocalizationResourceOptions<TResource>> resourceOptions,
        OmniLocalizationResource<TResource> resource,
        OmniLocalizationOptions options,
        ILogger<OmniLocalizer<TResource>>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(pluralRules);
        ArgumentNullException.ThrowIfNull(inheritedResources);
        ArgumentNullException.ThrowIfNull(resourceOptions);
        _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        ArgumentNullException.ThrowIfNull(options);

        _providers = providers.OrderByDescending(static provider => provider.Priority).ToArray();
        _pluralRules = pluralRules.ToArray();
        _inheritedResources = inheritedResources.ToArray();
        OmniLocalizationResourceOptions<TResource>? resourceOverride = resourceOptions.LastOrDefault();
        _missingBehavior = resourceOverride?.MissingTranslationBehavior
            ?? options.MissingTranslationBehavior;
        _maximumTrackedMissingTranslations = resourceOverride?.MaximumTrackedMissingTranslations
            ?? options.MaximumTrackedMissingTranslations;
        _logger = logger ?? NullLogger<OmniLocalizer<TResource>>.Instance;

        ValidateCatalogs();
        if (_missingBehavior == OmniMissingTranslationBehavior.Log &&
            _maximumTrackedMissingTranslations > 0)
        {
            _reportedMisses = new(StringComparer.Ordinal);
            _reportedMissesLock = new();
        }
    }

    public string this[string key] => Localize(key).Value;

    public string this[string key, params object?[] arguments] => Format(key, arguments: arguments);

    public OmniLocalizedString Localize(string key, CultureInfo? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        CultureInfo requestedCulture = culture ?? CultureInfo.CurrentUICulture;
        if (TryResolveRequestedChain(key, requestedCulture, null, null, out string value, out CultureInfo resolved))
        {
            bool usedFallback = !string.Equals(
                requestedCulture.Name,
                resolved.Name,
                StringComparison.OrdinalIgnoreCase);
            return new(key, value, requestedCulture, resolved, ResourceNotFound: false, usedFallback);
        }

        if (TryResolveDefaultChain(key, requestedCulture, null, null, out value, out resolved))
        {
            ReportMissing(key, requestedCulture);
            return new(key, value, requestedCulture, resolved, ResourceNotFound: false, UsedFallback: true);
        }

        if (TryInheritedResources(key, requestedCulture, null, null, out value, out resolved))
            return new(key, value, requestedCulture, resolved, ResourceNotFound: false, UsedFallback: true);

        ReportMissing(key, requestedCulture);
        return new(key, key, requestedCulture, null, ResourceNotFound: true, UsedFallback: true);
    }

    public string Format(string key, CultureInfo? culture = null, params object?[] arguments)
    {
        CultureInfo formatCulture = culture ?? CultureInfo.CurrentCulture;
        string format = Localize(key, culture ?? CultureInfo.CurrentUICulture).Value;
        return arguments.Length == 0 ? format : string.Format(formatCulture, format, arguments);
    }

    public string Plural(string key, decimal count, CultureInfo? culture = null, params object?[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        CultureInfo requestedCulture = culture ?? CultureInfo.CurrentUICulture;
        OmniPluralCategory category = GetPluralCategory(requestedCulture, count);

        bool found = TryResolveRequestedChain(
            key, requestedCulture, count, category, out string format, out _);
        if (!found)
        {
            found = TryResolveDefaultChain(
                key, requestedCulture, count, category, out format, out _);
            if (found)
                ReportMissing(string.Concat(key, ".", category.ToString()), requestedCulture);
        }

        if (!found)
            found = TryInheritedResources(key, requestedCulture, count, category, out format, out _);

        if (!found)
        {
            ReportMissing(string.Concat(key, ".", category.ToString()), requestedCulture);
            format = key;
        }

        if (arguments.Length == 0)
            return format;
        return string.Format(culture ?? CultureInfo.CurrentCulture, format, arguments);
    }

    bool IOmniLocalizationResolver<TResource>.TryResolveWithoutDiagnostics(
        string key,
        CultureInfo culture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture)
    {
        if (TryResolveRequestedChain(
            key, culture, count, category, out translation, out resolvedCulture))
            return true;
        if (TryResolveDefaultChain(
            key, culture, count, category, out translation, out resolvedCulture))
            return true;
        return TryInheritedResources(
            key, culture, count, category, out translation, out resolvedCulture);
    }

    private bool TryResolveRequestedChain(
        string key,
        CultureInfo requestedCulture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture)
        => TryCultureChain(key, requestedCulture, count, category, stopBefore: null,
            out translation, out resolvedCulture);

    private bool TryResolveDefaultChain(
        string key,
        CultureInfo requestedCulture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture)
    {
        if (ContainsCulture(requestedCulture, _resource.DefaultCulture))
        {
            translation = string.Empty;
            resolvedCulture = CultureInfo.InvariantCulture;
            return false;
        }

        return TryCultureChain(key, _resource.DefaultCulture, count, category,
            stopBefore: requestedCulture, out translation, out resolvedCulture);
    }

    private bool TryCultureChain(
        string key,
        CultureInfo start,
        decimal? count,
        OmniPluralCategory? category,
        CultureInfo? stopBefore,
        out string translation,
        out CultureInfo resolvedCulture)
    {
        for (CultureInfo? candidate = start;
             candidate is not null && candidate != CultureInfo.InvariantCulture;
             candidate = candidate.Parent)
        {
            if (stopBefore is not null && ContainsCulture(stopBefore, candidate))
                break;

            var request = new OmniTranslationRequest(key, candidate, count, category);
            foreach (IOmniTranslationProvider<TResource> provider in _providers)
            {
                if (provider.TryGetTranslation(in request, out translation) &&
                    !string.IsNullOrEmpty(translation))
                {
                    resolvedCulture = candidate;
                    return true;
                }
            }
        }

        translation = string.Empty;
        resolvedCulture = CultureInfo.InvariantCulture;
        return false;
    }

    private bool TryInheritedResources(
        string key,
        CultureInfo culture,
        decimal? count,
        OmniPluralCategory? category,
        out string translation,
        out CultureInfo resolvedCulture)
    {
        foreach (IOmniInheritedResource<TResource> inherited in _inheritedResources)
        {
            if (inherited.TryResolve(
                key, culture, count, category, out translation, out resolvedCulture))
                return true;
        }
        translation = string.Empty;
        resolvedCulture = CultureInfo.InvariantCulture;
        return false;
    }

    private static bool ContainsCulture(CultureInfo start, CultureInfo searched)
    {
        for (CultureInfo? candidate = start;
             candidate is not null && candidate != CultureInfo.InvariantCulture;
             candidate = candidate.Parent)
        {
            if (string.Equals(candidate.Name, searched.Name, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private OmniPluralCategory GetPluralCategory(CultureInfo culture, decimal count)
    {
        foreach (IOmniPluralRule rule in _pluralRules)
        {
            if (rule.TryGetCategory(culture, count, out OmniPluralCategory category))
                return category;
        }
        return OmniPluralRules.GetCategory(culture, count);
    }

    private void ValidateCatalogs()
    {
        foreach (IOmniTranslationProvider<TResource> provider in _providers)
        {
            if (provider is not OmniTranslationCatalog<TResource> catalog)
                continue;
            OmniTranslationCatalogValidationResult validation = OmniTranslationCatalogValidator.Validate(
                catalog.Culture.Name,
                catalog.Translations,
                _resource.ReferenceTranslations);
            if (!validation.IsValid)
                throw new InvalidOperationException(OmniTranslationCatalogValidator.FormatErrors(validation));
        }
    }

    private void ReportMissing(string key, CultureInfo culture)
    {
        switch (_missingBehavior)
        {
            case OmniMissingTranslationBehavior.Ignore:
                return;
            case OmniMissingTranslationBehavior.Throw:
                throw new OmniMissingTranslationException(typeof(TResource), key, culture.Name);
            case OmniMissingTranslationBehavior.Log:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_missingBehavior));
        }

        if (_reportedMisses is null || _reportedMissesLock is null)
            return;
        string identity = string.Concat(culture.Name, "\0", key);
        lock (_reportedMissesLock)
        {
            if (_reportedMisses.Count >= _maximumTrackedMissingTranslations ||
                !_reportedMisses.Add(identity))
                return;
        }

        _logger.LogWarning(
            "Translation {TranslationKey} from resource {ResourceType} was not found for culture {CultureName}; fallback was used.",
            key,
            typeof(TResource).FullName,
            culture.Name);
    }
}

/// <summary>Built-in CLDR-style plural selection for the cultures supported by Omni.</summary>
public static class OmniPluralRules
{
    /// <summary>Returns the default plural category for a culture and count.</summary>
    public static OmniPluralCategory GetCategory(CultureInfo culture, decimal count)
    {
        string language = culture.TwoLetterISOLanguageName;
        decimal absolute = Math.Abs(count);
        bool integer = decimal.Truncate(absolute) == absolute;
        long value = integer && absolute <= long.MaxValue ? (long)absolute : -1;
        long mod10 = value < 0 ? -1 : value % 10;
        long mod100 = value < 0 ? -1 : value % 100;

        return language switch
        {
            "ar" when value == 0 => OmniPluralCategory.Zero,
            "ar" when value == 1 => OmniPluralCategory.One,
            "ar" when value == 2 => OmniPluralCategory.Two,
            "ar" when mod100 is >= 3 and <= 10 => OmniPluralCategory.Few,
            "ar" when mod100 is >= 11 and <= 99 => OmniPluralCategory.Many,
            "ru" or "uk" or "be" when mod10 == 1 && mod100 != 11 => OmniPluralCategory.One,
            "ru" or "uk" or "be" when mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14) => OmniPluralCategory.Few,
            "ru" or "uk" or "be" when integer => OmniPluralCategory.Many,
            "pl" when value == 1 => OmniPluralCategory.One,
            "pl" when mod10 is >= 2 and <= 4 && mod100 is not (>= 12 and <= 14) => OmniPluralCategory.Few,
            "pl" when integer => OmniPluralCategory.Many,
            "cs" or "sk" when value == 1 => OmniPluralCategory.One,
            "cs" or "sk" when value is >= 2 and <= 4 => OmniPluralCategory.Few,
            "sl" when mod100 == 1 => OmniPluralCategory.One,
            "sl" when mod100 == 2 => OmniPluralCategory.Two,
            "sl" when mod100 is 3 or 4 => OmniPluralCategory.Few,
            "fr" when absolute is 0 or 1 => OmniPluralCategory.One,
            _ when absolute == 1 => OmniPluralCategory.One,
            _ => OmniPluralCategory.Other
        };
    }
}
