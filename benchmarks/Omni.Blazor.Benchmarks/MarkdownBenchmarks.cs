using System.Text;
using BenchmarkDotNet.Attributes;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Benchmarks;

/// <summary>
/// <see cref="MarkdownRenderer.ToHtml"/> runs roughly 32 regex passes over the
/// source. <c>OmniMarkdown</c> memoises the result per (source, allowHtml), so a
/// re-render costs nothing — but a streaming assistant reply re-parses on every
/// chunk with a *different* source each time, which is exactly what the cache
/// cannot help with. <see cref="StreamingChunk"/> models that: the same document
/// growing one chunk at a time.
///
/// <see cref="SanitizeHostileHtml"/> covers the hardened sanitiser separately,
/// since <c>AllowHtml</c> turns on a second regex battery.
/// </summary>
[MemoryDiagnoser]
public class MarkdownBenchmarks
{
    private string _short = string.Empty;
    private string _document = string.Empty;
    private string _streamingPrefix = string.Empty;
    private string _hostileHtml = string.Empty;

    [GlobalSetup]
    public void Setup()
    {
        _short = "A short **assistant** reply with `code` and a [link](https://example.com).";

        var document = new StringBuilder();
        document.AppendLine("# Report");
        document.AppendLine();
        for (int section = 0; section < 12; section++)
        {
            document.AppendLine($"## Section {section}");
            document.AppendLine();
            document.AppendLine("Body text with **bold**, *italic*, `inline code` and a [link](https://example.com/a).");
            document.AppendLine();
            document.AppendLine("- first item");
            document.AppendLine("- second item with `code`");
            document.AppendLine();
            document.AppendLine("```csharp");
            document.AppendLine("var builder = CssBuilder.Default(\"omni-x\");");
            document.AppendLine("```");
            document.AppendLine();
        }
        _document = document.ToString();

        // Mid-stream: the cache never sees this exact string twice.
        _streamingPrefix = _document[..(_document.Length / 2)];

        _hostileHtml =
            "<p onclick=\"steal()\">text</p>" +
            "<img src=\"javascript&#9;:alert(1)\">" +
            "<a href=javascript:alert(1)>x</a>" +
            "<script>alert(1)</script>" +
            "<img src=\"data:image/png;base64,iVBORw0KGgo=\">";
    }

    [Benchmark(Baseline = true)]
    public string ShortReply() => MarkdownRenderer.ToHtml(_short);

    [Benchmark]
    public string FullDocument() => MarkdownRenderer.ToHtml(_document);

    [Benchmark]
    public string StreamingChunk() => MarkdownRenderer.ToHtml(_streamingPrefix);

    [Benchmark]
    public string SanitizeHostileHtml() => MarkdownRenderer.ToHtml(_hostileHtml, allowHtml: true);
}
