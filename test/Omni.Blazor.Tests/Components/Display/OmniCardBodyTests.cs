using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Display;

/// <summary>
/// Behavioural contract for <see cref="OmniCardBody"/>: NoPadding, Divided,
/// cross-cutting splat.
/// </summary>
public class OmniCardBodyTests : TestContextBase
{
    [Fact]
    public void Renders_default_with_base_classes()
    {
        var cut = RenderComponent<OmniCardBody>(p => p.AddChildContent("body"));

        var root = cut.Find("div.omni-card-part");
        Assert.Contains("omni-card-part", root.ClassName);
        Assert.Contains("omni-card-body", root.ClassName);
        Assert.Contains("body", root.TextContent);
    }

    [Fact]
    public void NoPadding_adds_flush_modifier()
    {
        var cut = RenderComponent<OmniCardBody>(p => p
            .Add(c => c.NoPadding, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-body-flush", cut.Find("div.omni-card-part").ClassName);
    }

    [Fact]
    public void Divided_adds_divider_modifier()
    {
        var cut = RenderComponent<OmniCardBody>(p => p
            .Add(c => c.Divided, true)
            .AddChildContent("x"));

        Assert.Contains("omni-card-body-divided", cut.Find("div.omni-card-part").ClassName);
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderComponent<OmniCardBody>(p => p
            .Add(c => c.Class, "my-body")
            .AddChildContent("x"));

        Assert.Contains("my-body", cut.Find("div.omni-card-part").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderComponent<OmniCardBody>(p => p
            .Add(c => c.Style, "padding: 24px")
            .AddChildContent("x"));

        Assert.Equal("padding: 24px", cut.Find("div.omni-card-part").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderComponent<OmniCardBody>(p => p
            .AddUnmatched("data-testid", "cb1")
            .AddChildContent("x"));

        Assert.Equal("cb1", cut.Find("div.omni-card-part").GetAttribute("data-testid"));
    }
}
