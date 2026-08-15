using System.Globalization;
using System.Text;

namespace Omni.Localization;

/// <summary>Validates external catalogs against a typed resource reference catalog.</summary>
public sealed class OmniTranslationCatalogValidator<TResource>
{
    private readonly OmniLocalizationResource<TResource> _resource;

    /// <summary>Creates a validator for a typed resource.</summary>
    public OmniTranslationCatalogValidator(OmniLocalizationResource<TResource> resource)
        => _resource = resource ?? throw new ArgumentNullException(nameof(resource));

    /// <summary>Validates a catalog, including placeholder compatibility and optional completeness.</summary>
    public OmniTranslationCatalogValidationResult Validate(
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations,
        bool requireCompleteCatalog = false)
        => OmniTranslationCatalogValidator.Validate(
            cultureName,
            translations,
            _resource.ReferenceTranslations,
            requireCompleteCatalog);
}

/// <summary>Source-independent catalog validation helpers.</summary>
public static class OmniTranslationCatalogValidator
{
    /// <summary>Validates a catalog against an explicit reference catalog.</summary>
    public static OmniTranslationCatalogValidationResult Validate(
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations,
        IReadOnlyDictionary<string, string> referenceTranslations,
        bool requireCompleteCatalog = false)
    {
        ArgumentNullException.ThrowIfNull(referenceTranslations);
        OmniTranslationCatalogValidationResult basic = ValidateBasic(cultureName, translations);
        var issues = new List<OmniTranslationCatalogIssue>(basic.Issues);
        KeyValuePair<string, string>[] snapshot = translations.ToArray();
        var seen = new HashSet<string>(snapshot.Select(static pair => pair.Key), StringComparer.Ordinal);

        foreach (KeyValuePair<string, string> pair in snapshot)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
                continue;

            string? referenceKey = ResolveReferenceKey(pair.Key, referenceTranslations);
            if (referenceKey is null)
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Warning,
                    "The key is not part of the typed resource reference catalog."));
                continue;
            }

            string reference = referenceTranslations[referenceKey];
            if (!TryReadPlaceholderIndexes(reference, out HashSet<int>? expected, out _))
                continue;
            if (!TryReadPlaceholderIndexes(pair.Value, out HashSet<int>? actual, out string? error))
                continue; // Already reported by basic validation.
            if (!expected.SetEquals(actual))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error,
                    $"Placeholder indexes must be [{Join(expected)}], but were [{Join(actual)}]."));
            }
        }

        if (requireCompleteCatalog)
        {
            foreach (string key in referenceTranslations.Keys)
            {
                if (!seen.Contains(key))
                    issues.Add(new(key, OmniTranslationCatalogIssueSeverity.Error,
                        "The required translation key is missing."));
            }
        }

        return new(issues);
    }

    internal static OmniTranslationCatalogValidationResult ValidateBasic(
        string cultureName,
        IEnumerable<KeyValuePair<string, string>> translations)
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
                issues.Add(new(string.Empty, OmniTranslationCatalogIssueSeverity.Error,
                    "Translation keys cannot be empty."));
                continue;
            }
            if (!seen.Add(pair.Key))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error,
                    "The translation key is duplicated."));
                continue;
            }
            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error,
                    "The translation value cannot be empty."));
                continue;
            }
            if (!TryReadPlaceholderIndexes(pair.Value, out _, out string? error))
            {
                issues.Add(new(pair.Key, OmniTranslationCatalogIssueSeverity.Error,
                    $"Invalid composite format: {error}"));
            }
        }

        return new(issues);
    }

    internal static string FormatErrors(OmniTranslationCatalogValidationResult validation)
        => string.Join(Environment.NewLine, validation.Issues
            .Where(static issue => issue.Severity == OmniTranslationCatalogIssueSeverity.Error)
            .Select(static issue => $"{issue.Key}: {issue.Message}"));

    private static string? ResolveReferenceKey(
        string key,
        IReadOnlyDictionary<string, string> referenceTranslations)
    {
        if (referenceTranslations.ContainsKey(key))
            return key;

        int separator = key.LastIndexOf('.');
        if (separator <= 0 || separator == key.Length - 1)
            return null;
        if (!Enum.TryParse(key[(separator + 1)..], ignoreCase: false, out OmniPluralCategory _))
            return null;

        string baseKey = key[..separator];
        string other = string.Concat(baseKey, ".Other");
        if (referenceTranslations.ContainsKey(other))
            return other;
        return referenceTranslations.ContainsKey(baseKey) ? baseKey : null;
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
            if (int.TryParse(span[(index + 1)..cursor], NumberStyles.None,
                CultureInfo.InvariantCulture, out int value))
                indexes.Add(value);
        }

        return true;
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

    /// <summary>Whether the catalog contains no errors.</summary>
    public bool IsValid { get; }
}

/// <summary>One catalog validation finding.</summary>
public readonly record struct OmniTranslationCatalogIssue(
    string Key,
    OmniTranslationCatalogIssueSeverity Severity,
    string Message);

/// <summary>Severity of one catalog validation finding.</summary>
public enum OmniTranslationCatalogIssueSeverity
{
    /// <summary>Non-blocking compatibility warning.</summary>
    Warning,
    /// <summary>Error that blocks loading.</summary>
    Error
}
