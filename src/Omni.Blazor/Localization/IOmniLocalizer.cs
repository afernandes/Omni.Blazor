using System.Globalization;

namespace Omni.Blazor.Localization;

/// <summary>
/// Resolves Omni.Blazor UI strings for an explicit or ambient UI culture.
/// Implementations must be thread-safe and must not capture
/// <see cref="CultureInfo.CurrentUICulture"/> at construction time.
/// </summary>
public interface IOmniLocalizer
{
    /// <summary>Gets a localized string using the ambient UI culture.</summary>
    string this[string key] { get; }

    /// <summary>Gets and formats a localized string using the ambient cultures.</summary>
    string this[string key, params object?[] arguments] { get; }

    /// <summary>Resolves a key for <paramref name="culture"/> without formatting it.</summary>
    OmniLocalizedString Localize(string key, CultureInfo? culture = null);

    /// <summary>Resolves and formats a key for <paramref name="culture"/>.</summary>
    string Format(string key, CultureInfo? culture = null, params object?[] arguments);

    /// <summary>
    /// Resolves the plural form of <paramref name="key"/>. Catalogs use the
    /// suffixes <c>.Zero</c>, <c>.One</c>, <c>.Two</c>, <c>.Few</c>,
    /// <c>.Many</c> and <c>.Other</c>.
    /// </summary>
    string Plural(string key, decimal count, CultureInfo? culture = null, params object?[] arguments);
}

/// <summary>A localized value together with fallback diagnostics.</summary>
public readonly record struct OmniLocalizedString(
    string Key,
    string Value,
    CultureInfo Culture,
    bool ResourceNotFound)
{
    /// <inheritdoc />
    public override string ToString() => Value;

    /// <summary>Converts a localization result to its rendered value.</summary>
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

/// <summary>A zero-allocation request passed to custom translation providers.</summary>
public readonly record struct OmniTranslationRequest(
    string Key,
    CultureInfo Culture,
    decimal? Count,
    OmniPluralCategory? PluralCategory);

/// <summary>
/// Source-agnostic translation provider. Implement this interface to read Omni
/// strings from RESX, PO/Gettext, JSON, a database, a tenant store or another
/// source. Return <see langword="false"/> to preserve the fallback chain.
/// </summary>
public interface IOmniTranslationProvider
{
    /// <summary>Attempts to resolve one translation.</summary>
    bool TryGetTranslation(in OmniTranslationRequest request, out string translation);
}

/// <summary>Overrides plural selection for cultures handled by the application.</summary>
public interface IOmniPluralRule
{
    /// <summary>Attempts to select a plural category for a culture and count.</summary>
    bool TryGetCategory(CultureInfo culture, decimal count, out OmniPluralCategory category);
}
