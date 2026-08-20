using System.Text;
using System.Text.RegularExpressions;

namespace Omni.Blazor.Utilities;

/// <summary>
/// A focused, dependency-free Markdown → HTML renderer (CommonMark-ish subset +
/// GFM tables + strikethrough). XSS-safe by construction: every text run is
/// HTML-escaped, link/image URLs go through a scheme allow-list, and raw HTML in
/// the source is escaped unless <c>allowHtml</c> is set (then it is passed
/// through a best-effort sanitizer that strips scripts, event handlers and
/// dangerous URLs).
///
/// Supported: ATX headings (#…######), paragraphs, hard/soft breaks, bold
/// (**/__), italic (*/_), strikethrough (~~), inline code, fenced + indented
/// code, blockquotes, ordered/unordered nested lists, links, images,
/// angle-autolinks, GFM pipe tables (with alignment), thematic breaks.
/// </summary>
internal static partial class MarkdownRenderer
{
    // Placeholder sentinels — private-use-area chars that never appear in real text.
    private const char PH_OPEN = '';
    private const char PH_CLOSE = '';
    private const string HARD_BREAK = "";

    public static string ToHtml(string? markdown, bool allowHtml = false)
    {
        if (string.IsNullOrEmpty(markdown)) return string.Empty;
        var lines = markdown!.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        var sb = new StringBuilder();
        ParseBlocks(lines, 0, lines.Length, sb, allowHtml);
        return sb.ToString();
    }

    // ─── Block level ───────────────────────────────────────────────────────
    private static void ParseBlocks(string[] lines, int start, int end, StringBuilder sb, bool allowHtml)
    {
        int i = start;
        while (i < end)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) { i++; continue; }

            // Fenced code block.
            var fence = FenceOpenRegex().Match(line);
            if (fence.Success)
            {
                var marker = fence.Groups[2].Value;
                var lang = fence.Groups[3].Value;
                var code = new StringBuilder();
                i++;
                while (i < end && !IsClosingFence(lines[i], marker[0], marker.Length))
                {
                    code.Append(Esc(lines[i])).Append('\n');
                    i++;
                }
                if (i < end) i++; // closing fence
                var cls = string.IsNullOrEmpty(lang) ? "" : $" class=\"language-{EscAttr(lang)}\"";
                sb.Append("<pre><code").Append(cls).Append('>').Append(code).Append("</code></pre>\n");
                continue;
            }

            // ATX heading.
            var head = AtxHeadingRegex().Match(line);
            if (head.Success)
            {
                var level = head.Groups[1].Value.Length;
                sb.Append("<h").Append(level).Append('>')
                  .Append(Inline(head.Groups[2].Value, allowHtml))
                  .Append("</h").Append(level).Append(">\n");
                i++;
                continue;
            }

            // Thematic break.
            if (ThematicBreakRegex().IsMatch(line))
            {
                sb.Append("<hr />\n");
                i++;
                continue;
            }

            // Blockquote.
            if (BlockquoteRegex().IsMatch(line))
            {
                var inner = new List<string>();
                while (i < end && BlockquoteRegex().IsMatch(lines[i]))
                {
                    inner.Add(BlockquoteStripRegex().Replace(lines[i], ""));
                    i++;
                }
                sb.Append("<blockquote>\n");
                ParseBlocks(inner.ToArray(), 0, inner.Count, sb, allowHtml);
                sb.Append("</blockquote>\n");
                continue;
            }

            // Raw HTML block (only when allowed).
            if (allowHtml && HtmlBlockRegex().IsMatch(line))
            {
                var html = new StringBuilder();
                while (i < end && !string.IsNullOrWhiteSpace(lines[i]))
                {
                    html.Append(lines[i]).Append('\n');
                    i++;
                }
                sb.Append(SanitizeHtml(html.ToString())).Append('\n');
                continue;
            }

            // Table (a header row + a delimiter row).
            if (i + 1 < end && line.Contains('|') &&
                TableDelimiterRegex().IsMatch(lines[i + 1]))
            {
                i = ParseTable(lines, i, end, sb, allowHtml);
                continue;
            }

            // List.
            if (ListStartRegex().IsMatch(line))
            {
                i = ParseList(lines, i, end, sb, allowHtml);
                continue;
            }

            // Paragraph: gather until a blank line or a block-starting line.
            var para = new List<string>();
            while (i < end && !string.IsNullOrWhiteSpace(lines[i]) && !IsBlockStart(lines, i, end, allowHtml))
            {
                para.Add(lines[i]);
                i++;
            }
            if (para.Count > 0)
            {
                sb.Append("<p>").Append(InlineParagraph(para, allowHtml)).Append("</p>\n");
            }
        }
    }

    private static bool IsBlockStart(string[] lines, int i, int end, bool allowHtml)
    {
        var line = lines[i];
        if (FenceStartRegex().IsMatch(line)) return true;
        if (AtxHeadingStartRegex().IsMatch(line)) return true;
        if (BlockquoteRegex().IsMatch(line)) return true;
        if (ThematicBreakRegex().IsMatch(line)) return true;
        if (ListStartRegex().IsMatch(line)) return true;
        if (i + 1 < end && line.Contains('|') &&
            TableDelimiterRegex().IsMatch(lines[i + 1])) return true;
        if (allowHtml && HtmlBlockRegex().IsMatch(line)) return true;
        return false;
    }

    // ─── Lists ─────────────────────────────────────────────────────────────
    private static int ParseList(string[] lines, int start, int end, StringBuilder sb, bool allowHtml)
    {
        var first = ListItemMarkerRegex().Match(lines[start]);
        var baseIndent = first.Groups[1].Value.Length;
        var ordered = char.IsDigit(first.Groups[2].Value[0]);
        var startNum = ordered ? first.Groups[2].Value.TrimEnd('.', ')') : null;
        var startAttr = ordered && startNum != "1" ? $" start=\"{EscAttr(startNum!)}\"" : "";

        sb.Append(ordered ? $"<ol{startAttr}>\n" : "<ul>\n");
        int i = start;
        while (i < end)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) { i++; continue; }
            var m = ListItemFullRegex().Match(lines[i]);
            if (!m.Success || m.Groups[1].Value.Length < baseIndent) break;
            if (m.Groups[1].Value.Length > baseIndent) break; // belongs to a deeper list (handled as item content)

            var marker = m.Groups[2].Value;
            var contentIndent = m.Groups[1].Value.Length + marker.Length + m.Groups[3].Value.Length;
            var itemLines = new List<string> { m.Groups[4].Value };
            i++;
            while (i < end && (string.IsNullOrWhiteSpace(lines[i]) || LeadingSpaces(lines[i]) >= contentIndent))
            {
                itemLines.Add(string.IsNullOrWhiteSpace(lines[i]) ? "" : Dedent(lines[i], contentIndent));
                i++;
            }
            sb.Append("<li>").Append(RenderListItem(itemLines, allowHtml)).Append("</li>\n");
        }
        sb.Append(ordered ? "</ol>\n" : "</ul>\n");
        return i;
    }

    private static string RenderListItem(List<string> lines, bool allowHtml)
    {
        while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[^1])) lines.RemoveAt(lines.Count - 1);
        var inner = new StringBuilder();
        ParseBlocks(lines.ToArray(), 0, lines.Count, inner, allowHtml);
        var html = inner.ToString().Trim();
        // Tight item: unwrap a single paragraph so the marker hugs the text.
        var single = SingleParagraphRegex().Match(html);
        if (single.Success && !single.Groups[1].Value.Contains("<p>"))
            return single.Groups[1].Value;
        return html;
    }

    // ─── Tables ────────────────────────────────────────────────────────────
    private static int ParseTable(string[] lines, int start, int end, StringBuilder sb, bool allowHtml)
    {
        var header = SplitRow(lines[start]);
        var aligns = SplitRow(lines[start + 1]).Select(c =>
        {
            var t = c.Trim();
            var l = t.StartsWith(':');
            var r = t.EndsWith(':');
            return l && r ? "center" : r ? "right" : l ? "left" : "";
        }).ToList();
        int i = start + 2;
        var rows = new List<List<string>>();
        while (i < end && !string.IsNullOrWhiteSpace(lines[i]) && lines[i].Contains('|'))
        {
            rows.Add(SplitRow(lines[i]));
            i++;
        }

        sb.Append("<table class=\"omni-md-table\">\n<thead>\n<tr>");
        for (int c = 0; c < header.Count; c++)
        {
            var style = c < aligns.Count && aligns[c] != "" ? $" style=\"text-align:{aligns[c]}\"" : "";
            sb.Append("<th").Append(style).Append('>').Append(Inline(header[c], allowHtml)).Append("</th>");
        }
        sb.Append("</tr>\n</thead>\n<tbody>\n");
        foreach (var row in rows)
        {
            sb.Append("<tr>");
            for (int c = 0; c < header.Count; c++)
            {
                var style = c < aligns.Count && aligns[c] != "" ? $" style=\"text-align:{aligns[c]}\"" : "";
                var val = c < row.Count ? row[c] : "";
                sb.Append("<td").Append(style).Append('>').Append(Inline(val, allowHtml)).Append("</td>");
            }
            sb.Append("</tr>\n");
        }
        sb.Append("</tbody>\n</table>\n");
        return i;
    }

    private static List<string> SplitRow(string line)
    {
        var t = line.Trim();
        if (t.StartsWith("|")) t = t.Substring(1);
        if (t.EndsWith("|")) t = t.Substring(0, t.Length - 1);
        return Regex.Split(t, @"(?<!\\)\|").Select(c => c.Replace("\\|", "|").Trim()).ToList();
    }

    // ─── Inline level ──────────────────────────────────────────────────────
    private static string InlineParagraph(List<string> lines, bool allowHtml)
    {
        var sb = new StringBuilder();
        for (int k = 0; k < lines.Count; k++)
        {
            var l = lines[k];
            var hard = HardBreakRegex().IsMatch(l);
            sb.Append(l.TrimEnd());
            if (k < lines.Count - 1) sb.Append(hard ? HARD_BREAK : "\n");
        }
        return Inline(sb.ToString(), allowHtml);
    }

    private static string Inline(string text, bool allowHtml)
    {
        var ph = new List<string>();

        // 1) Protect code spans (content escaped, no further processing).
        text = InlineCodeRegex().Replace(text, m =>
            Store(ph, $"<code>{Esc(m.Groups[2].Value.Trim())}</code>"));

        // 2) Images.
        text = ImageRegex().Replace(text, m =>
        {
            var url = SanitizeUrl(m.Groups[2].Value);
            var alt = EscAttr(m.Groups[1].Value);
            if (url is null) return Store(ph, alt);
            var title = m.Groups[3].Success ? $" title=\"{EscAttr(m.Groups[3].Value)}\"" : "";
            return Store(ph, $"<img src=\"{EscAttr(url)}\" alt=\"{alt}\"{title} />");
        });

        // 3) Links.
        text = LinkRegex().Replace(text, m =>
        {
            var url = SanitizeUrl(m.Groups[2].Value);
            var inner = Inline(m.Groups[1].Value, allowHtml);
            if (url is null) return Store(ph, inner);
            var title = m.Groups[3].Success ? $" title=\"{EscAttr(m.Groups[3].Value)}\"" : "";
            var ext = url.StartsWith("http") ? " target=\"_blank\" rel=\"noopener noreferrer\"" : "";
            return Store(ph, $"<a href=\"{EscAttr(url)}\"{title}{ext}>{inner}</a>");
        });

        // 4) Angle autolinks <https://…> and <a@b>.
        text = AutolinkRegex().Replace(text, m =>
        {
            var raw = m.Groups[1].Value;
            var href = raw.Contains('@') && !raw.Contains(':') ? "mailto:" + raw : raw;
            var url = SanitizeUrl(href);
            if (url is null) return Store(ph, Esc(raw));
            return Store(ph, $"<a href=\"{EscAttr(url)}\">{Esc(raw)}</a>");
        });

        // 5) Raw inline HTML (only when allowed — sanitized; otherwise escaped below).
        if (allowHtml)
        {
            text = RawHtmlRegex().Replace(text, m => Store(ph, SanitizeHtml(m.Value)));
        }

        // 6) Escape everything that's left (placeholders survive — they're private-use chars).
        text = Esc(text);

        // 7) Emphasis / strong / strikethrough on the escaped text.
        text = ApplyEmphasis(text);

        // 8) Hard breaks.
        text = text.Replace(HARD_BREAK, "<br />\n");

        // 9) Restore protected spans.
        return Restore(text, ph);
    }

    private static string ApplyEmphasis(string s)
    {
        s = BoldStarRegex().Replace(s, "<strong>$1</strong>");
        s = BoldUnderscoreRegex().Replace(s, "<strong>$1</strong>");
        s = ItalicStarRegex().Replace(s, "<em>$1</em>");
        s = ItalicUnderscoreRegex().Replace(s, "<em>$1</em>");
        s = StrikethroughRegex().Replace(s, "<del>$1</del>");
        return s;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────
    private static string Store(List<string> ph, string html)
    {
        ph.Add(html);
        return $"{PH_OPEN}{ph.Count - 1}{PH_CLOSE}";
    }

    private static string Restore(string text, List<string> ph)
        => PlaceholderRegex().Replace(text, m => ph[int.Parse(m.Groups[1].Value)]);

    private static string Esc(string s)
        => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string EscAttr(string s)
        => Esc(s).Replace("\"", "&quot;");

    private static int LeadingSpaces(string s)
    {
        int n = 0;
        foreach (var c in s) { if (c == ' ') n++; else if (c == '\t') n += 4; else break; }
        return n;
    }

    private static string Dedent(string s, int count)
    {
        int removed = 0, i = 0;
        while (i < s.Length && removed < count && (s[i] == ' ' || s[i] == '\t')) { removed += s[i] == '\t' ? 4 : 1; i++; }
        return s.Substring(i);
    }

    /// <summary>Allow-lists URL schemes (http/https/mailto/tel) + relative/anchor URLs; rejects the rest.</summary>
    private static string? SanitizeUrl(string url) => UrlSafety.Sanitize(url);

    /// <summary>
    /// Best-effort HTML sanitizer for <c>AllowHtml</c>: strips scripts, event handlers and
    /// dangerous URLs. Hardened against unquoted-attribute, control-char-in-scheme and
    /// slash-separated-handler bypasses — but regex is not a substitute for a real parser:
    /// only pass TRUSTED content. For untrusted HTML use a parser-based sanitizer upstream.
    /// </summary>
    internal static string SanitizeHtml(string html)
    {
        // Resolve numeric character references (&#9; / &#x09;) — browsers decode these
        // BEFORE evaluating a URL scheme, so "jav&#9;ascript:" would otherwise slip past the
        // checks. Loop (capped) to defeat double-encoding. Named entities (&lt; …) are NOT
        // decoded here (the element/handler filters run afterwards). Regex is still
        // best-effort: only pass TRUSTED content; use a DOM/parser-based sanitizer for
        // hostile input.
        for (int i = 0; i < 5; i++)
        {
            string decoded = DecodeNumericEntities(html);
            if (string.Equals(decoded, html, StringComparison.Ordinal)) break;
            html = decoded;
        }
        // Control chars browsers strip from URL schemes -> space (breaks the scheme).
        html = ControlCharRegex().Replace(html, " ");
        // Dangerous elements (with content) + their standalone/self-closing tags.
        html = DangerousElementPairRegex().Replace(html, "");
        html = DangerousElementTagRegex().Replace(html, "");
        // Inline event handlers — attributes may be whitespace- OR slash-separated.
        html = EventHandlerAttrRegex().Replace(html, "");
        // href/src with a dangerous scheme -> neutralized to '#', re-emitting a well-formed value.
        html = UrlAttrRegex().Replace(html, NeutralizeUrl);
        return html;
    }

    // Neutralize a href/src whose value uses a dangerous scheme, re-emitting a well-formed
    // (quoted or unquoted) value. Every data: URL is blocked except safe static image types.
    private static string NeutralizeUrl(Match m)
    {
        string attr = m.Groups[1].Value;
        string raw = m.Groups[2].Value;
        char quote = raw.Length > 0 && (raw[0] == '"' || raw[0] == '\'') ? raw[0] : '\0';
        string inner = quote != '\0' ? raw.Trim(quote) : raw;
        string probe = WhitespaceRegex().Replace(inner, "").ToLowerInvariant();
        bool dangerous = probe.StartsWith("javascript:", StringComparison.Ordinal)
            || probe.StartsWith("vbscript:", StringComparison.Ordinal)
            || (probe.StartsWith("data:", StringComparison.Ordinal) && !IsSafeDataImage(probe));
        if (!dangerous) return m.Value;
        return quote != '\0' ? $"{attr}={quote}#{quote}" : $"{attr}=#";
    }

    private static bool IsSafeDataImage(string probe) =>
        probe.StartsWith("data:image/png", StringComparison.Ordinal)
        || probe.StartsWith("data:image/jpeg", StringComparison.Ordinal)
        || probe.StartsWith("data:image/jpg", StringComparison.Ordinal)
        || probe.StartsWith("data:image/gif", StringComparison.Ordinal)
        || probe.StartsWith("data:image/webp", StringComparison.Ordinal);

    private static string DecodeNumericEntities(string s) =>
        NumericEntityRegex().Replace(s, m =>
        {
            int code;
            bool ok = m.Groups[1].Success
                ? int.TryParse(m.Groups[1].Value, out code)
                : int.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out code);
            if (!ok || code <= 0 || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF)) return m.Value;
            return char.ConvertFromUtf32(code);
        });

    // ─── Closing fence ─────────────────────────────────────────────────────
    /// <summary>
    /// Whether <paramref name="line"/> closes a fenced block opened with
    /// <paramref name="length"/> or more of <paramref name="marker"/>.
    /// Hand-written rather than generated: the pattern depends on the opening
    /// fence, so it is the one check here that cannot be a compile-time literal.
    /// Building it per call was also the worst case for the old static-Regex
    /// cache, since a fresh pattern string can never hit it.
    /// </summary>
    private static bool IsClosingFence(string line, char marker, int length)
    {
        int i = 0;
        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;

        int run = 0;
        while (i < line.Length && line[i] == marker) { run++; i++; }
        if (run < length) return false;

        while (i < line.Length && char.IsWhiteSpace(line[i])) i++;
        return i == line.Length;
    }

    // ─── Patterns ──────────────────────────────────────────────────────────
    // Source-generated: each becomes a compiled static instance, so there is no
    // per-call cache lookup and nothing to recompile. The previous static
    // Regex.X(input, pattern) overloads shared a process-wide cache of 15 while
    // this file uses ~29 patterns, so most calls missed and recompiled — 84 KB
    // to render one short sentence. See benchmarks/README.md.

    [GeneratedRegex(@"^(\s*)(`{3,}|~{3,})\s*([^\s`]*)\s*$")]
    private static partial Regex FenceOpenRegex();

    [GeneratedRegex(@"^(\s*)(`{3,}|~{3,})")]
    private static partial Regex FenceStartRegex();

    [GeneratedRegex(@"^\s{0,3}(#{1,6})\s+(.*?)\s*#*\s*$")]
    private static partial Regex AtxHeadingRegex();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s")]
    private static partial Regex AtxHeadingStartRegex();

    [GeneratedRegex(@"^\s{0,3}([-*_])\s*(\1\s*){2,}$")]
    private static partial Regex ThematicBreakRegex();

    [GeneratedRegex(@"^\s{0,3}>")]
    private static partial Regex BlockquoteRegex();

    [GeneratedRegex(@"^\s{0,3}>\s?")]
    private static partial Regex BlockquoteStripRegex();

    [GeneratedRegex(@"^\s{0,3}<(!--|/?[a-zA-Z])")]
    private static partial Regex HtmlBlockRegex();

    [GeneratedRegex(@"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$")]
    private static partial Regex TableDelimiterRegex();

    [GeneratedRegex(@"^(\s*)([-*+]|\d{1,9}[.)])\s+")]
    private static partial Regex ListStartRegex();

    [GeneratedRegex(@"^(\s*)([-*+]|\d{1,9}[.)])(\s+)")]
    private static partial Regex ListItemMarkerRegex();

    [GeneratedRegex(@"^(\s*)([-*+]|\d{1,9}[.)])(\s+)(.*)$")]
    private static partial Regex ListItemFullRegex();

    [GeneratedRegex(@"^<p>(.*)</p>$", RegexOptions.Singleline)]
    private static partial Regex SingleParagraphRegex();

    [GeneratedRegex(@"(  +|\\)$")]
    private static partial Regex HardBreakRegex();

    [GeneratedRegex(@"(`+)([\s\S]+?)\1")]
    private static partial Regex InlineCodeRegex();

    [GeneratedRegex(@"!\[([^\]]*)\]\(\s*([^)\s]+)(?:\s+""([^""]*)"")?\s*\)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"\[([^\]]+)\]\(\s*([^)\s]+)(?:\s+""([^""]*)"")?\s*\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"<((?:https?|mailto|tel):[^>\s]+|[^@\s>]+@[^@\s>]+\.[^@\s>]+)>")]
    private static partial Regex AutolinkRegex();

    [GeneratedRegex(@"<!--[\s\S]*?-->|</?[a-zA-Z][^>]*>")]
    private static partial Regex RawHtmlRegex();

    [GeneratedRegex(@"\*\*(?=\S)(.+?)(?<=\S)\*\*")]
    private static partial Regex BoldStarRegex();

    [GeneratedRegex(@"(?<![\w])__(?=\S)(.+?)(?<=\S)__(?![\w])")]
    private static partial Regex BoldUnderscoreRegex();

    [GeneratedRegex(@"\*(?=\S)(.+?)(?<=\S)\*")]
    private static partial Regex ItalicStarRegex();

    [GeneratedRegex(@"(?<![\w])_(?=\S)(.+?)(?<=\S)_(?![\w])")]
    private static partial Regex ItalicUnderscoreRegex();

    [GeneratedRegex(@"~~(?=\S)(.+?)(?<=\S)~~")]
    private static partial Regex StrikethroughRegex();

    // \uE000 / \uE001 are PH_OPEN / PH_CLOSE. Spelled as regex escapes rather
    // than interpolated so the pattern is a compile-time constant — and so the
    // private-use-area characters stay visible to anyone reading this file.
    [GeneratedRegex(@"\uE000(\d+)\uE001")]
    private static partial Regex PlaceholderRegex();

    [GeneratedRegex(@"[\x00-\x1F\x7F]")]
    private static partial Regex ControlCharRegex();

    [GeneratedRegex(@"<(script|style|iframe|object|embed|form|svg|math)\b[\s\S]*?</\1\s*>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousElementPairRegex();

    [GeneratedRegex(@"</?(script|style|iframe|object|embed|form|svg|math|link|meta|base)\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousElementTagRegex();

    [GeneratedRegex(@"[\s/]on\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex EventHandlerAttrRegex();

    [GeneratedRegex(@"(href|src)\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlAttrRegex();

    [GeneratedRegex(@"\s")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"&#(\d{1,7});|&#[xX]([0-9a-fA-F]{1,6});")]
    private static partial Regex NumericEntityRegex();
}
