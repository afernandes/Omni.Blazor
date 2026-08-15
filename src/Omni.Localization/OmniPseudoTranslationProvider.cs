using System.Globalization;
using System.Text;

namespace Omni.Localization;

/// <summary>Generates expanded <c>en-XA</c> and RTL <c>ar-XB</c> translations.</summary>
public sealed class OmniPseudoTranslationProvider<TResource> : IOmniTranslationProvider<TResource>
{
    private readonly OmniLocalizationResource<TResource> _resource;

    /// <summary>Creates a provider from the resource reference catalog.</summary>
    public OmniPseudoTranslationProvider(OmniLocalizationResource<TResource> resource)
        => _resource = resource ?? throw new ArgumentNullException(nameof(resource));

    /// <inheritdoc />
    public int Priority => 10_000;

    /// <inheritdoc />
    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
    {
        bool expanded = string.Equals(request.Culture.Name, "en-XA", StringComparison.OrdinalIgnoreCase);
        bool rtl = string.Equals(request.Culture.Name, "ar-XB", StringComparison.OrdinalIgnoreCase);
        if (!expanded && !rtl)
        {
            translation = string.Empty;
            return false;
        }

        string? source = null;
        if (request.PluralCategory is { } category)
        {
            _resource.ReferenceTranslations.TryGetValue(
                string.Concat(request.Key, ".", category.ToString()), out source);
            if (source is null)
                _resource.ReferenceTranslations.TryGetValue(string.Concat(request.Key, ".Other"), out source);
        }
        if (source is null)
            _resource.ReferenceTranslations.TryGetValue(request.Key, out source);
        if (source is null)
        {
            translation = string.Empty;
            return false;
        }

        translation = Transform(source, rtl);
        return true;
    }

    private static string Transform(string source, bool rightToLeft)
    {
        var builder = new StringBuilder(source.Length + (source.Length / 2) + 8);
        builder.Append(rightToLeft ? "\u200f⟦" : "［");
        ReadOnlySpan<char> span = source.AsSpan();
        for (int index = 0; index < span.Length; index++)
        {
            char current = span[index];
            if (current == '{')
            {
                if (index + 1 < span.Length && span[index + 1] == '{')
                {
                    builder.Append("{{");
                    index++;
                    continue;
                }
                int closing = span[(index + 1)..].IndexOf('}');
                if (closing >= 0)
                {
                    closing += index + 1;
                    builder.Append(span[index..(closing + 1)]);
                    index = closing;
                    continue;
                }
            }

            builder.Append(Accent(current));
            if (IsVowel(current))
                builder.Append(Accent(char.ToLowerInvariant(current)));
        }
        builder.Append(rightToLeft ? "⟧\u200f" : "］");
        return builder.ToString();
    }

    private static bool IsVowel(char value)
        => char.ToLowerInvariant(value) is 'a' or 'e' or 'i' or 'o' or 'u';

    private static char Accent(char value) => value switch
    {
        'A' => 'Å',
        'a' => 'å',
        'B' => 'Ɓ',
        'b' => 'ƀ',
        'C' => 'Ç',
        'c' => 'ç',
        'D' => 'Ð',
        'd' => 'ð',
        'E' => 'Ë',
        'e' => 'ë',
        'F' => 'Ƒ',
        'f' => 'ƒ',
        'G' => 'Ģ',
        'g' => 'ģ',
        'H' => 'Ħ',
        'h' => 'ħ',
        'I' => 'Ï',
        'i' => 'ï',
        'J' => 'Ĵ',
        'j' => 'ĵ',
        'K' => 'Ķ',
        'k' => 'ķ',
        'L' => 'Ŀ',
        'l' => 'ŀ',
        'M' => 'Ḿ',
        'm' => 'ḿ',
        'N' => 'Ñ',
        'n' => 'ñ',
        'O' => 'Ö',
        'o' => 'ö',
        'P' => 'Þ',
        'p' => 'þ',
        'R' => 'Ŕ',
        'r' => 'ŕ',
        'S' => 'Š',
        's' => 'š',
        'T' => 'Ţ',
        't' => 'ţ',
        'U' => 'Ü',
        'u' => 'ü',
        'V' => 'Ṽ',
        'v' => 'ṽ',
        'W' => 'Ŵ',
        'w' => 'ŵ',
        'X' => 'Ж',
        'x' => 'ж',
        'Y' => 'Ÿ',
        'y' => 'ÿ',
        'Z' => 'Ž',
        'z' => 'ž',
        _ => value
    };
}
