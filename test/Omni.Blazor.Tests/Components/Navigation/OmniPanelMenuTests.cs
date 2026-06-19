namespace Omni.Blazor.Tests.Components.Navigation;

/// <summary>
/// Behavioural contract for <see cref="OmniPanelMenu"/>: renders as a
/// <c>&lt;nav&gt;</c> with the <c>omni-panel-menu</c> class, cascades itself
/// (so child items can detect the menu), and forwards Class/Style/Attributes
/// to the root.
/// </summary>
public class OmniPanelMenuTests : TestContextBase
{
    [Fact]
    public void Renders_nav_with_panel_menu_class()
    {
        var cut = Render<OmniPanelMenu>();

        var nav = cut.Find("nav");
        Assert.Contains("omni-panel-menu", nav.ClassName);
    }

    [Fact]
    public void Renders_child_content_inside_nav()
    {
        var cut = Render<OmniPanelMenu>(p => p
            .AddChildContent("<span class=\"probe\">x</span>"));

        Assert.NotNull(cut.Find("nav .probe"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniPanelMenu>(p => p.Add(c => c.Class, "custom-pm"));

        Assert.Contains("custom-pm", cut.Find("nav").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniPanelMenu>(p => p.Add(c => c.Style, "padding: 8px"));

        Assert.Equal("padding: 8px", cut.Find("nav").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniPanelMenu>(p => p
            .AddUnmatched("data-testid", "pm")
            .AddUnmatched("aria-label", "Sidebar"));

        var nav = cut.Find("nav");
        Assert.Equal("pm", nav.GetAttribute("data-testid"));
        Assert.Equal("Sidebar", nav.GetAttribute("aria-label"));
    }

    [Fact]
    public void SectionLabel_parameter_is_captured()
    {
        var cut = Render<OmniPanelMenu>(p => p.Add(c => c.SectionLabel, "WORKSPACE"));

        Assert.Equal("WORKSPACE", cut.Instance.SectionLabel);
    }
}
