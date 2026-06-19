using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniProgress"/>: bar vs segmented,
/// variants, sizes, custom color, cross-cutting splat.
/// </summary>
public class OmniProgressTests : TestContextBase
{
    [Fact]
    public void Renders_bar_by_default()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 40));

        var root = cut.Find("div.omni-progress");
        Assert.Contains("omni-progress", root.ClassName);
        Assert.NotNull(cut.Find(".omni-progress-bar"));
    }

    [Fact]
    public void Bar_width_reflects_clamped_value()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 50)
            .Add(c => c.Max, 100));

        var inner = cut.Find(".omni-progress-bar");
        var style = inner.GetAttribute("style") ?? string.Empty;
        Assert.Contains("width: 50%", style);
    }

    [Fact]
    public void Custom_Color_applies_to_bar_style()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 30)
            .Add(c => c.Color, "#ff0000"));

        var style = cut.Find(".omni-progress-bar").GetAttribute("style") ?? string.Empty;
        Assert.Contains("background: #ff0000", style);
    }

    [Theory]
    [InlineData(BadgeVariant.Good,   "omni-progress-good")]
    [InlineData(BadgeVariant.Warn,   "omni-progress-warn")]
    [InlineData(BadgeVariant.Danger, "omni-progress-danger")]
    public void Applies_variant_modifier(BadgeVariant variant, string expected)
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 10)
            .Add(c => c.Variant, variant));

        Assert.Contains(expected, cut.Find("div.omni-progress").ClassName);
    }

    [Fact]
    public void Segmented_mode_renders_cells()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Segments, 4)
            .Add(c => c.Active, 2));

        var cells = cut.FindAll(".omni-progress-seg-cell");
        Assert.Equal(4, cells.Count);
        Assert.Empty(cut.FindAll(".omni-progress-bar"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root_bar()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 30)
            .Add(c => c.Class, "my-prog"));

        Assert.Contains("my-prog", cut.Find("div.omni-progress").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root_bar()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 30)
            .Add(c => c.Style, "margin-top: 8px"));

        Assert.Equal("margin-top: 8px", cut.Find("div.omni-progress").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root_bar()
    {
        var cut = Render<OmniProgress>(p => p
            .Add(c => c.Value, 30)
            .AddUnmatched("data-testid", "p1"));

        Assert.Equal("p1", cut.Find("div.omni-progress").GetAttribute("data-testid"));
    }
}
