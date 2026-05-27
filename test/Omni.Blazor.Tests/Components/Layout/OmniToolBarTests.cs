using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniToolBar"/>: sub-bar below
/// <c>OmniAppBar</c>, with optional bottom border.
/// </summary>
public class OmniToolBarTests : TestContextBase
{
    [Fact]
    public void Renders_default_toolbar_with_bordered_class()
    {
        var cut = RenderComponent<OmniToolBar>(p => p.AddChildContent("body"));

        var div = cut.Find("div");
        Assert.Contains("omni-subheader", div.ClassName);
        Assert.Contains("omni-toolbar", div.ClassName);
        Assert.Contains("omni-toolbar-bordered", div.ClassName);
        Assert.Contains("body", div.TextContent);
    }

    [Fact]
    public void Bordered_false_removes_bordered_class()
    {
        var cut = RenderComponent<OmniToolBar>(p => p
            .Add(c => c.Bordered, false)
            .AddChildContent("X"));

        Assert.DoesNotContain("omni-toolbar-bordered", cut.Find("div").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniToolBar>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniToolBar>(p => p
            .Add(c => c.Style, "padding: 4px")
            .AddChildContent("X"));

        Assert.Equal("padding: 4px", cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniToolBar>(p => p
            .AddUnmatched("data-testid", "tb")
            .AddUnmatched("aria-label", "Toolbar")
            .AddChildContent("X"));

        var div = cut.Find("div");
        Assert.Equal("tb", div.GetAttribute("data-testid"));
        Assert.Equal("Toolbar", div.GetAttribute("aria-label"));
    }
}
