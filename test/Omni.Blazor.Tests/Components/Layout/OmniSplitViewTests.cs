using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniSplitView"/>: master/detail layout
/// primitive with an aside + main section, responsive mobile overlay.
/// </summary>
public class OmniSplitViewTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_aside_and_main()
    {
        var cut = RenderComponent<OmniSplitView>(p => p
            .Add(c => c.AsideContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "aside");
                builder.CloseElement();
            })
            .Add(c => c.ChildContent, builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "data-testid", "main");
                builder.CloseElement();
            }));

        var root = cut.Find(".omni-split");
        Assert.NotNull(root);
        Assert.NotNull(cut.Find("aside.omni-split-aside [data-testid='aside']"));
        Assert.NotNull(cut.Find("section.omni-split-main [data-testid='main']"));
    }

    [Fact]
    public void AsideRight_applies_right_modifier()
    {
        var cut = RenderComponent<OmniSplitView>(p => p.Add(c => c.AsideRight, true));

        Assert.Contains("omni-split-aside-right", cut.Find(".omni-split").ClassName);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-split-aside-narrow")]
    [InlineData(ComponentSize.Lg, "omni-split-aside-wide")]
    public void AsideSize_applies_modifier(ComponentSize size, string expected)
    {
        var cut = RenderComponent<OmniSplitView>(p => p.Add(c => c.AsideSize, size));

        Assert.Contains(expected, cut.Find(".omni-split").ClassName);
    }

    [Fact]
    public void AsideSize_Md_emits_no_size_modifier()
    {
        var cut = RenderComponent<OmniSplitView>(p => p.Add(c => c.AsideSize, ComponentSize.Md));

        var className = cut.Find(".omni-split").ClassName;
        Assert.DoesNotContain("omni-split-aside-narrow", className);
        Assert.DoesNotContain("omni-split-aside-wide", className);
        Assert.DoesNotContain("omni-split-aside-custom", className);
    }

    [Fact]
    public void AsideWidth_emits_custom_modifier_and_css_variable()
    {
        var cut = RenderComponent<OmniSplitView>(p => p.Add(c => c.AsideWidth, "320px"));

        var root = cut.Find(".omni-split");
        Assert.Contains("omni-split-aside-custom", root.ClassName);
        Assert.Contains("--omni-aside-w: 320px", root.GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniSplitView>(p => p.Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find(".omni-split").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniSplitView>(p => p
            .AddUnmatched("data-testid", "sv")
            .AddUnmatched("aria-label", "Master detail"));

        var root = cut.Find(".omni-split");
        Assert.Equal("sv", root.GetAttribute("data-testid"));
        Assert.Equal("Master detail", root.GetAttribute("aria-label"));
    }
}
