using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Navigation;

public class OmniTabsTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_omni_tabs_class()
    {
        var cut = RenderComponent<OmniTabs>();
        var root = cut.Find(".omni-tabs");
        Assert.NotNull(root);
        Assert.NotNull(cut.Find(".omni-tabs-bar"));
        Assert.NotNull(cut.Find(".omni-tabs-body"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniTabs>(p => p.Add(c => c.Class, "my-tabs"));
        Assert.Contains("my-tabs", cut.Find(".omni-tabs").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniTabs>(p => p.Add(c => c.Style, "background: red"));
        Assert.Equal("background: red", cut.Find(".omni-tabs").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniTabs>(p => p
            .AddUnmatched("data-testid", "tabs"));
        Assert.Equal("tabs", cut.Find(".omni-tabs").GetAttribute("data-testid"));
    }

    [Fact]
    public void Renders_tab_buttons_for_each_registered_item()
    {
        var cut = RenderComponent<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        var buttons = cut.FindAll(".omni-tab");
        Assert.Equal(2, buttons.Count);
        Assert.Contains("One", buttons[0].TextContent);
        Assert.Contains("Two", buttons[1].TextContent);
    }

    [Fact]
    public void First_registered_tab_is_active_by_default()
    {
        var cut = RenderComponent<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        Assert.Contains("omni-active", cut.FindAll(".omni-tab")[0].ClassName);
    }

    [Fact]
    public void Click_activates_target_tab()
    {
        var cut = RenderComponent<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        cut.FindAll(".omni-tab")[1].Click();

        Assert.DoesNotContain("omni-active", cut.FindAll(".omni-tab")[0].ClassName);
        Assert.Contains("omni-active", cut.FindAll(".omni-tab")[1].ClassName);
    }
}
