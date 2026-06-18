using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Ai;

public class OmniCitationTests : TestContextBase
{
    [Fact]
    public void Renders_bracketed_index_as_span_without_url()
    {
        var cut = RenderComponent<OmniCitation>(p => p.Add(c => c.Index, 3).Add(c => c.Title, "Docs"));

        var el = cut.Find(".omni-citation");
        Assert.Equal("SPAN", el.TagName);
        Assert.Equal("[3]", el.TextContent);
        Assert.Equal("Source 3: Docs", el.GetAttribute("aria-label"));
        Assert.Equal("Docs", el.GetAttribute("title"));
    }

    [Fact]
    public void Renders_link_opening_safely_when_url_set()
    {
        var cut = RenderComponent<OmniCitation>(p => p
            .Add(c => c.Index, 1).Add(c => c.Url, "https://example.com").Add(c => c.Title, "Example"));

        var a = cut.Find("a.omni-citation");
        Assert.Equal("https://example.com", a.GetAttribute("href"));
        Assert.Equal("_blank", a.GetAttribute("target"));
        Assert.Equal("noopener noreferrer", a.GetAttribute("rel"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    public void Falls_back_to_span_for_unsafe_url(string url)
    {
        var cut = RenderComponent<OmniCitation>(p => p.Add(c => c.Index, 1).Add(c => c.Url, url));

        var el = cut.Find(".omni-citation");
        Assert.Equal("SPAN", el.TagName);
        Assert.False(el.HasAttribute("href"));
    }

    [Fact]
    public void Renders_link_for_safe_relative_url()
    {
        var cut = RenderComponent<OmniCitation>(p => p.Add(c => c.Index, 1).Add(c => c.Url, "/docs/page"));

        var a = cut.Find("a.omni-citation");
        Assert.Equal("/docs/page", a.GetAttribute("href"));
    }

    [Fact]
    public void Tooltip_combines_title_and_snippet()
    {
        var cut = RenderComponent<OmniCitation>(p => p
            .Add(c => c.Index, 2).Add(c => c.Title, "Title").Add(c => c.Snippet, "Excerpt"));
        Assert.Equal("Title — Excerpt", cut.Find(".omni-citation").GetAttribute("title"));
    }

    [Fact]
    public void Text_overrides_the_bracketed_label()
    {
        var cut = RenderComponent<OmniCitation>(p => p.Add(c => c.Index, 4).Add(c => c.Text, "ref"));
        Assert.Equal("ref", cut.Find(".omni-citation").TextContent);
    }

    [Fact]
    public void Appends_Class_Style_and_Attributes()
    {
        var cut = RenderComponent<OmniCitation>(p => p
            .Add(c => c.Index, 1)
            .Add(c => c.Class, "cc")
            .Add(c => c.Style, "color: red")
            .AddUnmatched("data-testid", "cite"));

        var el = cut.Find(".omni-citation");
        Assert.Contains("cc", el.ClassName);
        Assert.Equal("color: red", el.GetAttribute("style"));
        Assert.Equal("cite", el.GetAttribute("data-testid"));
    }
}
