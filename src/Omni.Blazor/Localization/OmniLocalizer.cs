using System.Collections.Frozen;
using System.Globalization;
using System.Resources;

namespace Omni.Blazor.Localization;

internal sealed class OmniLocalizer : IOmniLocalizer
{
    private const string ResourceBaseName = "Omni.Blazor.Localization.Resources.OmniResources";
    private static readonly ResourceManager Resources = new(ResourceBaseName, typeof(OmniLocalizer).Assembly);
    private readonly IOmniTranslationProvider[] _providers;
    private readonly IOmniPluralRule[] _pluralRules;

    public OmniLocalizer(
        IEnumerable<IOmniTranslationProvider> providers,
        IEnumerable<IOmniPluralRule> pluralRules)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(pluralRules);
        _providers = providers.ToArray();
        _pluralRules = pluralRules.ToArray();
    }

    public string this[string key] => Localize(key).Value;

    public string this[string key, params object?[] arguments] => Format(key, arguments: arguments);

    public OmniLocalizedString Localize(string key, CultureInfo? culture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        CultureInfo effectiveCulture = culture ?? CultureInfo.CurrentUICulture;
        var request = new OmniTranslationRequest(key, effectiveCulture, null, null);

        if (TryProviders(in request, out string translated))
            return new(key, translated, effectiveCulture, ResourceNotFound: false);

        string? builtIn = Resources.GetString(key, effectiveCulture);
        return builtIn is null
            ? new(key, key, effectiveCulture, ResourceNotFound: true)
            : new(key, builtIn, effectiveCulture, ResourceNotFound: false);
    }

    public string Format(string key, CultureInfo? culture = null, params object?[] arguments)
    {
        CultureInfo effectiveCulture = culture ?? CultureInfo.CurrentCulture;
        string format = Localize(key, culture ?? CultureInfo.CurrentUICulture).Value;
        return arguments.Length == 0 ? format : string.Format(effectiveCulture, format, arguments);
    }

    public string Plural(string key, decimal count, CultureInfo? culture = null, params object?[] arguments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        CultureInfo effectiveUiCulture = culture ?? CultureInfo.CurrentUICulture;
        OmniPluralCategory category = GetPluralCategory(effectiveUiCulture, count);
        var request = new OmniTranslationRequest(key, effectiveUiCulture, count, category);

        string? format = null;
        if (!TryProviders(in request, out format))
        {
            format = Resources.GetString($"{key}.{category}", effectiveUiCulture)
                ?? Resources.GetString($"{key}.Other", effectiveUiCulture)
                ?? Resources.GetString(key, effectiveUiCulture)
                ?? key;
        }

        if (arguments.Length == 0)
            return format;

        CultureInfo formatCulture = culture ?? CultureInfo.CurrentCulture;
        return string.Format(formatCulture, format, arguments);
    }

    private bool TryProviders(in OmniTranslationRequest request, out string translation)
    {
        foreach (IOmniTranslationProvider provider in _providers)
        {
            if (provider.TryGetTranslation(in request, out translation) && !string.IsNullOrEmpty(translation))
                return true;
        }

        translation = string.Empty;
        return false;
    }

    private OmniPluralCategory GetPluralCategory(CultureInfo culture, decimal count)
    {
        foreach (IOmniPluralRule rule in _pluralRules)
        {
            if (rule.TryGetCategory(culture, count, out OmniPluralCategory category))
                return category;
        }

        return DefaultOmniPluralRule.GetCategory(culture, count);
    }
}

/// <summary>
/// Immutable translation catalog suited to JSON, configuration and database
/// snapshots. The supplied dictionary is copied into a frozen lookup table.
/// </summary>
public sealed class OmniTranslationCatalog : IOmniTranslationProvider
{
    private readonly string _cultureName;
    private readonly FrozenDictionary<string, string> _translations;

    /// <summary>Creates a catalog for one culture or neutral language.</summary>
    public OmniTranslationCatalog(string cultureName, IEnumerable<KeyValuePair<string, string>> translations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        ArgumentNullException.ThrowIfNull(translations);
        _cultureName = CultureInfo.GetCultureInfo(cultureName).Name;
        _translations = translations.ToFrozenDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.Ordinal);
    }

    /// <inheritdoc />
    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
    {
        if (!Matches(request.Culture))
        {
            translation = string.Empty;
            return false;
        }

        if (request.PluralCategory is { } category &&
            _translations.TryGetValue($"{request.Key}.{category}", out translation!))
            return true;

        return _translations.TryGetValue(request.Key, out translation!);
    }

    private bool Matches(CultureInfo culture)
    {
        for (CultureInfo? candidate = culture; candidate is not null && candidate != CultureInfo.InvariantCulture; candidate = candidate.Parent)
        {
            if (string.Equals(candidate.Name, _cultureName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}

internal static class DefaultOmniPluralRule
{
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
