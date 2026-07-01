using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniMarkdown"/>: block + inline rendering,
/// the cross-cutting splat, and — critically — XSS safety (text/HTML escaping and
/// URL scheme allow-listing).
/// </summary>
public class OmniMarkdownTests : TestContextBase
{
    private IRenderedComponent<OmniMarkdown> Render(string text, bool allowHtml = false)
        => Render<OmniMarkdown>(p => p
            .Add(c => c.Text, text)
            .Add(c => c.AllowHtml, allowHtml));

    // ─── Cross-cutting ────────────────────────────────────────────────────

    [Fact]
    public void Renders_root_with_base_class()
    {
        var cut = Render("hi");
        Assert.Contains("omni-markdown", cut.Find("div.omni-markdown").ClassName);
    }

    [Fact]
    public void Appends_Class_and_splats_attributes()
    {
        var cut = Render<OmniMarkdown>(p => p
            .Add(c => c.Text, "hi")
            .Add(c => c.Class, "doc")
            .AddUnmatched("data-testid", "md1"));
        var root = cut.Find("div.omni-markdown");
        Assert.Contains("doc", root.ClassName);
        Assert.Equal("md1", root.GetAttribute("data-testid"));
    }

    // ─── Block + inline rendering ─────────────────────────────────────────

    [Theory]
    [InlineData("# H1", "h1", "H1")]
    [InlineData("### H3", "h3", "H3")]
    public void Renders_headings(string md, string tag, string expected)
    {
        var cut = Render(md);
        Assert.Equal(expected, cut.Find(tag).TextContent);
    }

    [Fact]
    public void Renders_emphasis_strong_strike_code()
    {
        var cut = Render("a **b** *c* ~~d~~ `e`");
        Assert.Equal("b", cut.Find("strong").TextContent);
        Assert.Equal("c", cut.Find("em").TextContent);
        Assert.Equal("d", cut.Find("del").TextContent);
        Assert.Equal("e", cut.Find("code").TextContent);
    }

    [Fact]
    public void Renders_unordered_and_nested_lists()
    {
        var cut = Render("- a\n- b\n  - b1");
        Assert.NotNull(cut.Find("ul"));
        Assert.True(cut.FindAll("ul ul li").Count >= 1); // nested item
        Assert.Contains(cut.FindAll("li"), li => li.TextContent.Contains("b1"));
    }

    [Fact]
    public void Renders_ordered_list_with_start()
    {
        var cut = Render("3. three\n4. four");
        var ol = cut.Find("ol");
        Assert.Equal("3", ol.GetAttribute("start"));
        Assert.Equal(2, cut.FindAll("ol li").Count);
    }

    [Fact]
    public void Renders_fenced_code_with_language_class()
    {
        var cut = Render("```csharp\nvar x = 1;\n```");
        var code = cut.Find("pre code");
        Assert.Contains("language-csharp", code.ClassName);
        Assert.Contains("var x = 1;", code.TextContent);
    }

    [Fact]
    public void Renders_blockquote_and_hr()
    {
        var cut = Render("> quote\n\n---");
        Assert.Contains("quote", cut.Find("blockquote").TextContent);
        Assert.NotNull(cut.Find("hr"));
    }

    [Fact]
    public void Renders_table_with_alignment()
    {
        var cut = Render("| Name | Qty |\n|:--|--:|\n| Pizza | 3 |");
        Assert.NotNull(cut.Find("table.omni-md-table"));
        Assert.Equal(2, cut.FindAll("thead th").Count);
        var rows = cut.FindAll("tbody tr");
        Assert.Single(rows);
        var qtyHeader = cut.FindAll("thead th")[1];
        Assert.Contains("right", qtyHeader.GetAttribute("style") ?? "");
    }

    [Fact]
    public void Renders_safe_link_with_href()
    {
        var cut = Render("[site](https://example.com)");
        var a = cut.Find("a");
        Assert.Equal("https://example.com", a.GetAttribute("href"));
        Assert.Equal("site", a.TextContent);
    }

    [Fact]
    public void Renders_image()
    {
        var cut = Render("![alt text](https://example.com/x.png)");
        var img = cut.Find("img");
        Assert.Equal("https://example.com/x.png", img.GetAttribute("src"));
        Assert.Equal("alt text", img.GetAttribute("alt"));
    }

    // ─── XSS safety ───────────────────────────────────────────────────────

    [Fact]
    public void Escapes_raw_html_by_default()
    {
        var cut = Render("<script>alert(1)</script>");
        Assert.Empty(cut.FindAll("script"));            // not executed as an element
        Assert.Contains("alert(1)", cut.Markup);         // shown as literal text
        Assert.Contains("&lt;script&gt;", cut.Markup);
    }

    [Fact]
    public void Drops_javascript_url_in_links()
    {
        var cut = Render("[click](javascript:alert(1))");
        Assert.Empty(cut.FindAll("a"));                  // no anchor emitted
        Assert.DoesNotContain("javascript:", cut.Markup);
        Assert.Contains("click", cut.Markup);            // link text preserved
    }

    [Fact]
    public void Drops_javascript_url_in_images()
    {
        var cut = Render("![x](javascript:alert(1))");
        Assert.Empty(cut.FindAll("img"));
        Assert.DoesNotContain("javascript:", cut.Markup);
    }

    [Fact]
    public void AllowHtml_keeps_safe_tags_but_strips_scripts_and_handlers()
    {
        var cut = Render("<b>bold</b><script>alert(1)</script><span onclick=\"x()\">y</span>", allowHtml: true);
        Assert.Equal("bold", cut.Find("b").TextContent);  // safe tag kept
        Assert.Empty(cut.FindAll("script"));               // script stripped
        var span = cut.Find("span");
        Assert.Null(span.GetAttribute("onclick"));         // event handler stripped
    }

    // ─── Performance: parse memoization ───────────────────────────────────

    [Fact]
    public void Memoizes_the_parse_across_unrelated_rerenders()
    {
        var cut = Render<OmniMarkdown>(p => p.Add(c => c.Text, "**hi**"));
        int afterFirst = cut.Instance.ParseCount;
        Assert.True(afterFirst >= 1);

        // Re-render changing only Class → the source is unchanged, so NO reparse.
        cut.Render(p => p.Add(c => c.Text, "**hi**").Add(c => c.Class, "extra"));
        Assert.Equal(afterFirst, cut.Instance.ParseCount);

        // Re-render changing the Text → must reparse.
        cut.Render(p => p.Add(c => c.Text, "**bye**"));
        Assert.True(cut.Instance.ParseCount > afterFirst);
    }
}
