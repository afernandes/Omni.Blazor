using System.Globalization;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Conversões de cor para o <c>OmniColorPicker</c>: hex ⇄ RGB ⇄ HSV.
/// Hue em [0,360), saturation/value/alpha em [0,1]; bytes em [0,255].
/// Sempre <see cref="CultureInfo.InvariantCulture"/> (locale pt-BR usa vírgula).
/// </summary>
internal static class ColorConvert
{
    /// <summary>
    /// Faz o parse de <c>#rgb</c>, <c>#rgba</c>, <c>#rrggbb</c> ou <c>#rrggbbaa</c>
    /// (o <c>#</c> é opcional). Retorna false (e bytes em 0/alpha=255) se inválido.
    /// </summary>
    public static bool TryParseHex(string? hex, out byte r, out byte g, out byte b, out byte a)
    {
        r = 0; g = 0; b = 0; a = 255;
        if (string.IsNullOrWhiteSpace(hex)) return false;

        var s = hex.Trim();
        if (s.StartsWith('#')) s = s[1..];

        switch (s.Length)
        {
            case 3:
                if (!Nibble(s[0], out var n1) || !Nibble(s[1], out var n2) || !Nibble(s[2], out var n3)) return false;
                r = (byte)(n1 * 17); g = (byte)(n2 * 17); b = (byte)(n3 * 17);
                return true;
            case 4:
                if (!Nibble(s[0], out var m1) || !Nibble(s[1], out var m2) || !Nibble(s[2], out var m3) || !Nibble(s[3], out var m4)) return false;
                r = (byte)(m1 * 17); g = (byte)(m2 * 17); b = (byte)(m3 * 17); a = (byte)(m4 * 17);
                return true;
            case 6:
                return Pair(s, 0, out r) && Pair(s, 2, out g) && Pair(s, 4, out b);
            case 8:
                return Pair(s, 0, out r) && Pair(s, 2, out g) && Pair(s, 4, out b) && Pair(s, 6, out a);
            default:
                return false;
        }
    }

    /// <summary><c>#rrggbb</c> (a = null) ou <c>#rrggbbaa</c>. Lowercase.</summary>
    public static string ToHex(byte r, byte g, byte b, byte? a = null)
        => a is null
            ? $"#{r:x2}{g:x2}{b:x2}"
            : $"#{r:x2}{g:x2}{b:x2}{a.Value:x2}";

    /// <summary>RGB (0-255) → HSV. H em [0,360), S/V em [0,1].</summary>
    public static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
    {
        double rn = r / 255.0, gn = g / 255.0, bn = b / 255.0;
        double max = Math.Max(rn, Math.Max(gn, bn));
        double min = Math.Min(rn, Math.Min(gn, bn));
        double d = max - min;

        double h = 0;
        if (d > 1e-9)
        {
            if (max == rn) h = 60 * (((gn - bn) / d) % 6);
            else if (max == gn) h = 60 * ((bn - rn) / d + 2);
            else h = 60 * ((rn - gn) / d + 4);
        }
        if (h < 0) h += 360;

        double s = max <= 0 ? 0 : d / max;
        return (h, s, max);
    }

    /// <summary>HSV → RGB (0-255). Aceita H fora de [0,360) (faz wrap) e S/V fora de [0,1] (clampa).</summary>
    public static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        s = Math.Clamp(s, 0, 1);
        v = Math.Clamp(v, 0, 1);

        double c = v * s;
        double x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        double m = v - c;

        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return (To(r + m), To(g + m), To(b + m));

        static byte To(double d) => (byte)Math.Clamp(Math.Round(d * 255), 0, 255);
    }

    private static bool Nibble(char c, out byte v)
    {
        if (c is >= '0' and <= '9') { v = (byte)(c - '0'); return true; }
        if (c is >= 'a' and <= 'f') { v = (byte)(c - 'a' + 10); return true; }
        if (c is >= 'A' and <= 'F') { v = (byte)(c - 'A' + 10); return true; }
        v = 0; return false;
    }

    private static bool Pair(string s, int i, out byte v)
        => byte.TryParse(s.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v);
}
