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
internal static class MarkdownRenderer
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
            var fence = Regex.Match(line, @"^(\s*)(`{3,}|~{3,})\s*([^\s`]*)\s*$");
            if (fence.Success)
            {
                var marker = fence.Groups[2].Value;
                var lang = fence.Groups[3].Value;
                var code = new StringBuilder();
                i++;
                while (i < end && !Regex.IsMatch(lines[i], $@"^\s*{Regex.Escape(marker.Substring(0, 1))}{{{marker.Length},}}\s*$"))
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
            var head = Regex.Match(line, @"^\s{0,3}(#{1,6})\s+(.*?)\s*#*\s*$");
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
            if (Regex.IsMatch(line, @"^\s{0,3}([-*_])\s*(\1\s*){2,}$"))
            {
                sb.Append("<hr />\n");
                i++;
                continue;
            }

            // Blockquote.
            if (Regex.IsMatch(line, @"^\s{0,3}>"))
            {
                var inner = new List<string>();
                while (i < end && Regex.IsMatch(lines[i], @"^\s{0,3}>"))
                {
                    inner.Add(Regex.Replace(lines[i], @"^\s{0,3}>\s?", ""));
                    i++;
                }
                sb.Append("<blockquote>\n");
                ParseBlocks(inner.ToArray(), 0, inner.Count, sb, allowHtml);
                sb.Append("</blockquote>\n");
                continue;
            }

            // Raw HTML block (only when allowed).
            if (allowHtml && Regex.IsMatch(line, @"^\s{0,3}<(!--|/?[a-zA-Z])"))
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
                Regex.IsMatch(lines[i + 1], @"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$"))
            {
                i = ParseTable(lines, i, end, sb, allowHtml);
                continue;
            }

            // List.
            if (Regex.IsMatch(line, @"^(\s*)([-*+]|\d{1,9}[.)])\s+"))
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
        if (Regex.IsMatch(line, @"^(\s*)(`{3,}|~{3,})")) return true;
        if (Regex.IsMatch(line, @"^\s{0,3}#{1,6}\s")) return true;
        if (Regex.IsMatch(line, @"^\s{0,3}>")) return true;
        if (Regex.IsMatch(line, @"^\s{0,3}([-*_])\s*(\1\s*){2,}$")) return true;
        if (Regex.IsMatch(line, @"^(\s*)([-*+]|\d{1,9}[.)])\s+")) return true;
        if (i + 1 < end && line.Contains('|') &&
            Regex.IsMatch(lines[i + 1], @"^\s*\|?\s*:?-{1,}:?\s*(\|\s*:?-{1,}:?\s*)*\|?\s*$")) return true;
        if (allowHtml && Regex.IsMatch(line, @"^\s{0,3}<(!--|/?[a-zA-Z])")) return true;
        return false;
    }

    // ─── Lists ─────────────────────────────────────────────────────────────
    private static int ParseList(string[] lines, int start, int end, StringBuilder sb, bool allowHtml)
    {
        var first = Regex.Match(lines[start], @"^(\s*)([-*+]|\d{1,9}[.)])(\s+)");
        var baseIndent = first.Groups[1].Value.Length;
        var ordered = char.IsDigit(first.Groups[2].Value[0]);
        var startNum = ordered ? first.Groups[2].Value.TrimEnd('.', ')') : null;
        var startAttr = ordered && startNum != "1" ? $" start=\"{EscAttr(startNum!)}\"" : "";

        sb.Append(ordered ? $"<ol{startAttr}>\n" : "<ul>\n");
        int i = start;
        while (i < end)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) { i++; continue; }
            var m = Regex.Match(lines[i], @"^(\s*)([-*+]|\d{1,9}[.)])(\s+)(.*)$");
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
        var single = Regex.Match(html, @"^<p>(.*)</p>$", RegexOptions.Singleline);
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
            var hard = Regex.IsMatch(l, @"(  +|\\)$");
            sb.Append(l.TrimEnd());
            if (k < lines.Count - 1) sb.Append(hard ? HARD_BREAK : "\n");
        }
        return Inline(sb.ToString(), allowHtml);
    }

    private static string Inline(string text, bool allowHtml)
    {
        var ph = new List<string>();

        // 1) Protect code spans (content escaped, no further processing).
        text = Regex.Replace(text, @"(`+)([\s\S]+?)\1", m =>
            Store(ph, $"<code>{Esc(m.Groups[2].Value.Trim())}</code>"));

        // 2) Images.
        text = Regex.Replace(text, @"!\[([^\]]*)\]\(\s*([^)\s]+)(?:\s+""([^""]*)"")?\s*\)", m =>
        {
            var url = SanitizeUrl(m.Groups[2].Value);
            var alt = EscAttr(m.Groups[1].Value);
            if (url is null) return Store(ph, alt);
            var title = m.Groups[3].Success ? $" title=\"{EscAttr(m.Groups[3].Value)}\"" : "";
            return Store(ph, $"<img src=\"{EscAttr(url)}\" alt=\"{alt}\"{title} />");
        });

        // 3) Links.
        text = Regex.Replace(text, @"\[([^\]]+)\]\(\s*([^)\s]+)(?:\s+""([^""]*)"")?\s*\)", m =>
        {
            var url = SanitizeUrl(m.Groups[2].Value);
            var inner = Inline(m.Groups[1].Value, allowHtml);
            if (url is null) return Store(ph, inner);
            var title = m.Groups[3].Success ? $" title=\"{EscAttr(m.Groups[3].Value)}\"" : "";
            var ext = url.StartsWith("http") ? " target=\"_blank\" rel=\"noopener noreferrer\"" : "";
            return Store(ph, $"<a href=\"{EscAttr(url)}\"{title}{ext}>{inner}</a>");
        });

        // 4) Angle autolinks <https://…> and <a@b>.
        text = Regex.Replace(text, @"<((?:https?|mailto|tel):[^>\s]+|[^@\s>]+@[^@\s>]+\.[^@\s>]+)>", m =>
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
            text = Regex.Replace(text, @"<!--[\s\S]*?-->|</?[a-zA-Z][^>]*>", m => Store(ph, SanitizeHtml(m.Value)));
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
        s = Regex.Replace(s, @"\*\*(?=\S)(.+?)(?<=\S)\*\*", "<strong>$1</strong>");
        s = Regex.Replace(s, @"(?<![\w])__(?=\S)(.+?)(?<=\S)__(?![\w])", "<strong>$1</strong>");
        s = Regex.Replace(s, @"\*(?=\S)(.+?)(?<=\S)\*", "<em>$1</em>");
        s = Regex.Replace(s, @"(?<![\w])_(?=\S)(.+?)(?<=\S)_(?![\w])", "<em>$1</em>");
        s = Regex.Replace(s, @"~~(?=\S)(.+?)(?<=\S)~~", "<del>$1</del>");
        return s;
    }

    // ─── Helpers ───────────────────────────────────────────────────────────
    private static string Store(List<string> ph, string html)
    {
        ph.Add(html);
        return $"{PH_OPEN}{ph.Count - 1}{PH_CLOSE}";
    }

    private static string Restore(string text, List<string> ph)
        => Regex.Replace(text, $"{PH_OPEN}(\\d+){PH_CLOSE}", m => ph[int.Parse(m.Groups[1].Value)]);

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
        // Strip control chars browsers ignore inside schemes ("jav&#9;ascript:") so they
        // can't smuggle a dangerous scheme past the URL filter below.
        html = Regex.Replace(html, @"[\x00-\x1F\x7F]", " ");
        // Dangerous elements (with their content) + their standalone/self-closing tags.
        html = Regex.Replace(html, @"<(script|style|iframe|object|embed|form|svg|math)\b[\s\S]*?</\1\s*>", "",
            RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</?(script|style|iframe|object|embed|form|svg|math|link|meta|base)\b[^>]*>", "",
            RegexOptions.IgnoreCase);
        // Inline event handlers — attributes may be separated by whitespace OR a slash
        // (<img/onerror=...>), so accept either as the leading boundary.
        html = Regex.Replace(html, @"[\s/]on\w+\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", "", RegexOptions.IgnoreCase);
        // Dangerous URL schemes on href/src, whether quoted OR unquoted → neutralized to '#'.
        html = Regex.Replace(html,
            @"(href|src)\s*=\s*(""|'|)\s*(?:javascript|vbscript|data\s*:\s*text/html)[^""'>\s]*",
            "$1=$2#", RegexOptions.IgnoreCase);
        return html;
    }
}
