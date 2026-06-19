namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniMenu"/>: panel container with
/// role menu that hosts <c>OmniMenuItem</c>s, forwarding Class/Style/Attributes.
/// </summary>
public class OmniMenuTests : TestContextBase
{
    [Fact]
    public void Renders_div_with_menu_class_and_role()
    {
        var cut = Render<OmniMenu>(p => p.AddChildContent("<span>x</span>"));

        var root = cut.Find("div");
        Assert.Contains("omni-menu", root.ClassName);
        Assert.Equal("menu", root.GetAttribute("role"));
    }

    [Fact]
    public void Renders_child_content()
    {
        var cut = Render<OmniMenu>(p => p
            .AddChildContent("<span class=\"probe\">probe</span>"));

        Assert.NotNull(cut.Find(".probe"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniMenu>(p => p
            .Add(c => c.Class, "custom-menu")
            .AddChildContent("x"));

        Assert.Contains("custom-menu", cut.Find("div.omni-menu").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniMenu>(p => p
            .Add(c => c.Style, "min-width: 240px")
            .AddChildContent("x"));

        Assert.Equal("min-width: 240px", cut.Find("div.omni-menu").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniMenu>(p => p
            .AddUnmatched("data-testid", "menu")
            .AddUnmatched("aria-label", "Actions")
            .AddChildContent("x"));

        var root = cut.Find("div.omni-menu");
        Assert.Equal("menu", root.GetAttribute("data-testid"));
        Assert.Equal("Actions", root.GetAttribute("aria-label"));
    }
}
