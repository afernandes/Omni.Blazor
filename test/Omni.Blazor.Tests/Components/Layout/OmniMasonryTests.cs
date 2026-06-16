using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniMasonry"/>: a pure-CSS multicolumn cascade
/// (Pinterest-style). Column count via Columns (+ responsive) or intrinsic via MinColumnWidth.
/// </summary>
public class OmniMasonryTests : TestContextBase
{
    [Fact]
    public void Renders_with_default_columns_and_gap()
    {
        var cut = RenderComponent<OmniMasonry>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-masonry", div.ClassName);
        var style = div.GetAttribute("style") ?? "";
        Assert.Contains("--omni-masonry-cols: 3", style);
        Assert.Contains("--omni-masonry-gap: 16px", style);
    }

    [Fact]
    public void Emits_columns_and_gap_variables()
    {
        var cut = RenderComponent<OmniMasonry>(p => p
            .Add(c => c.Columns, 5)
            .Add(c => c.Gap, 8)
            .AddChildContent("X"));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-masonry-cols: 5", style);
        Assert.Contains("--omni-masonry-gap: 8px", style);
    }

    [Fact]
    public void Responsive_columns_emit_breakpoint_variables()
    {
        var cut = RenderComponent<OmniMasonry>(p => p
            .Add(c => c.Columns, 2)
            .Add(c => c.ColumnsSm, 3)
            .Add(c => c.ColumnsMd, 4)
            .Add(c => c.ColumnsLg, 5)
            .AddChildContent("X"));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-masonry-cols-sm: 3", style);
        Assert.Contains("--omni-masonry-cols-md: 4", style);
        Assert.Contains("--omni-masonry-cols-lg: 5", style);
    }

    [Fact]
    public void MinColumnWidth_enables_autocol_and_omits_columns()
    {
        var cut = RenderComponent<OmniMasonry>(p => p
            .Add(c => c.MinColumnWidth, 240)
            .Add(c => c.Columns, 5)        // ignored in autocol mode
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Contains("omni-masonry-autocol", div.ClassName);
        var style = div.GetAttribute("style") ?? "";
        Assert.Contains("--omni-masonry-min: 240px", style);
        Assert.DoesNotContain("--omni-masonry-cols:", style);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMasonry>(p => p.Add(c => c.Class, "custom-cls").AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMasonry>(p => p.Add(c => c.Style, "margin: 4px").AddChildContent("X"));

        Assert.Contains("margin: 4px", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMasonry>(p => p
            .AddUnmatched("data-testid", "m")
            .AddUnmatched("aria-label", "Galeria")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("m", div.GetAttribute("data-testid"));
        Assert.Equal("Galeria", div.GetAttribute("aria-label"));
    }
}
