using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Ai;

public class OmniThinkingBlockTests : TestContextBase
{
    [Fact]
    public void Collapsed_by_default_hides_body()
    {
        var cut = Render<OmniThinkingBlock>(p => p.AddChildContent("step 1"));

        Assert.Empty(cut.FindAll(".omni-thinking-body"));
        Assert.Equal("false", cut.Find("button.omni-thinking-toggle").GetAttribute("aria-expanded"));
        Assert.DoesNotContain("omni-expanded", cut.Find("div.omni-thinking").ClassName);
    }

    [Fact]
    public void Title_defaults_to_Reasoning_and_is_overridable()
    {
        var cut = Render<OmniThinkingBlock>(p => p.AddChildContent("x"));
        Assert.Contains("Reasoning", cut.Find(".omni-thinking-title").TextContent);

        cut.Render(p => p.Add(c => c.Title, "Thinking").AddChildContent("x"));
        Assert.Contains("Thinking", cut.Find(".omni-thinking-title").TextContent);
    }

    [Fact]
    public void Toggle_expands_and_reveals_body()
    {
        var cut = Render<OmniThinkingBlock>(p => p.AddChildContent("the chain of thought"));

        cut.Find("button.omni-thinking-toggle").Click();

        Assert.Equal("true", cut.Find("button.omni-thinking-toggle").GetAttribute("aria-expanded"));
        Assert.Contains("the chain of thought", cut.Find(".omni-thinking-body").TextContent);
        Assert.Contains("omni-expanded", cut.Find("div.omni-thinking").ClassName);
    }

    [Fact]
    public void ExpandedChanged_fires_with_new_state()
    {
        bool? state = null;
        var cut = Render<OmniThinkingBlock>(p => p
            .Add(c => c.ExpandedChanged, v => state = v)
            .AddChildContent("x"));

        cut.Find("button.omni-thinking-toggle").Click();
        Assert.True(state);
    }

    [Fact]
    public void Expanded_true_initially_shows_body()
    {
        var cut = Render<OmniThinkingBlock>(p => p.Add(c => c.Expanded, true).AddChildContent("visible"));
        Assert.Contains("visible", cut.Find(".omni-thinking-body").TextContent);
    }

    [Fact]
    public void Recompute_does_not_fire_on_Class_change()
    {
        var cut = Render<OmniThinkingBlock>(p => p.AddChildContent("x"));
        int before = cut.Instance.RecomputeCount;

        cut.Render(p => p.Add(c => c.Class, "cc").AddChildContent("x"));

        Assert.Equal(before, cut.Instance.RecomputeCount);
        Assert.Contains("cc", cut.Find("div.omni-thinking").ClassName);
    }

    [Fact]
    public void Appends_Style_and_Attributes()
    {
        var cut = Render<OmniThinkingBlock>(p => p
            .Add(c => c.Style, "margin: 8px")
            .AddUnmatched("data-testid", "think")
            .AddChildContent("x"));

        var root = cut.Find("div.omni-thinking");
        Assert.Equal("margin: 8px", root.GetAttribute("style"));
        Assert.Equal("think", root.GetAttribute("data-testid"));
    }
}
