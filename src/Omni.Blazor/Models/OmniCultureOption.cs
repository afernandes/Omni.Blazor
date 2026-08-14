using System.Globalization;

namespace Omni.Blazor.Models;

/// <summary>
/// One language offered by <c>OmniCulturePicker</c>.
///
/// Only <see cref="Name"/> is required; everything else is derived from
/// <see cref="CultureInfo"/> when left unset, so the common case is a bare list of
/// culture names.
/// </summary>
public sealed class OmniCultureOption
{
    private readonly string _name = string.Empty;

    /// <summary>Culture name, such as <c>pt-BR</c> or <c>en</c>.</summary>
    public required string Name
    {
        get => _name;
        init => _name = value ?? string.Empty;
    }

    /// <summary>
    /// Label shown to the user. Defaults to the culture's own display name, which reads
    /// in the culture being offered rather than in the one currently active — a reader
    /// looking for their language finds it written the way they write it.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>Secondary line. Defaults to the culture name.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// Two-letter region used to pick the flag, such as <c>BR</c>. Defaults to the region
    /// in <see cref="Name"/>; a culture without one (<c>en</c>, or a pseudo-locale) has no
    /// flag and falls back to a code badge.
    /// </summary>
    public string? Region { get; init; }

    /// <summary>Resolved label.</summary>
    public string ResolvedDisplayName => DisplayName ?? TryCulture()?.NativeName ?? Name;

    /// <summary>Resolved secondary line.</summary>
    public string ResolvedDescription => Description ?? Name;

    /// <summary>
    /// Resolved region, uppercased. Null when the culture carries none, which is the
    /// signal to draw the code badge instead of a flag.
    /// </summary>
    public string? ResolvedRegion
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Region))
                return Region.ToUpperInvariant();

            int separator = Name.LastIndexOfAny(['-', '_']);
            if (separator < 0 || separator == Name.Length - 1)
                return null;

            string candidate = Name[(separator + 1)..];
            // A script or variant subtag is not a region: "zh-Hans" and the "en-XA"
            // pseudo-locales must not be dressed up with somebody's flag.
            return candidate.Length == 2 && candidate.All(char.IsLetter)
                ? candidate.ToUpperInvariant()
                : null;
        }
    }

    /// <summary>Short badge for cultures with no flag, such as <c>XA</c>.</summary>
    public string CodeBadge
    {
        get
        {
            int separator = Name.LastIndexOfAny(['-', '_']);
            string tail = separator >= 0 && separator < Name.Length - 1 ? Name[(separator + 1)..] : Name;
            return tail.Length <= 3 ? tail.ToUpperInvariant() : tail[..2].ToUpperInvariant();
        }
    }

    private CultureInfo? TryCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo(Name);
        }
        catch (CultureNotFoundException)
        {
            // Pseudo-locales and made-up names are legitimate here — the picker still has
            // to render them, it just cannot ask the framework what they are called.
            return null;
        }
    }
}
