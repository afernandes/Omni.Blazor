using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniLink"/>: variants, href/target,
/// disabled rendering, onclick, and cross-cutting splat.
/// </summary>
public class OmniLinkTests : TestContextBase
{
    [Fact]
    public void Renders_anchor_with_href_and_text()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Href, "/about")
            .Add(c => c.Text, "About"));

        var a = cut.Find("a.omni-link");
        Assert.Equal("/about", a.GetAttribute("href"));
        Assert.Contains("About", a.TextContent);
    }

    [Theory]
    [InlineData(LinkVariant.Default, "omni-link-default")]
    [InlineData(LinkVariant.Muted,   "omni-link-muted")]
    [InlineData(LinkVariant.Danger,  "omni-link-danger")]
    public void Applies_variant_class(LinkVariant variant, string expected)
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Variant, variant));

        Assert.Contains(expected, cut.Find(".omni-link").ClassName);
    }

    [Fact]
    public void Target_blank_adds_noopener_rel()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Href, "https://x")
            .Add(c => c.Target, "_blank")
            .Add(c => c.Text, "X"));

        Assert.Equal("noopener noreferrer", cut.Find("a.omni-link").GetAttribute("rel"));
    }

    [Fact]
    public void Disabled_renders_as_span_not_anchor()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Disabled, true)
            .Add(c => c.Text, "X"));

        Assert.NotNull(cut.Find("span.omni-link"));
        Assert.Empty(cut.FindAll("a.omni-link"));
    }

    [Fact]
    public void Underline_adds_modifier_class()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Underline, true)
            .Add(c => c.Text, "X"));

        Assert.Contains("omni-link-underline", cut.Find(".omni-link").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Class, "my-link"));

        Assert.Contains("my-link", cut.Find(".omni-link").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.Style, "font-size: 16px"));

        Assert.Equal("font-size: 16px", cut.Find(".omni-link").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Text, "X")
            .AddUnmatched("data-testid", "lnk1"));

        Assert.Equal("lnk1", cut.Find(".omni-link").GetAttribute("data-testid"));
    }

    [Fact]
    public void OnClick_fires_when_enabled()
    {
        var fired = 0;
        var cut = Render<OmniLink>(p => p
            .Add(c => c.Text, "X")
            .Add(c => c.OnClick, (MouseEventArgs _) => fired++));

        cut.Find("a.omni-link").Click();
        Assert.Equal(1, fired);
    }
}
