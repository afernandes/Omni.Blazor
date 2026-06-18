using System.Text.RegularExpressions;

namespace Omni.Blazor.Utilities;

/// <summary>
/// URL scheme allow-list for values rendered into DOM attributes such as
/// <c>href</c>/<c>src</c>. Permits <c>http</c>/<c>https</c>/<c>mailto</c>/<c>tel</c>
/// plus relative and anchor URLs, and rejects everything else
/// (<c>javascript:</c>, <c>vbscript:</c>, <c>data:</c>, …). This matters when the
/// URL comes from untrusted data — common in AI/RAG answers — so a crafted scheme
/// cannot inject script when the value reaches an anchor's <c>href</c>.
/// </summary>
public static class UrlSafety
{
    private static readonly Regex SchemeRx =
        new(@"^([a-zA-Z][a-zA-Z0-9+.\-]*):", RegexOptions.Compiled);

    /// <summary>
    /// Returns the trimmed, control-char-stripped URL when its scheme is safe (or it
    /// is a relative/anchor URL with no scheme); otherwise <c>null</c>.
    /// </summary>
    public static string? Sanitize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        // Strip control chars that can split a scheme (e.g. "java\tscript:").
        var u = new string(url.Trim().Where(c => !char.IsControl(c)).ToArray());
        if (u.Length == 0) return null;
        var m = SchemeRx.Match(u);
        if (m.Success)
        {
            var scheme = m.Groups[1].Value.ToLowerInvariant();
            if (scheme is not ("http" or "https" or "mailto" or "tel")) return null;
        }
        return u;
    }

    /// <summary>True when <paramref name="url"/> is non-empty and safe to use as an <c>href</c>.</summary>
    public static bool IsSafeHref(string? url) => Sanitize(url) is not null;
}
