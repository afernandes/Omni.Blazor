using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniAppBar"/>: renders a header element
/// with position/elevation/border modifiers + cross-cutting Class/Style/Attributes.
/// </summary>
public class OmniAppBarTests : TestContextBase
{
    [Fact]
    public void Renders_default_header_with_base_classes()
    {
        var cut = Render<OmniAppBar>(p => p.AddChildContent("Header"));

        var header = cut.Find("header");
        Assert.Contains("omni-header", header.ClassName);
        Assert.Contains("omni-appbar", header.ClassName);
        Assert.Contains("omni-appbar-top", header.ClassName);
        // Default Elevated=true and Bordered=true.
        Assert.Contains("omni-appbar-elevated", header.ClassName);
        Assert.Contains("omni-appbar-bordered", header.ClassName);
        Assert.Equal("top", header.GetAttribute("data-pos"));
        Assert.Contains("Header", header.TextContent);
    }

    [Theory]
    [InlineData(BarPosition.Top,    "omni-appbar-top",    "top")]
    [InlineData(BarPosition.Bottom, "omni-appbar-bottom", "bottom")]
    public void Applies_position_modifier(BarPosition pos, string expectedClass, string expectedAttr)
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.Position, pos)
            .AddChildContent("X"));

        var header = cut.Find("header");
        Assert.Contains(expectedClass, header.ClassName);
        Assert.Equal(expectedAttr, header.GetAttribute("data-pos"));
    }

    [Fact]
    public void Elevated_false_removes_elevated_class()
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.Elevated, false)
            .AddChildContent("X"));

        Assert.DoesNotContain("omni-appbar-elevated", cut.Find("header").ClassName);
    }

    [Fact]
    public void Bordered_false_removes_bordered_class()
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.Bordered, false)
            .AddChildContent("X"));

        Assert.DoesNotContain("omni-appbar-bordered", cut.Find("header").ClassName);
    }

    [Fact]
    public void HideOnScroll_applies_modifier_class()
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.HideOnScroll, true)
            .AddChildContent("X"));

        Assert.Contains("omni-appbar-hide-on-scroll", cut.Find("header").ClassName);
    }

    [Fact]
    public void ElevateOnScroll_disables_static_elevated_class()
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.ElevateOnScroll, true)
            .AddChildContent("X"));

        var header = cut.Find("header");
        Assert.Contains("omni-appbar-elevate-on-scroll", header.ClassName);
        // When ElevateOnScroll=true the static .omni-appbar-elevated must be off
        // (CSS swaps via data-scrolled attr).
        Assert.DoesNotContain("omni-appbar-elevated", header.ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("header").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniAppBar>(p => p
            .Add(c => c.Style, "background: red")
            .AddChildContent("X"));

        Assert.Equal("background: red", cut.Find("header").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniAppBar>(p => p
            .AddUnmatched("data-testid", "appbar")
            .AddUnmatched("aria-label", "Top bar")
            .AddChildContent("X"));

        var header = cut.Find("header");
        Assert.Equal("appbar", header.GetAttribute("data-testid"));
        Assert.Equal("Top bar", header.GetAttribute("aria-label"));
    }
}
