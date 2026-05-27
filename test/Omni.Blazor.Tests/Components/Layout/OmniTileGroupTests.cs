using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniTileGroup"/>: "lego" container that
/// groups children with shared 1px lines; supports column/row/grid layouts.
/// </summary>
public class OmniTileGroupTests : TestContextBase
{
    [Fact]
    public void Renders_default_column_group()
    {
        var cut = RenderComponent<OmniTileGroup>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-tile-group", div.ClassName);
        Assert.Contains("omni-tile-group-col", div.ClassName);
        // Default Direction=Column → no grid CSS variables in style.
        Assert.DoesNotContain("--omni-tile-cols", div.GetAttribute("style") ?? "");
    }

    [Theory]
    [InlineData(TileDirection.Column, "omni-tile-group-col")]
    [InlineData(TileDirection.Row,    "omni-tile-group-row")]
    [InlineData(TileDirection.Grid,   "omni-tile-group-grid")]
    public void Applies_direction_modifier(TileDirection dir, string expected)
    {
        var cut = RenderComponent<OmniTileGroup>(p => p
            .Add(c => c.Direction, dir)
            .AddChildContent("X"));

        Assert.Contains(expected, cut.Find("div").ClassName);
    }

    [Fact]
    public void Grid_emits_columns_css_variable()
    {
        var cut = RenderComponent<OmniTileGroup>(p => p
            .Add(c => c.Direction, TileDirection.Grid)
            .Add(c => c.Columns, 4)
            .AddChildContent("X"));

        Assert.Contains("--omni-tile-cols: 4", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Grid_responsive_columns_emit_breakpoint_variables()
    {
        var cut = RenderComponent<OmniTileGroup>(p => p
            .Add(c => c.Direction, TileDirection.Grid)
            .Add(c => c.Columns, 12)
            .Add(c => c.ColumnsSm, 6)
            .Add(c => c.ColumnsMd, 4)
            .Add(c => c.ColumnsLg, 3)
            .AddChildContent("X"));

        var style = cut.Find("div").GetAttribute("style") ?? "";
        Assert.Contains("--omni-tile-cols: 12", style);
        Assert.Contains("--omni-tile-cols-sm: 6", style);
        Assert.Contains("--omni-tile-cols-md: 4", style);
        Assert.Contains("--omni-tile-cols-lg: 3", style);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniTileGroup>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniTileGroup>(p => p
            .AddUnmatched("data-testid", "tg")
            .AddUnmatched("aria-label", "Tiles")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("tg", div.GetAttribute("data-testid"));
        Assert.Equal("Tiles", div.GetAttribute("aria-label"));
    }
}
