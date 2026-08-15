using System.Collections.Frozen;
using System.Globalization;

namespace Omni.Localization;

/// <summary>Immutable exact-culture translation catalog for one typed resource.</summary>
public sealed class OmniTranslationCatalog<TResource> : IOmniTranslationProvider<TResource>
{
    private readonly FrozenDictionary<string, string> _translations;

    /// <summary>Creates an immutable catalog.</summary>
    public OmniTranslationCatalog(
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations,
        int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(translations);
        KeyValuePair<string, string>[] snapshot = translations.ToArray();
        OmniTranslationCatalogValidationResult validation =
            OmniTranslationCatalogValidator.ValidateBasic(cultureName, snapshot);
        if (!validation.IsValid)
            throw new ArgumentException(OmniTranslationCatalogValidator.FormatErrors(validation), nameof(translations));

        Culture = CultureInfo.GetCultureInfo(cultureName);
        Priority = priority;
        _translations = snapshot.ToFrozenDictionary(
            static pair => pair.Key,
            static pair => pair.Value,
            StringComparer.Ordinal);
    }

    /// <summary>The one exact culture served by this catalog.</summary>
    public CultureInfo Culture { get; }

    /// <inheritdoc />
    public int Priority { get; }

    /// <summary>The immutable translations, exposed for validation and tooling.</summary>
    public IReadOnlyDictionary<string, string> Translations => _translations;

    /// <inheritdoc />
    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
    {
        if (!string.Equals(request.Culture.Name, Culture.Name, StringComparison.OrdinalIgnoreCase))
        {
            translation = string.Empty;
            return false;
        }

        if (request.PluralCategory is { } category &&
            _translations.TryGetValue(string.Concat(request.Key, ".", category.ToString()), out translation!))
            return true;
        if (request.PluralCategory is not null &&
            _translations.TryGetValue(string.Concat(request.Key, ".Other"), out translation!))
            return true;
        return _translations.TryGetValue(request.Key, out translation!);
    }
}
