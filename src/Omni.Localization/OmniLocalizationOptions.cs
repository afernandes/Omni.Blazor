namespace Omni.Localization;

/// <summary>Controls diagnostics shared by typed Omni localization resources.</summary>
public sealed class OmniLocalizationOptions
{
    /// <summary>Action taken when resolution reaches a default resource or the key itself.</summary>
    public OmniMissingTranslationBehavior MissingTranslationBehavior { get; set; }

    /// <summary>Maximum distinct resource/culture/key misses retained per localizer scope.</summary>
    public int MaximumTrackedMissingTranslations { get; set; } = 256;
}

/// <summary>Optional diagnostics override for one typed resource.</summary>
public sealed class OmniLocalizationResourceOptions<TResource>
{
    /// <summary>Overrides the global missing-translation behavior when set.</summary>
    public OmniMissingTranslationBehavior? MissingTranslationBehavior { get; set; }

    /// <summary>Overrides the global bounded miss count when set.</summary>
    public int? MaximumTrackedMissingTranslations { get; set; }
}

/// <summary>Runtime behavior for missing translations.</summary>
public enum OmniMissingTranslationBehavior
{
    /// <summary>Preserves fallback silently.</summary>
    Ignore,
    /// <summary>Logs each distinct miss once, up to the configured bound.</summary>
    Log,
    /// <summary>Throws immediately.</summary>
    Throw
}

/// <summary>Exception raised when strict localization detects a missing translation.</summary>
public sealed class OmniMissingTranslationException : InvalidOperationException
{
    /// <summary>Creates the exception for a resource, key and requested culture.</summary>
    public OmniMissingTranslationException(Type resourceType, string key, string cultureName)
        : base($"Translation '{key}' from resource '{resourceType.FullName}' was not found for culture '{cultureName}'.")
    {
        ResourceType = resourceType;
        Key = key;
        CultureName = cultureName;
    }

    /// <summary>The typed resource that owns the key.</summary>
    public Type ResourceType { get; }

    /// <summary>The stable translation key.</summary>
    public string Key { get; }

    /// <summary>The requested culture name.</summary>
    public string CultureName { get; }
}
