using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniAccordionItem"/>: header, toggle,
/// disabled state, and the cross-cutting Class/Style/Attributes splat.
/// </summary>
public class OmniAccordionItemTests : TestContextBase
{
    [Fact]
    public void Renders_default_with_base_class_and_title()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .Add(c => c.Title, "Section A")
            .AddChildContent("body"));

        var root = cut.Find("div.omni-accordion-item");
        Assert.Contains("omni-accordion-item", root.ClassName);
        Assert.Contains("Section A", cut.Markup);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .Add(c => c.Class, "my-item")
            .Add(c => c.Title, "T"));

        Assert.Contains("my-item", cut.Find("div.omni-accordion-item").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .Add(c => c.Style, "padding: 8px")
            .Add(c => c.Title, "T"));

        Assert.Equal("padding: 8px", cut.Find("div.omni-accordion-item").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .AddUnmatched("data-testid", "item-a")
            .Add(c => c.Title, "T"));

        Assert.Equal("item-a", cut.Find("div.omni-accordion-item").GetAttribute("data-testid"));
    }

    [Fact]
    public void Expanded_adds_modifier_class()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .Add(c => c.Expanded, true)
            .Add(c => c.Title, "T"));

        Assert.Contains("omni-expanded", cut.Find("div.omni-accordion-item").ClassName);
    }

    [Fact]
    public void Disabled_adds_modifier_class_and_disables_button()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Title, "T"));

        var root = cut.Find("div.omni-accordion-item");
        Assert.Contains("omni-disabled", root.ClassName);
        Assert.True(cut.Find("button.omni-accordion-header").HasAttribute("disabled"));
    }

    // ── ParameterState: Expanded-driven recompute fires only on Expanded changes ──

    [Fact]
    public void Initial_recompute_fires_on_first_render()
    {
        var cut = Render<OmniAccordionItem>(p => p.Add(c => c.Title, "T"));
        Assert.Equal(1, cut.Instance.RecomputeCount);
    }

    [Fact]
    public void Recompute_does_not_fire_when_unrelated_params_change()
    {
        var cut = Render<OmniAccordionItem>(p => p.Add(c => c.Title, "T"));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p
            .Add(c => c.Title, "Other")
            .Add(c => c.Icon, "star")
            .Add(c => c.Class, "newcls")
            .Add(c => c.Style, "color:red")
            .AddUnmatched("data-foo", "bar"));

        Assert.Equal(baseline, cut.Instance.RecomputeCount);
        // DOM: new title rendered.
        Assert.Contains("Other", cut.Markup);
    }

    [Fact]
    public void Recompute_fires_when_Expanded_param_changes_and_DOM_reflects_it()
    {
        var cut = Render<OmniAccordionItem>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Expanded, false));

        var baseline = cut.Instance.RecomputeCount;
        cut.Render(p => p.Add(c => c.Expanded, true));

        Assert.Equal(baseline + 1, cut.Instance.RecomputeCount);
        Assert.Contains("omni-expanded", cut.Find("div.omni-accordion-item").ClassName);
    }
}
