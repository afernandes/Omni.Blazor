using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniDropZoneContainer{TItem}"/>:
/// renders a wrapper div, cascades itself, and supports the cross-cutting splat.
/// </summary>
public class OmniDropZoneContainerTests : TestContextBase
{
    private record Task1(string Title, string Status);

    [Fact]
    public void Renders_root_div_with_base_class()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, new[] { new Task1("a", "todo") })
            .AddChildContent("body"));

        var root = cut.Find("div.omni-dropzone-container");
        Assert.Contains("omni-dropzone-container", root.ClassName);
    }

    [Fact]
    public void Renders_child_content()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, new[] { new Task1("a", "todo") })
            .AddChildContent("<span class='kid'>x</span>"));

        Assert.NotNull(cut.Find("span.kid"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, new[] { new Task1("a", "todo") })
            .Add(c => c.Class, "kanban")
            .AddChildContent("x"));

        Assert.Contains("kanban", cut.Find("div.omni-dropzone-container").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, new[] { new Task1("a", "todo") })
            .Add(c => c.Style, "gap: 12px")
            .AddChildContent("x"));

        Assert.Equal("gap: 12px", cut.Find("div.omni-dropzone-container").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, new[] { new Task1("a", "todo") })
            .AddUnmatched("data-testid", "kanban1")
            .AddChildContent("x"));

        Assert.Equal("kanban1", cut.Find("div.omni-dropzone-container").GetAttribute("data-testid"));
    }
}
