using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniQtyStepper"/>: increment/decrement,
/// Min/Max clamping, size modifiers, and the cross-cutting splat.
/// </summary>
public class OmniQtyStepperTests : TestContextBase
{
    [Fact]
    public void Renders_three_children_div_with_base_class()
    {
        var cut = Render<OmniQtyStepper>(p => p.Add(c => c.Value, 1));

        var root = cut.Find("div.omni-qty-stepper");
        Assert.NotNull(root);
        Assert.Equal(2, cut.FindAll("button").Count);
        Assert.Contains("1", cut.Find("span.omni-qty-stepper-v").TextContent);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-qty-stepper-sm")]
    [InlineData(ComponentSize.Lg, "omni-qty-stepper-lg")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.Size, size));

        Assert.Contains(expected, cut.Find("div.omni-qty-stepper").ClassName);
    }

    [Fact]
    public void Increment_button_raises_ValueChanged()
    {
        var captured = 0;
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("button")[1].Click();
        Assert.Equal(2, captured);
    }

    [Fact]
    public void Decrement_button_raises_ValueChanged()
    {
        var captured = 0;
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 3)
            .Add(c => c.ValueChanged, v => captured = v));

        cut.FindAll("button")[0].Click();
        Assert.Equal(2, captured);
    }

    [Fact]
    public void Decrement_disabled_at_Min()
    {
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.Min, 1));

        Assert.True(cut.FindAll("button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Increment_disabled_at_Max()
    {
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 5)
            .Add(c => c.Max, 5));

        Assert.True(cut.FindAll("button")[1].HasAttribute("disabled"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("div.omni-qty-stepper").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 1)
            .Add(c => c.Style, "margin: 4px"));

        Assert.Equal("margin: 4px", cut.Find("div.omni-qty-stepper").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniQtyStepper>(p => p
            .Add(c => c.Value, 1)
            .AddUnmatched("data-testid", "qty"));

        Assert.Equal("qty", cut.Find("div.omni-qty-stepper").GetAttribute("data-testid"));
    }
}
