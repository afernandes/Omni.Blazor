using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Marketing;

/// <summary>
/// Behavioural contract for <see cref="OmniHero"/>: eyebrow/title/subtitle/CTA
/// slots, markup title, and cross-cutting splat.
/// </summary>
public class OmniHeroTests : TestContextBase
{
    [Fact]
    public void Renders_root_section_with_base_class()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "Welcome"));

        var root = cut.Find("section.omni-hero");
        Assert.Contains("omni-hero", root.ClassName);
    }

    [Fact]
    public void Renders_title_with_markup()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "Hello <em>World</em>"));

        var title = cut.Find(".omni-hero-title");
        Assert.Contains("Hello", title.TextContent);
        Assert.NotNull(title.QuerySelector("em"));
    }

    [Fact]
    public void Renders_subtitle_paragraph()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Subtitle, "We build things."));

        Assert.Contains("We build things.", cut.Find("p.omni-hero-sub").TextContent);
    }

    [Fact]
    public void Renders_string_eyebrow_via_OmniEyebrow()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Eyebrow, "New")
            .Add(c => c.Title, "T"));

        Assert.Contains("New", cut.Find("span.omni-eyebrow").TextContent);
    }

    [Fact]
    public void EyebrowContent_overrides_string_Eyebrow()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Eyebrow, "ignored")
            .Add(c => c.EyebrowContent, b => b.AddMarkupContent(0, "<span class='custom-eye'>custom</span>"))
            .Add(c => c.Title, "T"));

        Assert.NotNull(cut.Find("span.custom-eye"));
        Assert.Empty(cut.FindAll("span.omni-eyebrow"));
    }

    [Fact]
    public void Renders_cta_row_when_CtaContent_set()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.CtaContent, b => b.AddMarkupContent(0, "<button>Go</button>")));

        Assert.NotNull(cut.Find(".omni-hero-cta-row button"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Class, "hero-fancy"));

        Assert.Contains("hero-fancy", cut.Find("section.omni-hero").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "T")
            .Add(c => c.Style, "background: red"));

        Assert.Equal("background: red", cut.Find("section.omni-hero").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniHero>(p => p
            .Add(c => c.Title, "T")
            .AddUnmatched("data-testid", "hero1"));

        Assert.Equal("hero1", cut.Find("section.omni-hero").GetAttribute("data-testid"));
    }
}
