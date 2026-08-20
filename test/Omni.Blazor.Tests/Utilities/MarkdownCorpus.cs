namespace Omni.Blazor.Tests.Utilities;

/// <summary>
/// One input per Markdown feature the renderer claims to support, plus the
/// edge cases that distinguish the patterns from each other. Shared by the
/// characterization test, which pins the exact HTML this corpus produces.
///
/// The point is coverage of the *regex surface*: every pattern in
/// <c>MarkdownRenderer</c> should be exercised by at least one case here, so a
/// change to any of them shows up as a diff rather than as silence.
/// </summary>
internal static class MarkdownCorpus
{
    internal static readonly (string Name, string Source, bool AllowHtml)[] Cases =
    [
        ("atx-headings", "# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6\n## Closed ##", false),
        ("heading-not-a-heading", "#NoSpace\n####### seven hashes", false),
        ("paragraph-and-soft-break", "first line\nsecond line\n\nnew paragraph", false),
        ("hard-break-spaces", "line one  \nline two", false),
        ("hard-break-backslash", "line one\\\nline two", false),
        ("bold-asterisk", "**bold** and **two words**", false),
        ("bold-underscore", "__bold__ but snake_case_word untouched", false),
        ("italic-asterisk", "*italic* and a*b not italic", false),
        ("italic-underscore", "_italic_ but snake_case stays", false),
        ("strikethrough", "~~gone~~ and ~not~ single", false),
        ("nested-emphasis", "**bold with *italic* inside**", false),
        ("inline-code", "use `var x = 1;` here", false),
        ("inline-code-multi-backtick", "``code with ` backtick``", false),
        ("inline-code-protects-markup", "`**not bold**` outside **bold**", false),
        ("fenced-code-backtick", "```\nplain\n```", false),
        ("fenced-code-language", "```csharp\nvar x = 1;\n```", false),
        ("fenced-code-tilde", "~~~\ntilde fenced\n~~~", false),
        ("fenced-code-longer-fence", "````\ninner ``` stays\n````", false),
        ("fenced-code-escapes-html", "```\n<script>alert(1)</script>\n```", false),
        ("thematic-break-dash", "before\n\n---\n\nafter", false),
        ("thematic-break-star", "***", false),
        ("thematic-break-underscore", "___", false),
        ("blockquote", "> quoted line\n> second line", false),
        ("blockquote-nested-content", "> # heading in quote\n> **bold**", false),
        ("unordered-list", "- one\n- two\n- three", false),
        ("unordered-list-star", "* one\n* two", false),
        ("ordered-list", "1. first\n2. second", false),
        ("ordered-list-paren", "1) first\n2) second", false),
        ("nested-list", "- outer\n  - inner\n  - inner two\n- outer two", false),
        ("list-then-paragraph", "- item\n\nparagraph after", false),
        ("link", "a [link](https://example.com) here", false),
        ("link-with-title", "[link](https://example.com \"The Title\")", false),
        ("image", "![alt text](https://example.com/i.png)", false),
        ("image-with-title", "![alt](https://example.com/i.png \"Title\")", false),
        ("autolink-url", "<https://example.com/path>", false),
        ("autolink-mail", "<user@example.com>", false),
        ("link-dangerous-scheme", "[x](javascript:alert(1))", false),
        ("image-dangerous-scheme", "![x](javascript:alert(1))", false),
        ("table-simple", "| a | b |\n|---|---|\n| 1 | 2 |", false),
        ("table-alignment", "| l | c | r |\n|:--|:-:|--:|\n| 1 | 2 | 3 |", false),
        ("html-escaped-by-default", "<b>bold</b> & <i>it</i>", false),
        ("ampersand-and-entities", "a & b, &amp; already, &#65; numeric", false),
        ("raw-html-allowed", "<div class=\"x\">kept</div>", true),
        ("raw-html-script-stripped", "<script>alert(1)</script><p>kept</p>", true),
        ("raw-html-event-handler-stripped", "<p onclick=\"steal()\">text</p>", true),
        ("raw-html-javascript-href", "<a href=\"javascript:alert(1)\">x</a>", true),
        ("raw-html-entity-obfuscated-scheme", "<img src=\"javascript&#9;:alert(1)\">", true),
        ("raw-html-unquoted-attribute", "<a href=javascript:alert(1)>x</a>", true),
        ("raw-html-data-image-allowed", "<img src=\"data:image/png;base64,iVBORw0KGgo=\">", true),
        ("raw-html-data-other-blocked", "<img src=\"data:text/html,<script>alert(1)</script>\">", true),
        ("raw-html-comment", "<!-- a comment --><p>after</p>", true),
        ("empty", "", false),
        ("whitespace-only", "   \n\n   ", false),
        ("mixed-document",
            "# Title\n\nIntro **bold** with `code`.\n\n- list `item`\n- [link](https://example.com)\n\n" +
            "> quote\n\n```csharp\nvar x = 1;\n```\n\n| a | b |\n|---|---|\n| 1 | 2 |\n\n---\n\nEnd.",
            false),
    ];
}
