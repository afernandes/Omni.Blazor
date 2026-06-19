using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniCol"/>: 12-column grid cell with
/// responsive Size overrides per breakpoint.
/// </summary>
public class OmniColTests : TestContextBase
{
    [Fact]
    public void Renders_default_col_with_size_12()
    {
        var cut = Render<OmniCol>(p => p.AddChildContent("body"));

        var col = cut.Find("div");
        Assert.Contains("omni-col", col.ClassName);
        Assert.Contains("omni-col-12", col.ClassName);
        Assert.Contains("body", col.TextContent);
    }

    [Theory]
    [InlineData(1, "omni-col-1")]
    [InlineData(6, "omni-col-6")]
    [InlineData(12, "omni-col-12")]
    public void Applies_size_modifier(int size, string expectedClass)
    {
        var cut = Render<OmniCol>(p => p
            .Add(c => c.Size, size)
            .AddChildContent("X"));

        Assert.Contains(expectedClass, cut.Find("div").ClassName);
    }

    [Fact]
    public void Adds_responsive_size_overrides()
    {
        var cut = Render<OmniCol>(p => p
            .Add(c => c.SizeSm, 8)
            .Add(c => c.SizeMd, 6)
            .Add(c => c.SizeLg, 4)
            .AddChildContent("X"));

        var className = cut.Find("div").ClassName;
        Assert.Contains("omni-col-sm-8", className);
        Assert.Contains("omni-col-md-6", className);
        Assert.Contains("omni-col-lg-4", className);
    }

    [Fact]
    public void Omits_responsive_overrides_when_null()
    {
        var cut = Render<OmniCol>(p => p.AddChildContent("X"));

        var className = cut.Find("div").ClassName;
        Assert.DoesNotContain("omni-col-sm-", className);
        Assert.DoesNotContain("omni-col-md-", className);
        Assert.DoesNotContain("omni-col-lg-", className);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniCol>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniCol>(p => p
            .Add(c => c.Style, "padding: 8px")
            .AddChildContent("X"));

        Assert.Equal("padding: 8px", cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniCol>(p => p
            .AddUnmatched("data-testid", "col")
            .AddUnmatched("aria-label", "Grid cell")
            .AddChildContent("X"));

        var col = cut.Find("div");
        Assert.Equal("col", col.GetAttribute("data-testid"));
        Assert.Equal("Grid cell", col.GetAttribute("aria-label"));
    }
}
