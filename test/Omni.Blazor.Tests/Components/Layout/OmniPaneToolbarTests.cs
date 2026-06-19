using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniPaneToolbar"/>: free-form toolbar
/// band typically below an <c>OmniPaneHeader</c>.
/// </summary>
public class OmniPaneToolbarTests : TestContextBase
{
    [Fact]
    public void Renders_default_toolbar_root()
    {
        var cut = Render<OmniPaneToolbar>(p => p.AddChildContent("body"));

        var root = cut.Find(".omni-pane-toolbar");
        Assert.Equal("DIV", root.TagName);
        Assert.Contains("body", root.TextContent);
    }

    [Fact]
    public void Renders_children_inside_root()
    {
        var cut = Render<OmniPaneToolbar>(p => p.AddChildContent("<input data-testid='search'/>"));

        Assert.NotNull(cut.Find(".omni-pane-toolbar [data-testid='search']"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniPaneToolbar>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find(".omni-pane-toolbar").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniPaneToolbar>(p => p
            .Add(c => c.Style, "gap: 8px")
            .AddChildContent("X"));

        Assert.Equal("gap: 8px", cut.Find(".omni-pane-toolbar").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniPaneToolbar>(p => p
            .AddUnmatched("data-testid", "pt")
            .AddUnmatched("aria-label", "Toolbar")
            .AddChildContent("X"));

        var root = cut.Find(".omni-pane-toolbar");
        Assert.Equal("pt", root.GetAttribute("data-testid"));
        Assert.Equal("Toolbar", root.GetAttribute("aria-label"));
    }
}
