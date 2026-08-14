using System.Globalization;
using System.Resources;
using System.Text;

namespace Omni.Blazor.Localization;

/// <summary>
/// Generates expanded <c>en-XA</c> and right-to-left <c>ar-XB</c> translations
/// for localization and layout testing. Composite-format placeholders are preserved.
/// </summary>
public sealed class OmniPseudoTranslationProvider : IOmniTranslationProvider
{
    private const string ResourceBaseName = "Omni.Blazor.Localization.Resources.OmniResources";
    private static readonly ResourceManager Resources = new(ResourceBaseName, typeof(OmniPseudoTranslationProvider).Assembly);
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    /// <inheritdoc />
    public bool TryGetTranslation(in OmniTranslationRequest request, out string translation)
    {
        bool isExpanded = string.Equals(request.Culture.Name, "en-XA", StringComparison.OrdinalIgnoreCase);
        bool isRtl = string.Equals(request.Culture.Name, "ar-XB", StringComparison.OrdinalIgnoreCase);
        if (!isExpanded && !isRtl)
        {
            translation = string.Empty;
            return false;
        }

        string? source = request.PluralCategory is { } category
            ? Resources.GetString($"{request.Key}.{category}", English)
                ?? Resources.GetString($"{request.Key}.Other", English)
                ?? Resources.GetString(request.Key, English)
            : Resources.GetString(request.Key, English);
        if (source is null)
        {
            translation = string.Empty;
            return false;
        }

        translation = Transform(source, isRtl);
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
            if (current.IsVowel())
                builder.Append(Accent(char.ToLowerInvariant(current)));
        }

        builder.Append(rightToLeft ? "⟧\u200f" : "］");
        return builder.ToString();
    }

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
        'Q' => 'Q',
        'q' => 'q',
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

internal static class CharacterExtensions
{
    public static bool IsVowel(this char value)
        => char.ToLowerInvariant(value) is 'a' or 'e' or 'i' or 'o' or 'u';
}
