namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniMenuBar"/>: renders a <c>&lt;nav&gt;</c>
/// landmark with the correct trigger-mode modifier class, exposes an
/// <c>aria-label</c>, renders an optional <c>Aux</c> render-fragment to the
/// right, and forwards Class/Style/Attributes onto the root.
/// </summary>
public class OmniMenuBarTests : TestContextBase
{
    [Fact]
    public void Renders_nav_with_default_click_trigger_class()
    {
        var cut = RenderComponent<OmniMenuBar>();

        var nav = cut.Find("nav");
        Assert.Contains("omni-menubar", nav.ClassName);
        Assert.Contains("omni-menubar-click", nav.ClassName);
        Assert.Equal("Menu", nav.GetAttribute("aria-label"));
    }

    [Theory]
    [InlineData(MenuTrigger.Click, "omni-menubar-click")]
    [InlineData(MenuTrigger.Hover, "omni-menubar-hover")]
    public void Applies_trigger_modifier(MenuTrigger trigger, string expectedClass)
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.Add(c => c.Trigger, trigger));

        var nav = cut.Find("nav");
        Assert.Contains(expectedClass, nav.ClassName);
    }

    [Fact]
    public void Renders_menubar_list_with_role_menubar()
    {
        var cut = RenderComponent<OmniMenuBar>();

        var list = cut.Find("ul.omni-menubar-list");
        Assert.Equal("menubar", list.GetAttribute("role"));
    }

    [Fact]
    public void Aux_render_fragment_renders_when_provided()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p
            .Add(c => c.Aux, b => b.AddMarkupContent(0, "<button class=\"aux-probe\">Pro</button>")));

        var aux = cut.Find(".omni-menubar-aux");
        Assert.NotNull(aux.QuerySelector(".aux-probe"));
    }

    [Fact]
    public void No_Aux_omits_aux_container()
    {
        var cut = RenderComponent<OmniMenuBar>();

        Assert.Empty(cut.FindAll(".omni-menubar-aux"));
    }

    [Fact]
    public void Custom_AriaLabel_is_forwarded()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.Add(c => c.AriaLabel, "Main navigation"));

        Assert.Equal("Main navigation", cut.Find("nav").GetAttribute("aria-label"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.Add(c => c.Class, "custom-mb"));

        Assert.Contains("custom-mb", cut.Find("nav").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p.Add(c => c.Style, "background: #fff"));

        Assert.Equal("background: #fff", cut.Find("nav").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMenuBar>(p => p
            .AddUnmatched("data-testid", "mb")
            .AddUnmatched("id", "main-mb"));

        var nav = cut.Find("nav");
        Assert.Equal("mb", nav.GetAttribute("data-testid"));
        Assert.Equal("main-mb", nav.GetAttribute("id"));
    }
}
