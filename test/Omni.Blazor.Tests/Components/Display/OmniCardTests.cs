using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniCard"/>: title/subtitle/header,
/// variants, clickable, and cross-cutting splat.
/// </summary>
public class OmniCardTests : TestContextBase
{
    [Fact]
    public void Renders_default_root_with_base_class()
    {
        var cut = Render<OmniCard>(p => p.AddChildContent("body"));

        var root = cut.Find("div.omni-card");
        Assert.Contains("omni-card", root.ClassName);
    }

    [Fact]
    public void Renders_title_and_subtitle_in_header()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Title, "Hi")
            .Add(c => c.Subtitle, "World"));

        Assert.Contains("Hi", cut.Find(".omni-card-title").TextContent);
        Assert.Contains("World", cut.Find(".omni-card-sub").TextContent);
    }

    [Fact]
    public void Title_uses_h3_by_default_for_backward_compatibility()
    {
        var cut = Render<OmniCard>(p => p.Add(c => c.Title, "Section"));

        Assert.Equal("H3", cut.Find(".omni-card-title").TagName);
    }

    [Theory]
    [InlineData(1, "H1")]
    [InlineData(2, "H2")]
    [InlineData(4, "H4")]
    [InlineData(5, "H5")]
    [InlineData(6, "H6")]
    public void HeadingLevel_controls_the_semantic_title_element(int level, string expectedTag)
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Title, "Section")
            .Add(c => c.HeadingLevel, level));

        Assert.Equal(expectedTag, cut.Find(".omni-card-title").TagName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void HeadingLevel_rejects_values_outside_the_html_heading_range(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniCard>(p => p
            .Add(c => c.Title, "Section")
            .Add(c => c.HeadingLevel, level)));
    }

    [Fact]
    public void HeadingLevel_is_not_validated_when_HeaderContent_replaces_the_title()
    {
        // The parameter is documented as ignored here, and an ignored parameter that can
        // still throw is a trap: a consumer who never renders a heading pays for its value.
        var cut = Render<OmniCard>(p => p
            .Add(c => c.HeadingLevel, 0)
            .Add(c => c.HeaderContent, (RenderFragment)(b => b.AddMarkupContent(0, "<span>Custom</span>"))));

        Assert.Empty(cut.FindAll(".omni-card-title"));
        Assert.Contains("Custom", cut.Markup);
    }

    [Fact]
    public void HeadingLevel_is_not_validated_when_there_is_no_title_to_render()
    {
        var cut = Render<OmniCard>(p => p.Add(c => c.HeadingLevel, 7));

        Assert.Empty(cut.FindAll(".omni-card-title"));
    }

    [Fact]
    public void Elevated_adds_modifier()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Elevated, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-elevated", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void Flat_adds_modifier()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Flat, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-flat", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void Clickable_adds_modifier()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Clickable, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-clickable", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void OnClick_fires_when_card_clicked()
    {
        var fired = 0;
        var cut = Render<OmniCard>(p => p
            .Add(c => c.OnClick, (MouseEventArgs _) => fired++)
            .AddChildContent("x"));

        cut.Find("div.omni-card").Click();
        Assert.Equal(1, fired);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Class, "my-card")
            .AddChildContent("x"));

        Assert.Contains("my-card", cut.Find("div.omni-card").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniCard>(p => p
            .Add(c => c.Style, "max-width: 320px")
            .AddChildContent("x"));

        Assert.Equal("max-width: 320px", cut.Find("div.omni-card").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniCard>(p => p
            .AddUnmatched("data-testid", "card1")
            .AddChildContent("x"));

        Assert.Equal("card1", cut.Find("div.omni-card").GetAttribute("data-testid"));
    }
}
