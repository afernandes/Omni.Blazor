using System.Globalization;

namespace Omni.Localization;

/// <summary>Resolves strings belonging to one typed localization resource.</summary>
/// <typeparam name="TResource">Marker type that owns the localization key space.</typeparam>
public interface IOmniLocalizer<TResource>
{
    /// <summary>Gets a localized string using the ambient UI culture.</summary>
    string this[string key] { get; }

    /// <summary>Gets and formats a localized string using the ambient cultures.</summary>
    string this[string key, params object?[] arguments] { get; }

    /// <summary>Resolves a key for an explicit or ambient UI culture.</summary>
    OmniLocalizedString Localize(string key, CultureInfo? culture = null);

    /// <summary>Resolves and formats a key for an explicit or ambient culture.</summary>
    string Format(string key, CultureInfo? culture = null, params object?[] arguments);

    /// <summary>Resolves a CLDR-style plural form for a count.</summary>
    string Plural(string key, decimal count, CultureInfo? culture = null, params object?[] arguments);
}

/// <summary>A localized value together with resolution diagnostics.</summary>
public readonly record struct OmniLocalizedString(
    string Key,
    string Value,
    CultureInfo RequestedCulture,
    CultureInfo? ResolvedCulture,
    bool ResourceNotFound,
    bool UsedFallback)
{
    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Converts the result to its rendered value.</summary>
    public static implicit operator string(OmniLocalizedString value) => value.Value;
}

/// <summary>Plural categories used by Omni translation catalogs.</summary>
public enum OmniPluralCategory
{
    /// <summary>The zero form.</summary>
    Zero,
    /// <summary>The singular form.</summary>
    One,
    /// <summary>The dual form.</summary>
    Two,
    /// <summary>The paucal form.</summary>
    Few,
    /// <summary>The many form.</summary>
    Many,
    /// <summary>The general fallback form.</summary>
    Other
}

/// <summary>A compact request passed synchronously to translation providers.</summary>
public readonly record struct OmniTranslationRequest(
    string Key,
    CultureInfo Culture,
    decimal? Count,
    OmniPluralCategory? PluralCategory);

/// <summary>Source-agnostic translations for one typed resource.</summary>
/// <typeparam name="TResource">Marker type that owns the localization key space.</typeparam>
public interface IOmniTranslationProvider<TResource>
{
    /// <summary>
    /// Provider precedence. Higher values win. Providers with the same priority preserve
    /// dependency-injection registration order.
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// Attempts an exact-culture lookup. Providers must not walk parent cultures; the
    /// central resolver owns the complete fallback chain.
    /// </summary>
    bool TryGetTranslation(in OmniTranslationRequest request, out string translation);
}

/// <summary>Overrides plural selection for cultures handled by an application.</summary>
public interface IOmniPluralRule
{
    /// <summary>Attempts to select a plural category for a culture and count.</summary>
    bool TryGetCategory(CultureInfo culture, decimal count, out OmniPluralCategory category);
}
