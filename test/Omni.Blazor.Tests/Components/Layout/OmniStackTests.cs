using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniStack"/>: flex container with
/// direction / gap / alignment / justification parameters.
/// </summary>
public class OmniStackTests : TestContextBase
{
    [Fact]
    public void Renders_default_column_stack()
    {
        var cut = Render<OmniStack>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-stack", div.ClassName);
        Assert.Contains("omni-stack-column", div.ClassName);
        Assert.Contains("omni-stack-align-stretch", div.ClassName);
        Assert.Contains("omni-stack-justify-start", div.ClassName);
        Assert.Contains("--omni-stack-gap: 8px", div.GetAttribute("style") ?? "");
    }

    [Theory]
    [InlineData(StackDirection.Row,    "omni-stack-row")]
    [InlineData(StackDirection.Column, "omni-stack-column")]
    public void Applies_direction_modifier(StackDirection dir, string expected)
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.Direction, dir)
            .AddChildContent("X"));

        Assert.Contains(expected, cut.Find("div").ClassName);
    }

    [Theory]
    [InlineData(StackAlign.Center,   "omni-stack-align-center")]
    [InlineData(StackAlign.End,      "omni-stack-align-end")]
    [InlineData(StackAlign.Baseline, "omni-stack-align-baseline")]
    public void Applies_align_items_modifier(StackAlign align, string expected)
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.AlignItems, align)
            .AddChildContent("X"));

        Assert.Contains(expected, cut.Find("div").ClassName);
    }

    [Theory]
    [InlineData(StackJustify.Between, "omni-stack-justify-between")]
    [InlineData(StackJustify.Center,  "omni-stack-justify-center")]
    [InlineData(StackJustify.Evenly,  "omni-stack-justify-evenly")]
    public void Applies_justify_modifier(StackJustify justify, string expected)
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.Justify, justify)
            .AddChildContent("X"));

        Assert.Contains(expected, cut.Find("div").ClassName);
    }

    [Fact]
    public void Wrap_applies_modifier_class()
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.Wrap, true)
            .AddChildContent("X"));

        Assert.Contains("omni-stack-wrap", cut.Find("div").ClassName);
    }

    [Fact]
    public void Responsive_direction_overrides_emit_breakpoint_classes()
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.DirectionSm, StackDirection.Row)
            .Add(c => c.DirectionMd, StackDirection.Column)
            .Add(c => c.DirectionLg, StackDirection.Row)
            .AddChildContent("X"));

        var className = cut.Find("div").ClassName;
        Assert.Contains("omni-stack-sm-row", className);
        Assert.Contains("omni-stack-md-column", className);
        Assert.Contains("omni-stack-lg-row", className);
    }

    [Fact]
    public void Custom_Gap_emits_css_variable()
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.Gap, 20)
            .AddChildContent("X"));

        Assert.Contains("--omni-stack-gap: 20px", cut.Find("div").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniStack>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniStack>(p => p
            .AddUnmatched("data-testid", "stack")
            .AddUnmatched("aria-label", "Stack")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("stack", div.GetAttribute("data-testid"));
        Assert.Equal("Stack", div.GetAttribute("aria-label"));
    }
}
