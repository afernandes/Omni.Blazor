namespace Omni.Blazor.Tests.Components.Overlay;

/// <summary>
/// Behavioural contract for <see cref="OmniMenuSeparator"/>: renders a single
/// divider <c>div</c> with role separator and forwards Class/Style/Attributes.
/// </summary>
public class OmniMenuSeparatorTests : TestContextBase
{
    [Fact]
    public void Renders_div_with_separator_class_and_role()
    {
        var cut = RenderComponent<OmniMenuSeparator>();

        var root = cut.Find("div");
        Assert.Contains("omni-menu-sep", root.ClassName);
        Assert.Equal("separator", root.GetAttribute("role"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniMenuSeparator>(p => p
            .Add(c => c.Class, "custom-sep"));

        Assert.Contains("custom-sep", cut.Find("div").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniMenuSeparator>(p => p
            .Add(c => c.Style, "margin: 4px 0"));

        Assert.Equal("margin: 4px 0", cut.Find("div").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniMenuSeparator>(p => p
            .AddUnmatched("data-testid", "sep")
            .AddUnmatched("aria-orientation", "horizontal"));

        var root = cut.Find("div");
        Assert.Equal("sep", root.GetAttribute("data-testid"));
        Assert.Equal("horizontal", root.GetAttribute("aria-orientation"));
    }
}
