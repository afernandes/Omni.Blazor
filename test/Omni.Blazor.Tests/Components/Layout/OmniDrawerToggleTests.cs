using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniDrawerToggle"/>: hamburger button
/// that toggles a target <see cref="OmniDrawer"/>.
/// </summary>
public class OmniDrawerToggleTests : TestContextBase
{
    [Fact]
    public void Renders_button_with_orphan_class_when_no_target_resolved()
    {
        var cut = Render<OmniDrawerToggle>();

        var btn = cut.Find("button");
        Assert.Contains("omni-drawer-toggle", btn.ClassName);
        // No target resolved → orphan class.
        Assert.Contains("omni-drawer-toggle-orphan", btn.ClassName);
    }

    [Fact]
    public void Default_icon_and_title_are_set()
    {
        var cut = Render<OmniDrawerToggle>();

        var btn = cut.Find("button");
        Assert.Equal("Abrir menu", btn.GetAttribute("aria-label"));
        Assert.Equal("Abrir menu", btn.GetAttribute("title"));
    }

    [Fact]
    public void Custom_Title_and_Icon_are_applied()
    {
        var cut = Render<OmniDrawerToggle>(p => p
            .Add(c => c.Title, "Menu")
            .Add(c => c.Icon, "menu-2"));

        var btn = cut.Find("button");
        Assert.Equal("Menu", btn.GetAttribute("aria-label"));
        Assert.Equal("Menu", btn.GetAttribute("title"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniDrawerToggle>(p => p.Add(c => c.Class, "custom-cls"));

        Assert.Contains("custom-cls", cut.Find("button").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniDrawerToggle>(p => p.Add(c => c.Style, "color: red"));

        Assert.Equal("color: red", cut.Find("button").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniDrawerToggle>(p => p
            .AddUnmatched("data-testid", "dt"));

        Assert.Equal("dt", cut.Find("button").GetAttribute("data-testid"));
    }
}
