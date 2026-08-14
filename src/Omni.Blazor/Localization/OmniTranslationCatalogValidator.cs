using System.Collections.Frozen;
using System.Globalization;
using System.Resources;
using System.Text;

namespace Omni.Blazor.Localization;

/// <summary>Validates external translation catalogs before they are used at runtime.</summary>
public static class OmniTranslationCatalogValidator
{
    private const string ResourceBaseName = "Omni.Blazor.Localization.Resources.OmniResources";
    private static readonly ResourceManager Resources = new(ResourceBaseName, typeof(OmniTranslationCatalogValidator).Assembly);
    private static readonly CultureInfo ReferenceCulture = CultureInfo.GetCultureInfo("en");
    private static readonly FrozenSet<string> KnownKeys =
        OmniTranslationKeys.AllCatalogKeys.ToFrozenSet(StringComparer.Ordinal);
    private static readonly FrozenSet<string> KnownBaseKeys =
        OmniTranslationKeys.All.ToFrozenSet(StringComparer.Ordinal);

    /// <summary>
    /// Checks culture validity, duplicate and unknown keys, empty translations,
    /// malformed composite formats and placeholder compatibility with the built-in catalog.
    /// Set <paramref name="requireCompleteCatalog"/> for release-time completeness checks.
    /// </summary>
    public static OmniTranslationCatalogValidationResult Validate(
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations,
        bool requireCompleteCatalog = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cultureName);
        ArgumentNullException.ThrowIfNull(translations);
        var issues = new List<OmniTranslationCatalogIssue>();
        try
        {
            _ = CultureInfo.GetCultureInfo(cultureName);
        }
        catch (CultureNotFoundException exception)
        {
            issues.Add(new(string.Empty, OmniTranslationCatalogIssueSeverity.Error, exception.Message));
            return new(issues);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> pair in translations)
        {
            if (string.IsNullOrWhiteSpace(pair.Key))
            {
                issues.Add(new(string.Empty, OmniTranslationCatalogIssueSeverity.Error, "Translation keys cannot be empty."));
                continue;
            }

            if (!seen.Add(pair.Key))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error, "The translation key is duplicated."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error, "The translation value cannot be empty."));
                continue;
            }

            string? referenceKey = ResolveReferenceKey(pair.Key);
            if (referenceKey is null)
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Warning, "The key is not part of the current Omni.Blazor catalog."));
                continue;
            }

            string? reference = Resources.GetString(referenceKey, ReferenceCulture);
            if (reference is null)
                continue;

            if (!TryReadPlaceholderIndexes(reference, out HashSet<int>? expected, out _))
                continue;

            if (!TryReadPlaceholderIndexes(pair.Value, out HashSet<int>? actual, out string? error))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error, $"Invalid composite format: {error}"));
                continue;
            }

            if (!expected.SetEquals(actual))
            {
                issues.Add(new(
                    pair.Key,
                    OmniTranslationCatalogIssueSeverity.Error,
                    $"Placeholder indexes must be [{Join(expected)}], but were [{Join(actual)}]."));
            }
        }

        if (requireCompleteCatalog)
        {
            foreach (string key in OmniTranslationKeys.AllCatalogKeys)
            {
                if (!seen.Contains(key))
                    issues.Add(new(key, OmniTranslationCatalogIssueSeverity.Error, "The required translation key is missing."));
            }
        }

        return new(issues);
    }

    private static bool TryReadPlaceholderIndexes(
        string format,
        out HashSet<int> indexes,
        out string? error)
    {
        indexes = [];
        error = null;
        try
        {
            _ = CompositeFormat.Parse(format);
        }
        catch (FormatException exception)
        {
            error = exception.Message;
            return false;
        }

        ReadOnlySpan<char> span = format.AsSpan();
        for (int index = 0; index < span.Length; index++)
        {
            if (span[index] != '{')
                continue;
            if (index + 1 < span.Length && span[index + 1] == '{')
            {
                index++;
                continue;
            }

            int cursor = index + 1;
            while (cursor < span.Length && char.IsAsciiDigit(span[cursor]))
                cursor++;
            if (int.TryParse(span[(index + 1)..cursor], NumberStyles.None, CultureInfo.InvariantCulture, out int value))
                indexes.Add(value);
        }

        return true;
    }

    private static string? ResolveReferenceKey(string key)
    {
        if (KnownKeys.Contains(key))
            return key;

        int separator = key.LastIndexOf('.');
        if (separator <= 0 || separator == key.Length - 1)
            return null;

        string suffix = key[(separator + 1)..];
        if (!Enum.TryParse(suffix, ignoreCase: false, out OmniPluralCategory _))
            return null;

        string baseKey = key[..separator];
        if (!KnownBaseKeys.Contains(baseKey))
            return null;

        string other = $"{baseKey}.Other";
        return KnownKeys.Contains(other) ? other : baseKey;
    }

    private static string Join(HashSet<int> indexes)
    {
        if (indexes.Count == 0)
            return string.Empty;

        int[] values = indexes.ToArray();
        Array.Sort(values);
        var builder = new StringBuilder();
        for (int index = 0; index < values.Length; index++)
        {
            if (index > 0)
                builder.Append(", ");
            builder.Append(values[index].ToString(CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}

/// <summary>Result of validating one translation catalog.</summary>
public sealed class OmniTranslationCatalogValidationResult
{
    internal OmniTranslationCatalogValidationResult(IReadOnlyList<OmniTranslationCatalogIssue> issues)
    {
        Issues = Array.AsReadOnly(issues.ToArray());
        IsValid = !Issues.Any(static issue => issue.Severity == OmniTranslationCatalogIssueSeverity.Error);
    }

    /// <summary>All validation errors and warnings.</summary>
    public IReadOnlyList<OmniTranslationCatalogIssue> Issues { get; }

    /// <summary>Whether the catalog contains no errors and can be loaded safely.</summary>
    public bool IsValid { get; }
}

/// <summary>One catalog validation finding.</summary>
/// <param name="Key">Stable translation key, or an empty string for a catalog-level issue.</param>
/// <param name="Severity">Whether the finding blocks loading the catalog.</param>
/// <param name="Message">Human-readable validation detail.</param>
public readonly record struct OmniTranslationCatalogIssue(
    string Key,
    OmniTranslationCatalogIssueSeverity Severity,
    string Message);

/// <summary>Severity of a catalog validation finding.</summary>
public enum OmniTranslationCatalogIssueSeverity
{
    /// <summary>Informational compatibility warning.</summary>
    Warning,

    /// <summary>Error that makes the catalog unsafe to load.</summary>
    Error
}
