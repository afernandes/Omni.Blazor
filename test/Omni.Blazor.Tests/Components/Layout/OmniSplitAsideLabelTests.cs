using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniSplitAsideLabel"/>: small uppercase
/// label used inside an <c>OmniSplitView</c> aside. Accepts either Text or
/// ChildContent.
/// </summary>
public class OmniSplitAsideLabelTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_base_class()
    {
        var cut = Render<OmniSplitAsideLabel>(p => p.Add(c => c.Text, "GROUP"));

        var div = cut.Find("div");
        Assert.Contains("omni-split-aside-label", div.ClassName);
        Assert.Contains("GROUP", div.TextContent);
    }

    [Fact]
    public void ChildContent_takes_precedence_over_Text()
    {
        var cut = Render<OmniSplitAsideLabel>(p => p
            .Add(c => c.Text, "FROM_TEXT")
            .AddChildContent("<span data-testid=\"slot\">FROM_CHILD</span>"));

        var div = cut.Find("div");
        Assert.NotNull(cut.Find("[data-testid='slot']"));
        Assert.Contains("FROM_CHILD", div.TextContent);
        Assert.DoesNotContain("FROM_TEXT", div.TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniSplitAsideLabel>(p => p
            .Add(c => c.Class, "custom-cls")
            .Add(c => c.Text, "X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniSplitAsideLabel>(p => p
            .Add(c => c.Style, "letter-spacing: 1px")
            .Add(c => c.Text, "X"));

        Assert.Equal("letter-spacing: 1px", cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniSplitAsideLabel>(p => p
            .AddUnmatched("data-testid", "label")
            .AddUnmatched("aria-label", "Group label")
            .Add(c => c.Text, "X"));

        var div = cut.Find("div");
        Assert.Equal("label", div.GetAttribute("data-testid"));
        Assert.Equal("Group label", div.GetAttribute("aria-label"));
    }
}
