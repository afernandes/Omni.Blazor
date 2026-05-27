using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniVirtualize{TItem}"/>: wraps Blazor's
/// Virtualize. We check the wrapper div, the empty state slot, height style,
/// and the cross-cutting splat. Inner virtualization itself is Blazor's
/// responsibility — we just confirm our chrome.
/// </summary>
public class OmniVirtualizeTests : TestContextBase
{
    private static readonly List<string> SampleItems = new() { "alpha", "beta", "gamma" };

    [Fact]
    public void Renders_root_div_with_base_class()
    {
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, SampleItems)
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<span>{item}</span>")));

        var root = cut.Find("div.omni-virtualize");
        Assert.Contains("omni-virtualize", root.ClassName);
    }

    [Fact]
    public void Renders_empty_state_text_when_Items_empty()
    {
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, new List<string>())
            .Add(c => c.DefaultEmptyText, "Nothing here")
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<span>{item}</span>")));

        Assert.Contains("Nothing here", cut.Find("div.omni-virtualize-empty").TextContent);
    }

    [Fact]
    public void Renders_custom_EmptyContent_when_set()
    {
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, new List<string>())
            .Add(c => c.EmptyContent, b => b.AddMarkupContent(0, "<div class='my-empty'>nada</div>"))
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<span>{item}</span>")));

        Assert.NotNull(cut.Find("div.my-empty"));
        Assert.Empty(cut.FindAll("div.omni-virtualize-empty"));
    }

    [Fact]
    public void Applies_height_to_wrapper_style()
    {
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, SampleItems)
            .Add(c => c.Height, "200px")
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<span>{item}</span>")));

        var style = cut.Find("div.omni-virtualize").GetAttribute("style") ?? "";
        Assert.Contains("height:200px", style);
        Assert.Contains("overflow:auto", style);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, SampleItems)
            .Add(c => c.Class, "v-cls")
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<span>{item}</span>")));

        Assert.Contains("v-cls", cut.Find("div.omni-virtualize").ClassName);
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, SampleItems)
            .AddUnmatched("data-testid", "v1")
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<span>{item}</span>")));

        Assert.Equal("v1", cut.Find("div.omni-virtualize").GetAttribute("data-testid"));
    }

    [Fact]
    public void SpacerElement_tr_skips_wrapper_div()
    {
        // Mounted inside a fake tbody — we just confirm no .omni-virtualize wrapper is emitted.
        var cut = RenderComponent<OmniVirtualize<string>>(p => p
            .Add(c => c.Items, SampleItems)
            .Add(c => c.SpacerElement, "tr")
            .Add(c => c.ItemContent, item => b => b.AddMarkupContent(0, $"<tr><td>{item}</td></tr>")));

        Assert.Empty(cut.FindAll("div.omni-virtualize"));
    }
}
