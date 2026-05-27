using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Inputs;

/// <summary>
/// Behavioural contract for <see cref="OmniRating"/>: star rendering, size
/// modifiers, click to select, readonly/disabled flags, and the cross-cutting splat.
/// </summary>
public class OmniRatingTests : TestContextBase
{
    [Fact]
    public void Renders_five_stars_by_default()
    {
        var cut = RenderComponent<OmniRating>();
        Assert.Equal(5, cut.FindAll("span.omni-rating-star").Count);
    }

    [Fact]
    public void Honors_Max_count()
    {
        var cut = RenderComponent<OmniRating>(p => p.Add(c => c.Max, 10));
        Assert.Equal(10, cut.FindAll("span.omni-rating-star").Count);
    }

    [Theory]
    [InlineData(ComponentSize.Sm, "omni-rating-sm")]
    [InlineData(ComponentSize.Md, "omni-rating-md")]
    [InlineData(ComponentSize.Lg, "omni-rating-lg")]
    [InlineData(ComponentSize.Xl, "omni-rating-xl")]
    public void Applies_size_modifier(ComponentSize size, string expected)
    {
        var cut = RenderComponent<OmniRating>(p => p.Add(c => c.Size, size));
        Assert.Contains(expected, cut.Find("div.omni-rating").ClassName);
    }

    [Fact]
    public void Click_on_third_star_sets_value_to_three()
    {
        double captured = 0;
        var cut = RenderComponent<OmniRating>(p => p
            .Add(c => c.Value, 0)
            .Add(c => c.ValueChanged, v => captured = v));

        // First mouseover on the star fills _hoverValue; click commits it.
        cut.FindAll("span.omni-rating-star")[2].MouseOver();
        cut.FindAll("span.omni-rating-star")[2].Click();
        Assert.Equal(3.0, captured);
    }

    [Fact]
    public void ReadOnly_applies_modifier_class()
    {
        var cut = RenderComponent<OmniRating>(p => p.Add(c => c.ReadOnly, true));
        Assert.Contains("omni-rating-readonly", cut.Find("div.omni-rating").ClassName);
    }

    [Fact]
    public void Disabled_applies_modifier_class()
    {
        var cut = RenderComponent<OmniRating>(p => p.Add(c => c.Disabled, true));
        Assert.Contains("omni-rating-disabled", cut.Find("div.omni-rating").ClassName);
    }

    [Fact]
    public void ShowValue_renders_numeric_label()
    {
        var cut = RenderComponent<OmniRating>(p => p
            .Add(c => c.Value, 4.0)
            .Add(c => c.ShowValue, true));

        Assert.Contains("4", cut.Find("span.omni-rating-value").TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniRating>(p => p.Add(c => c.Class, "custom-cls"));
        Assert.Contains("custom-cls", cut.Find("div.omni-rating").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniRating>(p => p.Add(c => c.Style, "color: gold"));
        Assert.Equal("color: gold", cut.Find("div.omni-rating").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniRating>(p => p
            .AddUnmatched("data-testid", "rate"));

        Assert.Equal("rate", cut.Find("div.omni-rating").GetAttribute("data-testid"));
    }
}
