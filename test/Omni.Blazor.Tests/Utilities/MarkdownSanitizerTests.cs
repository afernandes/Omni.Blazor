namespace Omni.Blazor.Tests.Utilities;

/// <summary>
/// Regression tests for the hardened <c>MarkdownRenderer.SanitizeHtml</c> — closes the
/// unquoted-scheme, control-char-in-scheme and slash-separated-handler XSS bypasses that
/// the security audit reproduced, while keeping safe markup intact.
/// </summary>
public class MarkdownSanitizerTests
{
    [Theory]
    [InlineData("<a href=javascript:alert(1)>x</a>")]        // unquoted dangerous scheme
    [InlineData("<a href=\"jav\tascript:alert(1)\">x</a>")]  // tab smuggled into the scheme
    [InlineData("<a href = 'javascript:alert(1)'>x</a>")]    // spaced + single-quoted
    [InlineData("<a href=\"vbscript:msgbox(1)\">x</a>")]
    [InlineData("<a href=\"data:text/html;base64,x\">x</a>")]
    public void Neutralizes_dangerous_url_schemes(string html)
    {
        string outp = MarkdownRenderer.SanitizeHtml(html);
        Assert.DoesNotContain("javascript:", outp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("vbscript:", outp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("data:text/html", outp, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<img/src=x/onerror=alert(1)>")]  // slash-separated handler
    [InlineData("<img src=x onerror=alert(1)>")]  // space-separated handler
    [InlineData("<div onclick=\"steal()\">y</div>")]
    public void Strips_inline_event_handlers(string html)
    {
        string outp = MarkdownRenderer.SanitizeHtml(html);
        Assert.DoesNotContain("onerror", outp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onclick", outp, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("<iframe src=evil></iframe>")]
    [InlineData("<svg onload=alert(1)>")]
    public void Removes_dangerous_elements(string html)
    {
        string outp = MarkdownRenderer.SanitizeHtml(html);
        Assert.DoesNotContain("<script", outp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<iframe", outp, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<svg", outp, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Keeps_safe_markup_and_urls()
    {
        string outp = MarkdownRenderer.SanitizeHtml("<a href=\"https://example.com\">ok</a> <b>bold</b>");
        Assert.Contains("https://example.com", outp);
        Assert.Contains("<b>bold</b>", outp);
    }
}
