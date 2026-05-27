using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniQRCode"/>: renders SVG, handles
/// empty value, cross-cutting splat.
/// </summary>
public class OmniQRCodeTests : TestContextBase
{
    [Fact]
    public void Renders_svg_for_valid_value()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "https://example.com"));

        var root = cut.Find("div.omni-qrcode");
        Assert.Contains("omni-qrcode", root.ClassName);
        Assert.NotNull(cut.Find("svg.omni-qrcode-svg"));
    }

    [Fact]
    public void Empty_value_renders_no_svg()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, ""));

        Assert.NotNull(cut.Find("div.omni-qrcode"));
        Assert.Empty(cut.FindAll("svg.omni-qrcode-svg"));
    }

    [Fact]
    public void Size_applied_to_root_style()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.Size, "150px"));

        var style = cut.Find("div.omni-qrcode").GetAttribute("style") ?? string.Empty;
        Assert.Contains("width:150px", style);
        Assert.Contains("height:150px", style);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.Class, "my-qr"));

        Assert.Contains("my-qr", cut.Find("div.omni-qrcode").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_concatenated_with_size()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "abc")
            .Add(c => c.Style, "padding: 4px"));

        var style = cut.Find("div.omni-qrcode").GetAttribute("style") ?? string.Empty;
        Assert.Contains("padding: 4px", style);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "abc")
            .AddUnmatched("data-testid", "qr1"));

        Assert.Equal("qr1", cut.Find("div.omni-qrcode").GetAttribute("data-testid"));
    }

    // ─── ParameterState change-detection contract ─────────────────────────

    [Fact]
    public void Encode_runs_on_initial_render()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "https://example.com"));

        // Matrix populated by Recompute() through ParameterState first detect.
        Assert.NotNull(cut.Instance._matrix);
        // Plus the SVG renders at least one dark module (a <rect> or <circle>).
        Assert.NotEmpty(cut.FindAll("svg.omni-qrcode-svg rect, svg.omni-qrcode-svg circle"));
    }

    [Fact]
    public void Encode_does_NOT_run_when_unrelated_parameter_changes()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "https://example.com"));

        var matrixBefore = cut.Instance._matrix;
        var modulesBefore = cut.FindAll("svg.omni-qrcode-svg rect, svg.omni-qrcode-svg circle").Count;

        cut.SetParametersAndRender(p => p.Add(c => c.Class, "new-class"));

        // Reference equality — Recompute() never ran, the same matrix instance
        // is still in place.
        Assert.Same(matrixBefore, cut.Instance._matrix);
        Assert.Equal(modulesBefore, cut.FindAll("svg.omni-qrcode-svg rect, svg.omni-qrcode-svg circle").Count);
    }

    [Fact]
    public void Encode_runs_again_when_Value_changes()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "https://example.com"));

        var matrixBefore = cut.Instance._matrix;

        cut.SetParametersAndRender(p => p.Add(c => c.Value, "https://different-url.com/much/longer/path/x/y/z"));

        Assert.NotSame(matrixBefore, cut.Instance._matrix);
    }

    [Fact]
    public void Encode_runs_again_when_Ecc_changes()
    {
        var cut = RenderComponent<OmniQRCode>(p => p
            .Add(c => c.Value, "https://example.com")
            .Add(c => c.Ecc, QRCodeEcc.Low));

        var matrixBefore = cut.Instance._matrix;

        cut.SetParametersAndRender(p => p.Add(c => c.Ecc, QRCodeEcc.High));

        Assert.NotSame(matrixBefore, cut.Instance._matrix);
    }
}
