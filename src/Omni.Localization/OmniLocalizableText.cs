using System.Globalization;

namespace Omni.Localization;

/// <summary>A serializable fixed value or late-bound key for one typed resource.</summary>
public readonly record struct OmniLocalizableText<TResource>
{
    private OmniLocalizableText(string value, bool isKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
        IsKey = isKey;
    }

    /// <summary>The fixed value or localization key.</summary>
    public string Value { get; }

    /// <summary>Whether <see cref="Value"/> is resolved through a localizer.</summary>
    public bool IsKey { get; }

    /// <summary>Creates a late-bound localized key.</summary>
    public static OmniLocalizableText<TResource> Localized(string key) => new(key, isKey: true);

    /// <summary>Creates a fixed value that bypasses localization.</summary>
    public static OmniLocalizableText<TResource> Fixed(string value) => new(value, isKey: false);

    /// <summary>Resolves this value.</summary>
    public string Resolve(IOmniLocalizer<TResource> localizer, CultureInfo? culture = null)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        return IsKey ? localizer.Localize(Value, culture).Value : Value;
    }

    /// <summary>Serializes as <c>L:key</c> or <c>F:value</c>.</summary>
    public override string ToString() => string.Concat(IsKey ? "L:" : "F:", Value);

    /// <summary>Parses a serialized value; unprefixed input is treated as fixed text.</summary>
    public static OmniLocalizableText<TResource> Parse(string serialized)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serialized);
        if (serialized.StartsWith("L:", StringComparison.Ordinal) && serialized.Length > 2)
            return Localized(serialized[2..]);
        if (serialized.StartsWith("F:", StringComparison.Ordinal) && serialized.Length > 2)
            return Fixed(serialized[2..]);
        return Fixed(serialized);
    }
}
