using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniBarcode"/>: renders SVG for valid
/// value, error for bad input, splat support.
/// </summary>
public class OmniBarcodeTests : TestContextBase
{
    [Fact]
    public void Renders_default_root_with_base_class()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "HELLO"));

        var root = cut.Find("div.omni-barcode");
        Assert.Contains("omni-barcode", root.ClassName);
        Assert.NotNull(cut.Find("svg.omni-barcode-svg"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC")
            .Add(c => c.Class, "my-bc"));

        Assert.Contains("my-bc", cut.Find("div.omni-barcode").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC")
            .Add(c => c.Style, "padding: 4px"));

        var styleAttr = cut.Find("div.omni-barcode").GetAttribute("style") ?? string.Empty;
        Assert.Contains("padding: 4px", styleAttr);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC")
            .AddUnmatched("data-testid", "bc1"));

        Assert.Equal("bc1", cut.Find("div.omni-barcode").GetAttribute("data-testid"));
    }

    [Fact]
    public void Renders_nothing_inside_when_value_empty()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, ""));

        var root = cut.Find("div.omni-barcode");
        Assert.NotNull(root);
        Assert.Empty(cut.FindAll("svg.omni-barcode-svg"));
    }

    [Fact]
    public void Renders_error_for_invalid_ean13()
    {
        // EAN-13 requires 12 or 13 digits; non-digit chars must error.
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Type, BarcodeType.Ean13)
            .Add(c => c.Value, "abc"));

        Assert.NotNull(cut.Find(".omni-barcode-error"));
    }

    // ─── ParameterState change-detection contract ─────────────────────────
    // After the manual OnParametersSet → ParameterState migration, encoding
    // must (a) run on first render, (b) NOT run when an unrelated parameter
    // (Class) changes, and (c) run again when Value changes. We probe the
    // encoded state via internal DisplayValue + the SVG <rect> count.

    [Fact]
    public void Encode_runs_on_initial_render()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC123"));

        // DisplayValue is populated by Recompute() through the ParameterState
        // change handler firing on first detect.
        Assert.Equal("ABC123", cut.Instance.DisplayValue);
        Assert.NotEmpty(cut.FindAll("svg.omni-barcode-svg rect"));
    }

    [Fact]
    public void Encode_does_NOT_run_when_unrelated_parameter_changes()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC123"));

        var rectsBefore = cut.FindAll("svg.omni-barcode-svg rect").Count;
        var displayBefore = cut.Instance.DisplayValue;

        // Re-render only changing Class — neither Value nor Type changed.
        cut.Render(p => p.Add(c => c.Class, "new-class"));

        var rectsAfter = cut.FindAll("svg.omni-barcode-svg rect").Count;
        Assert.Equal(rectsBefore, rectsAfter);
        // Reference equality: encoder didn't re-run, so DisplayValue is the
        // exact same string instance set on first detect.
        Assert.Same(displayBefore, cut.Instance.DisplayValue);
        Assert.Contains("new-class", cut.Find("div.omni-barcode").ClassName);
    }

    [Fact]
    public void Encode_runs_again_when_Value_changes()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC123"));

        var displayBefore = cut.Instance.DisplayValue;

        cut.Render(p => p.Add(c => c.Value, "DEF456"));

        Assert.NotEqual(displayBefore, cut.Instance.DisplayValue);
        Assert.Equal("DEF456", cut.Instance.DisplayValue);
    }

    [Fact]
    public void Encode_runs_again_when_Type_changes()
    {
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "12345670")
            .Add(c => c.Type, BarcodeType.Ean8));

        var rectsBefore = cut.FindAll("svg.omni-barcode-svg rect").Count;

        cut.Render(p => p.Add(c => c.Type, BarcodeType.Code128));

        // Different symbology produces a different module count.
        var rectsAfter = cut.FindAll("svg.omni-barcode-svg rect").Count;
        Assert.NotEqual(rectsBefore, rectsAfter);
    }

    [Fact]
    public void Width_and_Height_apply_to_root_style()
    {
        // Migration moved Width/Height from OnParametersSet (which mutated
        // Style) into a RootStyle getter. They should still appear in the
        // root element's inline style.
        var cut = Render<OmniBarcode>(p => p
            .Add(c => c.Value, "ABC")
            .Add(c => c.Width, "240px")
            .Add(c => c.Height, "80px"));

        var style = cut.Find("div.omni-barcode").GetAttribute("style") ?? string.Empty;
        Assert.Contains("width: 240px", style);
        Assert.Contains("height: 80px", style);
    }
}
