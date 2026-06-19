using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Ai;

public class OmniSuggestionChipsTests : TestContextBase
{
    private static readonly string[] Three = ["Summarize", "Explain", "Translate"];

    [Fact]
    public void Renders_a_chip_per_suggestion_in_a_group()
    {
        var cut = Render<OmniSuggestionChips>(p => p.Add(c => c.Suggestions, Three));

        var root = cut.Find("div.omni-suggestion-chips");
        Assert.Equal("group", root.GetAttribute("role"));
        Assert.Equal("Suggestions", root.GetAttribute("aria-label"));
        Assert.Equal(3, cut.FindAll("button.omni-chip").Count);
        Assert.Contains("Explain", cut.FindAll("button.omni-chip")[1].TextContent);
    }

    [Fact]
    public void OnSelect_fires_with_clicked_suggestion()
    {
        string? selected = null;
        var cut = Render<OmniSuggestionChips>(p => p
            .Add(c => c.Suggestions, Three)
            .Add(c => c.OnSelect, s => selected = s));

        cut.FindAll("button.omni-chip")[2].Click();
        Assert.Equal("Translate", selected);
    }

    [Fact]
    public void Custom_AriaLabel_is_applied()
    {
        var cut = Render<OmniSuggestionChips>(p => p
            .Add(c => c.Suggestions, Three).Add(c => c.AriaLabel, "Follow-ups"));
        Assert.Equal("Follow-ups", cut.Find("div.omni-suggestion-chips").GetAttribute("aria-label"));
    }

    [Fact]
    public void Renders_ChildContent_chips()
    {
        var cut = Render<OmniSuggestionChips>(p => p
            .AddChildContent("<button class=\"omni-chip custom\">Manual</button>"));
        Assert.NotNull(cut.Find("button.custom"));
    }

    [Fact]
    public void Appends_Class_Style_and_Attributes()
    {
        var cut = Render<OmniSuggestionChips>(p => p
            .Add(c => c.Suggestions, Three)
            .Add(c => c.Class, "cc")
            .Add(c => c.Style, "gap: 4px")
            .AddUnmatched("data-testid", "chips"));

        var root = cut.Find("div.omni-suggestion-chips");
        Assert.Contains("cc", root.ClassName);
        Assert.Equal("gap: 4px", root.GetAttribute("style"));
        Assert.Equal("chips", root.GetAttribute("data-testid"));
    }
}
