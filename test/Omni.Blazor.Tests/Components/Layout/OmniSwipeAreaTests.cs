using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniSwipeArea"/>: pointer gesture detection
/// surface. Tests focus on the rendered shell + class composition; actual pointer
/// callback behaviour is covered by browser tests.
/// </summary>
public class OmniSwipeAreaTests : TestContextBase
{
    [Fact]
    public void Renders_default_swipearea_root()
    {
        var cut = Render<OmniSwipeArea>(p => p.AddChildContent("body"));

        var div = cut.Find("div.omni-swipearea");
        Assert.Contains("omni-swipearea", div.ClassName);
        Assert.DoesNotContain("omni-swipearea-live", div.ClassName);
        Assert.Contains("body", div.TextContent);
    }

    [Theory]
    [InlineData(SwipeAreaLiveTransform.X,    true)]
    [InlineData(SwipeAreaLiveTransform.Y,    true)]
    [InlineData(SwipeAreaLiveTransform.Both, true)]
    [InlineData(SwipeAreaLiveTransform.None, false)]
    public void LiveTransform_applies_modifier_class(SwipeAreaLiveTransform mode, bool expected)
    {
        var cut = Render<OmniSwipeArea>(p => p
            .Add(c => c.LiveTransform, mode)
            .AddChildContent("X"));

        if (expected)
            Assert.Contains("omni-swipearea-live", cut.Find("div.omni-swipearea").ClassName);
        else
            Assert.DoesNotContain("omni-swipearea-live", cut.Find("div.omni-swipearea").ClassName);
    }

    [Fact]
    public void Cancel_resets_internal_state()
    {
        // Cancel is a public method that just resets a few private fields; ensure
        // it doesn't throw when called on a fresh component.
        var cut = Render<OmniSwipeArea>(p => p.AddChildContent("X"));
        cut.Instance.Cancel();
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniSwipeArea>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div.omni-swipearea").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniSwipeArea>(p => p
            .Add(c => c.Style, "touch-action: none")
            .AddChildContent("X"));

        Assert.Equal("touch-action: none", cut.Find("div.omni-swipearea").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniSwipeArea>(p => p
            .AddUnmatched("data-testid", "sa")
            .AddUnmatched("aria-label", "Swipe")
            .AddChildContent("X"));

        var div = cut.Find("div.omni-swipearea");
        Assert.Equal("sa", div.GetAttribute("data-testid"));
        Assert.Equal("Swipe", div.GetAttribute("aria-label"));
    }
}
