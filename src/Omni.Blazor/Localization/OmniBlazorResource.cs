using System.Collections;
using System.Globalization;
using System.Resources;
using Omni.Localization;

namespace Omni.Blazor.Localization;

/// <summary>Typed localization resource that owns all built-in Omni.Blazor UI keys.</summary>
public sealed class OmniBlazorResource;

internal static class OmniBlazorResources
{
    private const string ResourceBaseName = "Omni.Blazor.Localization.Resources.OmniResources";
    private static readonly ResourceManager Resources =
        new(ResourceBaseName, typeof(OmniBlazorResource).Assembly);
    private static readonly Lazy<OmniLocalizationResource<OmniBlazorResource>> LazyDefinition =
        new(CreateDefinition, LazyThreadSafetyMode.ExecutionAndPublication);

    public static OmniLocalizationResource<OmniBlazorResource> Definition => LazyDefinition.Value;

    public static bool TryGetExact(
        string key,
        CultureInfo culture,
        OmniPluralCategory? category,
        out string translation)
    {
        ResourceSet? set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false);
        if (set is null)
        {
            translation = string.Empty;
            return false;
        }

        if (category is not null)
        {
            string? plural = set.GetString(string.Concat(key, ".", category.Value.ToString()), ignoreCase: false)
                ?? set.GetString(string.Concat(key, ".Other"), ignoreCase: false);
            if (!string.IsNullOrEmpty(plural))
            {
                translation = plural;
                return true;
            }
        }

        translation = set.GetString(key, ignoreCase: false) ?? string.Empty;
        return translation.Length > 0;
    }

    public static string? GetPluralFormat(string key, decimal count, CultureInfo culture)
    {
        OmniPluralCategory category = OmniPluralRules.GetCategory(culture, count);
        return Resources.GetString(string.Concat(key, ".", category.ToString()), culture)
            ?? Resources.GetString(string.Concat(key, ".Other"), culture)
            ?? Resources.GetString(key, culture);
    }

    private static OmniLocalizationResource<OmniBlazorResource> CreateDefinition()
        => new(
            defaultCulture: "pt-BR",
            referenceCulture: "pt-BR",
            referenceTranslations: ReadCatalog(CultureInfo.InvariantCulture));

    private static Dictionary<string, string> ReadCatalog(CultureInfo culture)
    {
        ResourceSet? set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"Embedded Omni.Blazor catalog '{culture.Name}' was not found.");
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in set)
        {
            if (entry.Key is string key && entry.Value is string value)
                result.Add(key, value);
        }
        return result;
    }

}

internal sealed class OmniBlazorEmbeddedTranslationProvider
    : IOmniTranslationProvider<OmniBlazorResource>
{
    public int Priority => -9_000;

    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
        => OmniBlazorResources.TryGetExact(
            request.Key,
            request.Culture,
            request.PluralCategory,
            out translation);
}
