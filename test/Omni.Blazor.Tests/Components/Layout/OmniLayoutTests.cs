using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Layout;

/// <summary>
/// Behavioural contract for <see cref="OmniLayout"/>: the shell container that
/// emits the .omni-layout grid + optional skip-to-content link.
/// </summary>
public class OmniLayoutTests : TestContextBase
{
    [Fact]
    public void Renders_grid_container_with_base_class()
    {
        var cut = Render<OmniLayout>(p => p.AddChildContent("<div data-testid=\"slot\">body</div>"));

        var root = cut.Find(".omni-layout");
        Assert.Equal("DIV", root.TagName);
        Assert.Contains("omni-layout", root.ClassName);
        // Skip link renders by default (WCAG).
        Assert.NotNull(cut.Find("a.omni-skip-link"));
    }

    [Fact]
    public void SkipToContent_false_omits_skip_link()
    {
        var cut = Render<OmniLayout>(p => p
            .Add(c => c.SkipToContent, false)
            .AddChildContent("body"));

        Assert.Empty(cut.FindAll(".omni-skip-link"));
    }

    [Fact]
    public void Skip_link_uses_default_label_and_target()
    {
        var cut = Render<OmniLayout>();

        var link = cut.Find("a.omni-skip-link");
        Assert.Equal("#main", link.GetAttribute("href"));
        Assert.Contains("Pular para o conteúdo", link.TextContent);
    }

    [Fact]
    public void Skip_link_honours_custom_label_and_target()
    {
        var cut = Render<OmniLayout>(p => p
            .Add(c => c.SkipLabel, "Skip to body")
            .Add(c => c.SkipTarget, "#content"));

        var link = cut.Find("a.omni-skip-link");
        Assert.Equal("#content", link.GetAttribute("href"));
        Assert.Contains("Skip to body", link.TextContent);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniLayout>(p => p
            .Add(c => c.Class, "custom-cls")
            .AddChildContent("X"));

        Assert.Contains("custom-cls", cut.Find(".omni-layout").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniLayout>(p => p
            .Add(c => c.Style, "min-height: 100vh")
            .AddChildContent("X"));

        Assert.Equal("min-height: 100vh", cut.Find(".omni-layout").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniLayout>(p => p
            .AddUnmatched("data-testid", "layout")
            .AddUnmatched("aria-label", "App shell")
            .AddChildContent("X"));

        var root = cut.Find(".omni-layout");
        Assert.Equal("layout", root.GetAttribute("data-testid"));
        Assert.Equal("App shell", root.GetAttribute("aria-label"));
    }

    [Fact]
    public void Drawer_registry_starts_empty()
    {
        var cut = Render<OmniLayout>(p => p.AddChildContent("body"));
        Assert.Empty(cut.Instance.Drawers);
    }
}
