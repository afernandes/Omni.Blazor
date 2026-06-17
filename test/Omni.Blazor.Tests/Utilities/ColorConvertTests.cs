using Omni.Blazor.Utilities;

namespace Omni.Blazor.Tests.Utilities;

/// <summary>
/// Unit contract for <see cref="ColorConvert"/> — the hex ⇄ RGB ⇄ HSV math
/// behind <c>OmniColorPicker</c>. Exercised directly (internal via
/// InternalsVisibleTo) since the conversions are pure and easy to pin down.
/// </summary>
public class ColorConvertTests
{
    [Theory]
    [InlineData("#fff", 255, 255, 255, 255)]
    [InlineData("#000", 0, 0, 0, 255)]
    [InlineData("#f00", 255, 0, 0, 255)]
    [InlineData("abc", 170, 187, 204, 255)]          // sem '#', 3 dígitos
    [InlineData("#ff0000", 255, 0, 0, 255)]
    [InlineData("#3B82F6", 59, 130, 246, 255)]       // uppercase aceito
    [InlineData("#ff000080", 255, 0, 0, 128)]        // 8 dígitos com alpha
    [InlineData("#f008", 255, 0, 0, 136)]            // 4 dígitos (rgba curto)
    public void TryParseHex_parses_valid(string hex, int r, int g, int b, int a)
    {
        Assert.True(ColorConvert.TryParseHex(hex, out var rr, out var gg, out var bb, out var aa));
        Assert.Equal((byte)r, rr);
        Assert.Equal((byte)g, gg);
        Assert.Equal((byte)b, bb);
        Assert.Equal((byte)a, aa);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#gggggg")]
    public void TryParseHex_rejects_invalid(string? hex)
    {
        Assert.False(ColorConvert.TryParseHex(hex, out _, out _, out _, out _));
    }

    [Fact]
    public void ToHex_formats_lowercase_with_and_without_alpha()
    {
        Assert.Equal("#ff0000", ColorConvert.ToHex(255, 0, 0));
        Assert.Equal("#ff000080", ColorConvert.ToHex(255, 0, 0, 128));
        Assert.Equal("#0a0b0c", ColorConvert.ToHex(10, 11, 12));
    }

    [Theory]
    [InlineData(255, 0, 0, 0, 1, 1)]       // vermelho
    [InlineData(0, 255, 0, 120, 1, 1)]     // verde
    [InlineData(0, 0, 255, 240, 1, 1)]     // azul
    [InlineData(255, 255, 0, 60, 1, 1)]    // amarelo
    [InlineData(0, 0, 0, 0, 0, 0)]         // preto
    public void RgbToHsv_matches_known(int r, int g, int b, double h, double s, double v)
    {
        var (hh, ss, vv) = ColorConvert.RgbToHsv((byte)r, (byte)g, (byte)b);
        Assert.Equal(h, hh, 2);
        Assert.Equal(s, ss, 3);
        Assert.Equal(v, vv, 3);
    }

    [Fact]
    public void RgbToHsv_white_is_unsaturated_full_value()
    {
        var (_, s, v) = ColorConvert.RgbToHsv(255, 255, 255);
        Assert.Equal(0, s, 3);
        Assert.Equal(1, v, 3);
    }

    [Theory]
    [InlineData(0, 1, 1, 255, 0, 0)]
    [InlineData(120, 1, 1, 0, 255, 0)]
    [InlineData(240, 1, 1, 0, 0, 255)]
    [InlineData(60, 1, 1, 255, 255, 0)]
    public void HsvToRgb_matches_known(double h, double s, double v, int r, int g, int b)
    {
        var (rr, gg, bb) = ColorConvert.HsvToRgb(h, s, v);
        Assert.Equal((byte)r, rr);
        Assert.Equal((byte)g, gg);
        Assert.Equal((byte)b, bb);
    }

    [Theory]
    [InlineData("#f59e0b")]
    [InlineData("#3b82f6")]
    [InlineData("#16a34a")]
    [InlineData("#d946ef")]
    public void RoundTrip_rgb_hsv_rgb_is_lossless(string hex)
    {
        Assert.True(ColorConvert.TryParseHex(hex, out var r, out var g, out var b, out _));
        var (h, s, v) = ColorConvert.RgbToHsv(r, g, b);
        var (r2, g2, b2) = ColorConvert.HsvToRgb(h, s, v);
        Assert.Equal(hex, ColorConvert.ToHex(r2, g2, b2));
    }

    [Fact]
    public void HsvToRgb_wraps_hue_and_clamps_sv()
    {
        // h=360 deve equivaler a h=0; s/v fora de [0,1] são clampados.
        Assert.Equal(ColorConvert.HsvToRgb(0, 1, 1), ColorConvert.HsvToRgb(360, 1, 1));
        Assert.Equal(ColorConvert.HsvToRgb(0, 1, 1), ColorConvert.HsvToRgb(0, 2, 5));
    }
}
