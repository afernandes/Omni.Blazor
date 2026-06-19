using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniBento"/>: a dense 2D Bento grid. Apple-style by
/// default (real gap, rounded tiles); column count via Columns or auto-fit via MinTileWidth.
/// </summary>
public class OmniBentoTests : TestContextBase
{
    [Fact]
    public void Renders_grid_with_default_columns_and_gap()
    {
        var cut = Render<OmniBento>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-bento", div.ClassName);
        var style = div.GetAttribute("style") ?? "";
        Assert.Contains("--omni-bento-cols: 12", style);
        Assert.Contains("--omni-bento-gap: 16px", style);
    }

    [Fact]
    public void Emits_columns_and_gap_variables()
    {
        var cut = Render<OmniBento>(p => p
            .Add(c => c.Columns, 4)
            .Add(c => c.Gap, 8)
            .AddChildContent("X"));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-bento-cols: 4", style);
        Assert.Contains("--omni-bento-gap: 8px", style);
    }

    [Fact]
    public void Responsive_columns_emit_breakpoint_variables()
    {
        var cut = Render<OmniBento>(p => p
            .Add(c => c.Columns, 12)
            .Add(c => c.ColumnsSm, 6)
            .Add(c => c.ColumnsMd, 4)
            .Add(c => c.ColumnsLg, 3)
            .AddChildContent("X"));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-bento-cols-sm: 6", style);
        Assert.Contains("--omni-bento-cols-md: 4", style);
        Assert.Contains("--omni-bento-cols-lg: 3", style);
    }

    [Fact]
    public void MinTileWidth_enables_autofit_and_omits_columns()
    {
        var cut = Render<OmniBento>(p => p
            .Add(c => c.MinTileWidth, 240)
            .Add(c => c.Columns, 4)        // ignored when auto-fit
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Contains("omni-bento-autofit", div.ClassName);
        var style = div.GetAttribute("style") ?? "";
        Assert.Contains("--omni-bento-min: 240px", style);
        Assert.DoesNotContain("--omni-bento-cols:", style);
    }

    [Fact]
    public void RowHeight_adds_rows_class_and_variable()
    {
        var cut = Render<OmniBento>(p => p
            .Add(c => c.RowHeight, 100)
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Contains("omni-bento-rows", div.ClassName);
        Assert.Contains("--omni-bento-row-h: 100px", div.GetAttribute("style") ?? "");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Square_toggles_modifier(bool square)
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.Square, square).AddChildContent("X"));

        if (square) Assert.Contains("omni-bento-square", cut.Find("div").ClassName);
        else Assert.DoesNotContain("omni-bento-square", cut.Find("div").ClassName);
    }

    [Fact]
    public void Connected_adds_modifier()
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.Connected, true).AddChildContent("X"));

        Assert.Contains("omni-bento-connected", cut.Find("div").ClassName);
    }

    [Fact]
    public void Dense_default_on_omits_sparse_and_false_adds_it()
    {
        var on = Render<OmniBento>(p => p.AddChildContent("X"));
        Assert.DoesNotContain("omni-bento-sparse", on.Find("div").ClassName);

        var off = Render<OmniBento>(p => p.Add(c => c.Dense, false).AddChildContent("X"));
        Assert.Contains("omni-bento-sparse", off.Find("div").ClassName);
    }

    [Fact]
    public void AlignItems_emits_alignment_class()
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.AlignItems, StackAlign.Center).AddChildContent("X"));

        Assert.Contains("omni-bento-align-center", cut.Find("div").ClassName);
    }

    [Fact]
    public void EqualRows_emits_class()
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.EqualRows, true).AddChildContent("X"));

        Assert.Contains("omni-bento-equal", cut.Find("div").ClassName);
    }

    [Fact]
    public void Container_wraps_in_query_container_and_marks_grid()
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.Container, true).AddChildContent("X"));

        var grid = cut.Find(".omni-bento-cq .omni-bento");   // wrapper > grid
        Assert.Contains("omni-bento-container", grid.ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.Class, "custom-cls").AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniBento>(p => p.Add(c => c.Style, "margin: 4px").AddChildContent("X"));

        Assert.Contains("margin: 4px", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniBento>(p => p
            .AddUnmatched("data-testid", "b")
            .AddUnmatched("aria-label", "Bento")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("b", div.GetAttribute("data-testid"));
        Assert.Equal("Bento", div.GetAttribute("aria-label"));
    }
}
