namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniPanelMenuItem"/>: a leaf item with
/// a <c>Path</c> renders as <c>&lt;a&gt;</c>, a leaf without one renders as
/// <c>&lt;button&gt;</c>, and an item with children renders a toggle
/// <c>&lt;button&gt;</c>. The outer wrapper carries the <c>data-label</c>,
/// optional <c>data-badge</c>, and the Class/Style/Attributes from the
/// consumer.
/// </summary>
public class OmniPanelMenuItemTests : TestContextBase
{
    [Fact]
    public void Leaf_with_Path_renders_anchor_with_href()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")));

        var anchor = cut.Find(".omni-panel-menu-item-wrap > a");
        Assert.Equal("/", anchor.GetAttribute("href"));
        Assert.Contains("Home", anchor.TextContent);
    }

    [Fact]
    public void Leaf_without_Path_renders_button_and_fires_OnClick()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Action")
            .Add(i => i.OnClick, EventCallback.Factory.Create(this, () => clicks++))));

        var btn = cut.Find(".omni-panel-menu-item-wrap > button");
        btn.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Item_with_children_renders_toggle_button()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Group")
            .AddChildContent<OmniPanelMenuItem>(d => d
                .Add(i => i.Text, "Child")
                .Add(i => i.Path, "/c"))));

        // The wrapper has both omni-has-children and the toggle button.
        var wrap = cut.Find(".omni-panel-menu-item-wrap");
        Assert.Contains("omni-has-children", wrap.ClassName);
        var toggle = cut.Find(".omni-panel-menu-item-wrap > button.omni-panel-menu-item");
        Assert.NotNull(toggle);
    }

    [Fact]
    public void Toggle_button_expands_children_on_click()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Group")
            .AddChildContent<OmniPanelMenuItem>(d => d
                .Add(i => i.Text, "Child")
                .Add(i => i.Path, "/c"))));

        // Initially collapsed.
        Assert.Empty(cut.FindAll(".omni-panel-menu-children"));

        cut.Find("button.omni-panel-menu-item").Click();

        // Now expanded.
        Assert.NotNull(cut.Find(".omni-panel-menu-children"));
    }

    [Fact]
    public void Count_renders_count_pill()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Inbox")
            .Add(i => i.Path, "/inbox")
            .Add(i => i.Count, 7)));

        var pill = cut.Find(".omni-panel-menu-count");
        Assert.Equal("7", pill.TextContent);
    }

    [Fact]
    public void CountText_renders_string_pill_when_Count_absent()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Beta")
            .Add(i => i.Path, "/beta")
            .Add(i => i.CountText, "NEW")));

        var pill = cut.Find(".omni-panel-menu-count");
        Assert.Equal("NEW", pill.TextContent);
    }

    [Theory]
    [InlineData(MenuMetaKind.Accent, "omni-meta-accent")]
    [InlineData(MenuMetaKind.Good,   "omni-meta-good")]
    [InlineData(MenuMetaKind.Warn,   "omni-meta-warn")]
    [InlineData(MenuMetaKind.Danger, "omni-meta-danger")]
    public void Count_pill_applies_CountKind_modifier(MenuMetaKind kind, string expectedClass)
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "X")
            .Add(i => i.Path, "/x")
            .Add(i => i.Count, 1)
            .Add(i => i.CountKind, kind)));

        Assert.Contains(expectedClass, cut.Find(".omni-panel-menu-count").ClassName);
    }

    [Fact]
    public void Wrapper_has_data_label_attribute()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")));

        Assert.Equal("Home", cut.Find(".omni-panel-menu-item-wrap").GetAttribute("data-label"));
    }

    [Fact]
    public void Disabled_sets_disabled_attribute_on_button()
    {
        // The C# HandleClickAsync doesn't short-circuit on Disabled — that's
        // the browser's job via the disabled attribute. The contract we
        // assert here is: when Disabled=true, the rendered button has the
        // attribute, so the real browser will block the click.
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Action")
            .Add(i => i.Disabled, true)));

        var btn = cut.Find(".omni-panel-menu-item-wrap > button");
        Assert.True(btn.HasAttribute("disabled"));
    }

    [Fact]
    public void Class_parameter_appends_to_outer_wrapper()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")
            .Add(i => i.Class, "custom-pmi")));

        Assert.Contains("custom-pmi", cut.Find(".omni-panel-menu-item-wrap").ClassName);
    }

    [Fact]
    public void Style_parameter_forwards_to_outer_wrapper()
    {
        var cut = RenderComponent<OmniPanelMenu>(p => p.AddChildContent<OmniPanelMenuItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")
            .Add(i => i.Style, "opacity: 0.8")));

        Assert.Equal("opacity: 0.8", cut.Find(".omni-panel-menu-item-wrap").GetAttribute("style"));
    }
}
