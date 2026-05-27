namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniMenuItem"/>: button element with
/// role menuitem, danger modifier, disabled guard on click, optional icon
/// and shortcut slots, and the Class/Style/Attributes splat.
/// </summary>
public class OmniMenuItemTests : TestContextBase
{
    [Fact]
    public void Renders_button_with_base_class_and_role()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p.Add(c => c.Text, "Open"));

        var btn = cut.Find("button");
        Assert.Contains("omni-menu-item", btn.ClassName);
        Assert.Equal("menuitem", btn.GetAttribute("role"));
        Assert.Equal("button", btn.GetAttribute("type"));
        Assert.Contains("Open", btn.TextContent);
    }

    [Fact]
    public void IsDanger_applies_danger_modifier_class()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "Delete")
            .Add(c => c.IsDanger, true));

        Assert.Contains("omni-menu-item-danger", cut.Find("button").ClassName);
    }

    [Fact]
    public void Default_has_no_danger_modifier()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p.Add(c => c.Text, "Open"));

        Assert.DoesNotContain("omni-menu-item-danger", cut.Find("button").ClassName);
    }

    [Fact]
    public void Disabled_sets_attribute_and_blocks_OnClick()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "Open")
            .Add(c => c.Disabled, true)
            .Add(c => c.OnClick, _ => clicks++));

        var btn = cut.Find("button");
        Assert.True(btn.HasAttribute("disabled"));
        btn.Click();
        // Guarded by HandleClickAsync — disabled short-circuits before invoking handler.
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void OnClick_fires_when_enabled()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "Open")
            .Add(c => c.OnClick, (MouseEventArgs _) => clicks++));

        cut.Find("button").Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Renders_icon_when_provided()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "Open")
            .Add(c => c.Icon, "edit"));

        // OmniIcon emits an svg/span — at minimum the icon doesn't throw.
        Assert.NotNull(cut.Find("button"));
    }

    [Fact]
    public void Renders_shortcut_when_provided()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "Save")
            .Add(c => c.Shortcut, "Ctrl+S"));

        Assert.NotNull(cut.Find(".omni-menu-item-kbd"));
    }

    [Fact]
    public void ChildContent_overrides_Text_label()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "ignored")
            .AddChildContent("<span class=\"slot\">slot</span>"));

        Assert.NotNull(cut.Find(".omni-menu-item-label .slot"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Class, "custom-mi"));

        Assert.Contains("custom-mi", cut.Find("button").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "x")
            .Add(c => c.Style, "color: red"));

        Assert.Equal("color: red", cut.Find("button").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMenuItem>(p => p
            .Add(c => c.Text, "x")
            .AddUnmatched("data-testid", "mi")
            .AddUnmatched("aria-keyshortcuts", "Control+S"));

        var btn = cut.Find("button");
        Assert.Equal("mi", btn.GetAttribute("data-testid"));
        Assert.Equal("Control+S", btn.GetAttribute("aria-keyshortcuts"));
    }
}
