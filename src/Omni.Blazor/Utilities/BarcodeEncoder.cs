using System.Text;
using Omni.Blazor.Models;

namespace Omni.Blazor.Utilities;

/// <summary>
/// Pure-C# 1D barcode encoders. Each <c>Encode*</c> method returns a bit
/// string (<c>"1"</c> = bar, <c>"0"</c> = space) representing the modules.
/// The renderer scales these into SVG rectangles.
/// </summary>
internal static class BarcodeEncoder
{
    /// <summary>Encode <paramref name="value"/> according to <paramref name="type"/>.
    /// Throws <see cref="ArgumentException"/> for invalid characters or wrong-length
    /// inputs. Returns a bit string where each character is one module.</summary>
    public static string Encode(string value, BarcodeType type)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return type switch
        {
            BarcodeType.Code128 => EncodeCode128(value),
            BarcodeType.Code39  => EncodeCode39(value),
            BarcodeType.Ean13   => EncodeEan13(value),
            BarcodeType.Ean8    => EncodeEan8(value),
            BarcodeType.UpcA    => EncodeUpcA(value),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    /// <summary>Human-readable text that appears under the barcode (typically
    /// the value, plus any auto-appended checksum digit for EAN/UPC).</summary>
    public static string DisplayText(string value, BarcodeType type)
    {
        return type switch
        {
            BarcodeType.Ean13 => value.Length == 12 ? value + Ean13Check(value) : value,
            BarcodeType.Ean8  => value.Length == 7  ? value + Ean8Check(value)  : value,
            BarcodeType.UpcA  => value.Length == 11 ? value + UpcACheck(value)  : value,
            _ => value,
        };
    }

    // ============================================================
    // Code128 subset B (covers printable ASCII 32..127)
    // ============================================================
    // 107 patterns total, indexed 0..106. Each is 11 modules expressed as 6
    // alternating widths (bar,space,bar,space,bar,space).
    // Subset B starts with code 104 and uses values 0..94 directly (ASCII − 32).
    private static readonly string[] Code128Patterns = {
        "212222","222122","222221","121223","121322","131222","122213","122312","132212","221213",
        "221312","231212","112232","122132","122231","113222","123122","123221","223211","221132",
        "221231","213212","223112","312131","311222","321122","321221","312212","322112","322211",
        "212123","212321","232121","111323","131123","131321","112313","132113","132311","211313",
        "231113","231311","112133","112331","132131","113123","113321","133121","313121","211331",
        "231131","213113","213311","213131","311123","311321","331121","312113","312311","332111",
        "314111","221411","431111","111224","111422","121124","121421","141122","141221","112214",
        "112412","122114","122411","142112","142211","241211","221114","413111","241112","134111",
        "111242","121142","121241","114212","124112","124211","411212","421112","421211","212141",
        "214121","412121","111143","111341","131141","114113","114311","411113","411311","113141",
        "114131","311141","411131","211412","211214","211232","2331112"
    };

    private static string EncodeCode128(string value)
    {
        // Subset B accepts ASCII 32..127. Reject anything else with a clear message.
        foreach (var ch in value)
        {
            if (ch < 32 || ch > 127)
                throw new ArgumentException($"Code128 subset B aceita apenas ASCII 32..127. Caractere inválido: 0x{(int)ch:X4}");
        }

        var codes = new List<int> { 104 }; // start code B
        foreach (var ch in value) codes.Add(ch - 32);

        // Checksum
        int sum = codes[0];
        for (int i = 1; i < codes.Count; i++) sum += codes[i] * i;
        var check = sum % 103;
        codes.Add(check);
        codes.Add(106); // stop pattern

        var sb = new StringBuilder();
        foreach (var code in codes)
        {
            var pattern = Code128Patterns[code];
            for (int i = 0; i < pattern.Length; i++)
            {
                var modules = pattern[i] - '0';
                var bit = (i % 2 == 0) ? '1' : '0';
                sb.Append(bit, modules);
            }
        }
        return sb.ToString();
    }

    // ============================================================
    // Code39 (no checksum, * start/stop)
    // ============================================================
    // 9 elements per character: 5 bars + 4 spaces, 3 wide + 6 narrow.
    // Wide:Narrow ratio = 3:1. Each char encoded here as 9-bit string where
    // '1' = bar (alternating bar/space across the 9 positions).
    private const string Code39Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%*";
    private static readonly string[] Code39Patterns = {
        "101001101101","110100101011","101100101011","110110010101","101001101011",
        "110100110101","101100110101","101001011011","110100101101","101100101101",
        "110101001011","101101001011","110110100101","101011001011","110101100101",
        "101101100101","101010011011","110101001101","101101001101","101011001101",
        "110101010011","101101010011","110110101001","101011010011","110101101001",
        "101101101001","101010110011","110101011001","101101011001","101011011001",
        "110010101011","100110101011","110011010101","100101101011","110010110101",
        "100110110101","100101011011","110010101101","100110101101","100100100101",
        "100100101001","100101001001","101001001001","100101101101"
    };

    private static string EncodeCode39(string value)
    {
        var sb = new StringBuilder();
        // Implicit start/stop with *
        var full = "*" + value.ToUpperInvariant() + "*";
        for (int i = 0; i < full.Length; i++)
        {
            var ch = full[i];
            var idx = Code39Chars.IndexOf(ch);
            if (idx < 0) throw new ArgumentException($"Code39 não aceita o caractere '{ch}'.");
            sb.Append(Code39Patterns[idx]);
            // 1-module narrow space between characters (except after last)
            if (i < full.Length - 1) sb.Append('0');
        }
        return sb.ToString();
    }

    // ============================================================
    // EAN-13 / EAN-8 / UPC-A — shared L/G/R encoding tables
    // ============================================================
    // L-codes (left-hand odd parity)
    private static readonly string[] LCodes = {
        "0001101","0011001","0010011","0111101","0100011",
        "0110001","0101111","0111011","0110111","0001011"
    };
    // G-codes (left-hand even parity) — used in EAN-13 first-digit pattern
    private static readonly string[] GCodes = {
        "0100111","0110011","0011011","0100001","0011101",
        "0111001","0000101","0010001","0001001","0010111"
    };
    // R-codes (right-hand) — inverse of L
    private static readonly string[] RCodes = {
        "1110010","1100110","1101100","1000010","1011100",
        "1001110","1010000","1000100","1001000","1110100"
    };

    // EAN-13 first-digit parity patterns: L = odd, G = even
    private static readonly string[] Ean13ParityPatterns = {
        "LLLLLL","LLGLGG","LLGGLG","LLGGGL","LGLLGG",
        "LGGLLG","LGGGLL","LGLGLG","LGLGGL","LGGLGL"
    };

    private static int Ean13Check(string digits12)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            var d = digits12[i] - '0';
            sum += (i % 2 == 0) ? d : d * 3;
        }
        return (10 - sum % 10) % 10;
    }

    private static int Ean8Check(string digits7)
    {
        int sum = 0;
        for (int i = 0; i < 7; i++)
        {
            var d = digits7[i] - '0';
            sum += (i % 2 == 0) ? d * 3 : d;
        }
        return (10 - sum % 10) % 10;
    }

    private static int UpcACheck(string digits11)
    {
        int sum = 0;
        for (int i = 0; i < 11; i++)
        {
            var d = digits11[i] - '0';
            sum += (i % 2 == 0) ? d * 3 : d;
        }
        return (10 - sum % 10) % 10;
    }

    private static string EncodeEan13(string value)
    {
        // Accept 12 (we add checksum) or 13 (we validate).
        if (value.Length is not 12 and not 13 || !value.All(char.IsDigit))
            throw new ArgumentException("EAN-13 requer 12 ou 13 dígitos numéricos.");
        var digits12 = value[..12];
        var checkDigit = Ean13Check(digits12);
        if (value.Length == 13 && value[12] - '0' != checkDigit)
            throw new ArgumentException($"EAN-13: dígito verificador inválido. Esperado {checkDigit}.");

        var first = digits12[0] - '0';
        var pattern = Ean13ParityPatterns[first];
        var sb = new StringBuilder();
        sb.Append("101"); // left guard
        for (int i = 1; i <= 6; i++)
        {
            var d = digits12[i] - '0';
            sb.Append(pattern[i - 1] == 'L' ? LCodes[d] : GCodes[d]);
        }
        sb.Append("01010"); // center guard
        for (int i = 7; i < 12; i++)
        {
            var d = digits12[i] - '0';
            sb.Append(RCodes[d]);
        }
        sb.Append(RCodes[checkDigit]);
        sb.Append("101"); // right guard
        return sb.ToString();
    }

    private static string EncodeEan8(string value)
    {
        if (value.Length is not 7 and not 8 || !value.All(char.IsDigit))
            throw new ArgumentException("EAN-8 requer 7 ou 8 dígitos numéricos.");
        var digits7 = value[..7];
        var checkDigit = Ean8Check(digits7);
        if (value.Length == 8 && value[7] - '0' != checkDigit)
            throw new ArgumentException($"EAN-8: dígito verificador inválido. Esperado {checkDigit}.");

        var sb = new StringBuilder();
        sb.Append("101"); // left guard
        for (int i = 0; i < 4; i++) sb.Append(LCodes[digits7[i] - '0']);
        sb.Append("01010"); // center guard
        for (int i = 4; i < 7; i++) sb.Append(RCodes[digits7[i] - '0']);
        sb.Append(RCodes[checkDigit]);
        sb.Append("101"); // right guard
        return sb.ToString();
    }

    private static string EncodeUpcA(string value)
    {
        if (value.Length is not 11 and not 12 || !value.All(char.IsDigit))
            throw new ArgumentException("UPC-A requer 11 ou 12 dígitos numéricos.");
        var digits11 = value[..11];
        var checkDigit = UpcACheck(digits11);
        if (value.Length == 12 && value[11] - '0' != checkDigit)
            throw new ArgumentException($"UPC-A: dígito verificador inválido. Esperado {checkDigit}.");

        var sb = new StringBuilder();
        sb.Append("101"); // left guard
        for (int i = 0; i < 6; i++) sb.Append(LCodes[digits11[i] - '0']);
        sb.Append("01010"); // center guard
        for (int i = 6; i < 11; i++) sb.Append(RCodes[digits11[i] - '0']);
        sb.Append(RCodes[checkDigit]);
        sb.Append("101"); // right guard
        return sb.ToString();
    }
}
