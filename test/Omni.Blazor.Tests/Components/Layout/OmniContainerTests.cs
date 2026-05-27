using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniContainer"/>: centred wrapper with
/// responsive max-width, optional fluid mode and gutters toggle.
/// </summary>
public class OmniContainerTests : TestContextBase
{
    [Fact]
    public void Renders_default_container_with_lg_maxwidth()
    {
        var cut = RenderComponent<OmniContainer>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-container", div.ClassName);
        Assert.Contains("omni-container-lg", div.ClassName);
        Assert.Contains("body", div.TextContent);
    }

    [Theory]
    [InlineData(ContainerMaxWidth.Sm,  "omni-container-sm")]
    [InlineData(ContainerMaxWidth.Md,  "omni-container-md")]
    [InlineData(ContainerMaxWidth.Lg,  "omni-container-lg")]
    [InlineData(ContainerMaxWidth.Xl,  "omni-container-xl")]
    [InlineData(ContainerMaxWidth.Xxl, "omni-container-xxl")]
    [InlineData(ContainerMaxWidth.Full,"omni-container-full")]
    public void Applies_maxwidth_modifier(ContainerMaxWidth max, string expected)
    {
        var cut = RenderComponent<OmniContainer>(p => p
            .Add(c => c.MaxWidth, max)
            .AddChildContent("X"));

        Assert.Contains(expected, cut.Find("div").ClassName);
    }

    [Fact]
    public void Fluid_forces_full_modifier()
    {
        var cut = RenderComponent<OmniContainer>(p => p
            .Add(c => c.MaxWidth, ContainerMaxWidth.Md)
            .Add(c => c.Fluid, true)
            .AddChildContent("X"));

        // Fluid overrides MaxWidth — should always emit -full.
        Assert.Contains("omni-container-full", cut.Find("div").ClassName);
    }

    [Fact]
    public void Gutters_false_adds_no_gutters_modifier()
    {
        var cut = RenderComponent<OmniContainer>(p => p
            .Add(c => c.Gutters, false)
            .AddChildContent("X"));

        Assert.Contains("omni-container-no-gutters", cut.Find("div").ClassName);
    }

    [Fact]
    public void Default_Gutters_true_omits_modifier()
    {
        var cut = RenderComponent<OmniContainer>(p => p.AddChildContent("X"));

        Assert.DoesNotContain("omni-container-no-gutters", cut.Find("div").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniContainer>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniContainer>(p => p
            .Add(c => c.Style, "padding: 0")
            .AddChildContent("X"));

        Assert.Equal("padding: 0", cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniContainer>(p => p
            .AddUnmatched("data-testid", "container")
            .AddUnmatched("aria-label", "Wrap")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("container", div.GetAttribute("data-testid"));
        Assert.Equal("Wrap", div.GetAttribute("aria-label"));
    }
}
