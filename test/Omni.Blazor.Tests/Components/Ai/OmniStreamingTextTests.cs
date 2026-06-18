using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Ai;

/// <summary>
/// Behavioural contract for <see cref="OmniStreamingText"/>: markdown vs plain
/// rendering, the streaming caret + aria-busy live region, placeholder, and the
/// cross-cutting Class/Style/Attributes splat.
/// </summary>
public class OmniStreamingTextTests : TestContextBase
{
    [Fact]
    public void Renders_root_as_polite_live_region()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Text, "hi"));

        var root = cut.Find("div.omni-streaming-text");
        Assert.Equal("polite", root.GetAttribute("aria-live"));
        Assert.Equal("false", root.GetAttribute("aria-busy"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Class, "custom-cls").Add(c => c.Text, "x"));
        Assert.Contains("custom-cls", cut.Find("div.omni-streaming-text").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Style, "max-width: 40ch").Add(c => c.Text, "x"));
        Assert.Equal("max-width: 40ch", cut.Find("div.omni-streaming-text").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p
            .AddUnmatched("data-testid", "stream")
            .Add(c => c.Text, "x"));
        Assert.Equal("stream", cut.Find("div.omni-streaming-text").GetAttribute("data-testid"));
    }

    [Fact]
    public void Renders_text_as_markdown_by_default()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Text, "**bold**"));
        Assert.Contains("<strong>bold</strong>", cut.Find(".omni-markdown").InnerHtml);
    }

    [Fact]
    public void Renders_plain_text_when_Markdown_disabled()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Markdown, false).Add(c => c.Text, "**bold**"));
        Assert.Empty(cut.FindAll(".omni-markdown"));
        Assert.Equal("**bold**", cut.Find(".omni-streaming-text-content").TextContent);
    }

    [Fact]
    public void Streaming_shows_caret_and_busy_and_on_class()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Streaming, true).Add(c => c.Text, "hi"));

        var root = cut.Find("div.omni-streaming-text");
        Assert.Contains("omni-streaming-text-on", root.ClassName);
        Assert.Equal("true", root.GetAttribute("aria-busy"));
        Assert.NotNull(cut.Find(".omni-streaming-caret"));
    }

    [Fact]
    public void Not_streaming_has_no_caret()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p.Add(c => c.Text, "hi"));
        Assert.Empty(cut.FindAll(".omni-streaming-caret"));
    }

    [Fact]
    public void Caret_disabled_hides_caret_while_streaming()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p
            .Add(c => c.Streaming, true).Add(c => c.Caret, false).Add(c => c.Text, "hi"));
        Assert.Empty(cut.FindAll(".omni-streaming-caret"));
    }

    [Fact]
    public void Placeholder_shown_only_when_streaming_and_empty()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p
            .Add(c => c.Streaming, true).Add(c => c.Placeholder, "Thinking…"));
        Assert.Equal("Thinking…", cut.Find(".omni-streaming-text-placeholder").TextContent);

        cut.SetParametersAndRender(p => p.Add(c => c.Text, "Answer"));
        Assert.Empty(cut.FindAll(".omni-streaming-text-placeholder")); // gone once text arrives
    }

    [Fact]
    public void Updating_Text_streams_more_content()
    {
        var cut = RenderComponent<OmniStreamingText>(p => p
            .Add(c => c.Streaming, true).Add(c => c.Markdown, false).Add(c => c.Text, "He"));
        cut.SetParametersAndRender(p => p.Add(c => c.Text, "Hello"));
        Assert.Equal("Hello", cut.Find(".omni-streaming-text-content").TextContent);
    }
}
