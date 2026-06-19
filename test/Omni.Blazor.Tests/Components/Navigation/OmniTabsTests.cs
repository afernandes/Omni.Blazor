using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Navigation;

public class OmniTabsTests : TestContextBase
{
    [Fact]
    public void Renders_root_with_omni_tabs_class()
    {
        var cut = Render<OmniTabs>();
        var root = cut.Find(".omni-tabs");
        Assert.NotNull(root);
        Assert.NotNull(cut.Find(".omni-tabs-bar"));
        Assert.NotNull(cut.Find(".omni-tabs-body"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniTabs>(p => p.Add(c => c.Class, "my-tabs"));
        Assert.Contains("my-tabs", cut.Find(".omni-tabs").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniTabs>(p => p.Add(c => c.Style, "background: red"));
        Assert.Equal("background: red", cut.Find(".omni-tabs").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniTabs>(p => p
            .AddUnmatched("data-testid", "tabs"));
        Assert.Equal("tabs", cut.Find(".omni-tabs").GetAttribute("data-testid"));
    }

    [Fact]
    public void Renders_tab_buttons_for_each_registered_item()
    {
        var cut = Render<OmniTabs>(p => p
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
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        Assert.Contains("omni-active", cut.FindAll(".omni-tab")[0].ClassName);
    }

    [Fact]
    public void Click_activates_target_tab()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        cut.FindAll(".omni-tab")[1].Click();

        Assert.DoesNotContain("omni-active", cut.FindAll(".omni-tab")[0].ClassName);
        Assert.Contains("omni-active", cut.FindAll(".omni-tab")[1].ClassName);
    }

    [Fact]
    public void Tab_bar_has_tablist_role()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One")));

        Assert.Equal("tablist", cut.Find(".omni-tabs-bar").GetAttribute("role"));
    }

    [Fact]
    public void Each_tab_button_has_tab_role()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        var buttons = cut.FindAll(".omni-tab");
        Assert.All(buttons, b => Assert.Equal("tab", b.GetAttribute("role")));
    }

    [Fact]
    public void Active_body_has_tabpanel_role()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One")));

        Assert.Equal("tabpanel", cut.Find(".omni-tabs-body").GetAttribute("role"));
    }

    [Fact]
    public void Aria_selected_is_true_only_on_active_tab()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        var buttons = cut.FindAll(".omni-tab");
        Assert.Equal("true", buttons[0].GetAttribute("aria-selected"));
        Assert.Equal("false", buttons[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Aria_selected_follows_active_tab_after_click()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        cut.FindAll(".omni-tab")[1].Click();

        var buttons = cut.FindAll(".omni-tab");
        Assert.Equal("false", buttons[0].GetAttribute("aria-selected"));
        Assert.Equal("true", buttons[1].GetAttribute("aria-selected"));
    }

    [Fact]
    public void Active_panel_is_labelled_by_active_tab()
    {
        var cut = Render<OmniTabs>(p => p
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "One"))
            .AddChildContent<OmniTabItem>(t => t.Add(c => c.Title, "Two")));

        var activeTab = cut.FindAll(".omni-tab")[0];
        var panel = cut.Find(".omni-tabs-body");

        var tabId = activeTab.GetAttribute("id");
        Assert.False(string.IsNullOrEmpty(tabId));
        Assert.Equal(tabId, panel.GetAttribute("aria-labelledby"));
        Assert.Equal(activeTab.GetAttribute("aria-controls"), panel.GetAttribute("id"));
    }
}
