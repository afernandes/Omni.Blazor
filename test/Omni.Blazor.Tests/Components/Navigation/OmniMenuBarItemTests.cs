namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniMenuBarItem"/> at the root level
/// (inside a parent <see cref="OmniMenuBar"/>): renders as a link, button, or
/// trigger button depending on Path / dropdown content, applies
/// <c>omni-active</c> when the route matches, and forwards Class to the
/// outer <c>&lt;li&gt;</c>. Nested behaviour is exercised indirectly via the
/// menu bar render tree.
/// </summary>
public class OmniMenuBarItemTests : TestContextBase
{
    [Fact]
    public void Path_only_renders_anchor_with_href()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")));

        var anchor = cut.Find("li.omni-menubar-item > a");
        Assert.Equal("/", anchor.GetAttribute("href"));
        Assert.Contains("Home", anchor.TextContent);
        Assert.Equal("menuitem", anchor.GetAttribute("role"));
    }

    [Fact]
    public void No_Path_no_children_renders_button_and_fires_OnClick()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "Action")
            .Add(i => i.OnClick, EventCallback.Factory.Create(this, () => clicks++))));

        var btn = cut.Find("li.omni-menubar-item > button");
        btn.Click();
        Assert.Equal(1, clicks);
    }

    [Fact]
    public void Disabled_button_does_not_fire_OnClick()
    {
        var clicks = 0;
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "Action")
            .Add(i => i.Disabled, true)
            .Add(i => i.OnClick, EventCallback.Factory.Create(this, () => clicks++))));

        var btn = cut.Find("li.omni-menubar-item > button");
        Assert.True(btn.HasAttribute("disabled"));
        btn.Click();
        Assert.Equal(0, clicks);
    }

    [Fact]
    public void Children_renders_a_trigger_button_with_haspopup()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "File")
            .AddChildContent<OmniMenuBarItem>(d => d
                .Add(i => i.Text, "Open")
                .Add(i => i.Path, "/open"))));

        var trigger = cut.Find("li.omni-menubar-item > button");
        Assert.Equal("true", trigger.GetAttribute("aria-haspopup"));
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        // Caret icon present on the trigger.
        Assert.NotNull(cut.Find(".omni-menubar-caret"));
    }

    [Fact]
    public void Count_renders_count_badge()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "Inbox")
            .Add(i => i.Path, "/inbox")
            .Add(i => i.Count, 3)));

        var count = cut.Find(".omni-menubar-count");
        Assert.Equal("3", count.TextContent);
    }

    [Fact]
    public void Icon_renders_OmniIcon_in_link()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")
            .Add(i => i.Icon, "home")));

        Assert.NotNull(cut.Find(".omni-menubar-link .omni-icon"));
    }

    [Fact]
    public void Class_parameter_appends_to_outer_li()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.AddChildContent<OmniMenuBarItem>(c => c
            .Add(i => i.Text, "Home")
            .Add(i => i.Path, "/")
            .Add(i => i.Class, "custom-mi")));

        Assert.Contains("custom-mi", cut.Find("li.omni-menubar-item").ClassName);
    }
}
