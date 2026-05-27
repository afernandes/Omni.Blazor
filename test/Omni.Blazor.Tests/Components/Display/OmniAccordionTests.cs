using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniAccordion"/>: mode, child rendering,
/// and the cross-cutting Class/Style/Attributes splat from <see cref="OmniComponent"/>.
/// </summary>
public class OmniAccordionTests : TestContextBase
{
    [Fact]
    public void Renders_default_root_with_base_class()
    {
        var cut = RenderComponent<OmniAccordion>(p => p.AddChildContent("<div>content</div>"));

        var root = cut.Find("div.omni-accordion");
        Assert.NotNull(root);
        Assert.Contains("omni-accordion", root.ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniAccordion>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("x"));

        Assert.Contains("custom-cls", cut.Find("div.omni-accordion").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniAccordion>(p => p
            .Add(c => c.Style, "margin: 12px")
            .AddChildContent("x"));

        Assert.Equal("margin: 12px", cut.Find("div.omni-accordion").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniAccordion>(p => p
            .AddUnmatched("data-testid", "acc")
            .AddUnmatched("aria-label", "Settings")
            .AddChildContent("x"));

        var root = cut.Find("div.omni-accordion");
        Assert.Equal("acc", root.GetAttribute("data-testid"));
        Assert.Equal("Settings", root.GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(AccordionMode.Single)]
    [InlineData(AccordionMode.Multi)]
    public void Accepts_mode_parameter(AccordionMode mode)
    {
        // Mode is internal behavioural — just verify it renders without error.
        var cut = RenderComponent<OmniAccordion>(p => p
            .Add(c => c.Mode, mode)
            .AddChildContent("x"));

        Assert.NotNull(cut.Find("div.omni-accordion"));
    }
}
