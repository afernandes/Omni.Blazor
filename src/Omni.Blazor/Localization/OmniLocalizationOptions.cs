namespace Omni.Blazor.Localization;

/// <summary>Controls how missing translations are diagnosed at runtime.</summary>
public sealed class OmniLocalizationOptions
{
    /// <summary>
    /// Action taken when a requested culture falls back to the built-in neutral
    /// catalog or to the stable translation key. Defaults to <see cref="OmniMissingTranslationBehavior.Ignore"/>.
    /// </summary>
    public OmniMissingTranslationBehavior MissingTranslationBehavior { get; set; }

    /// <summary>
    /// Maximum number of distinct culture/key misses retained and logged by one
    /// dependency-injection scope. The bound prevents an attacker-controlled key
    /// stream from creating an unbounded cache. Defaults to 256.
    /// </summary>
    public int MaximumTrackedMissingTranslations { get; set; } = 256;
}

/// <summary>Runtime behavior for missing or fallback translations.</summary>
public enum OmniMissingTranslationBehavior
{
    /// <summary>Preserves the fallback silently.</summary>
    Ignore,

    /// <summary>Logs each distinct miss once, up to the configured bound.</summary>
    Log,

    /// <summary>Throws an <see cref="OmniMissingTranslationException"/> immediately.</summary>
    Throw
}

/// <summary>Exception raised when strict localization detects a missing translation.</summary>
public sealed class OmniMissingTranslationException : InvalidOperationException
{
    /// <summary>Creates the exception for a stable key and requested culture.</summary>
    public OmniMissingTranslationException(string key, string cultureName)
        : base($"Translation '{key}' was not found for culture '{cultureName}'.")
    {
        Key = key;
        CultureName = cultureName;
    }

    /// <summary>The stable translation key.</summary>
    public string Key { get; }

    /// <summary>The requested culture name.</summary>
    public string CultureName { get; }
}
