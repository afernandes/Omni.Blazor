namespace Omni.Blazor.Tests.Utilities;

/// <summary>
/// Unit contract for <see cref="UrlSafety"/> — the scheme allow-list that guards
/// <c>href</c>/<c>src</c> attributes against dangerous schemes when URLs come from
/// untrusted data (e.g. AI/RAG answers).
/// </summary>
public class UrlSafetyTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com/path?q=1#frag")]
    [InlineData("HTTPS://EXAMPLE.COM")]          // scheme case-insensitive
    [InlineData("mailto:user@example.com")]
    [InlineData("tel:+5511999998888")]
    [InlineData("/docs/page")]                    // relativo (raiz)
    [InlineData("page.html")]                     // relativo (sem esquema)
    [InlineData("./sub/page")]                    // relativo (ponto)
    [InlineData("#section")]                       // âncora
    [InlineData("?q=term")]                        // query
    public void IsSafeHref_allows_http_mail_tel_and_relative(string url)
    {
        Assert.True(UrlSafety.IsSafeHref(url));
        Assert.Equal(url.Trim(), UrlSafety.Sanitize(url));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("JavaScript:alert(1)")]           // esquema case-insensitive
    [InlineData(" javascript:alert(1) ")]         // espaços ao redor
    [InlineData("java\tscript:alert(1)")]         // char de controle dividindo o esquema
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=")]
    public void IsSafeHref_rejects_dangerous_schemes(string url)
    {
        Assert.False(UrlSafety.IsSafeHref(url));
        Assert.Null(UrlSafety.Sanitize(url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsSafeHref_rejects_null_or_blank(string? url)
    {
        Assert.False(UrlSafety.IsSafeHref(url));
        Assert.Null(UrlSafety.Sanitize(url));
    }

    [Fact]
    public void Sanitize_trims_and_strips_control_chars()
    {
        Assert.Equal("https://example.com", UrlSafety.Sanitize("  https://exa\tmple.com  "));
    }
}
