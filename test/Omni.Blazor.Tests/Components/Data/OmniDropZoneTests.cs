using Bunit;
using Omni.Blazor.Components;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniDropZone{TItem}"/>: renders a bucket
/// div, filters items via the parent container's ItemSelector, and supports the
/// cross-cutting splat. The DropZone must live inside an
/// <see cref="OmniDropZoneContainer{TItem}"/>.
/// </summary>
public class OmniDropZoneTests : TestContextBase
{
    private record Task1(string Title, string Status);

    private static readonly Task1[] Sample =
    {
        new("a", "todo"),
        new("b", "doing"),
        new("c", "todo")
    };

    [Fact]
    public void Renders_dropzone_div_with_base_class_inside_container()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")
                .AddChildContent("zone body")));

        var zone = cut.Find("div.omni-dropzone");
        Assert.Contains("omni-dropzone", zone.ClassName);
    }

    [Fact]
    public void Filters_items_by_ItemSelector()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .Add(c => c.Template, item => b => b.AddMarkupContent(0, $"<span class='it'>{item.Title}</span>"))
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")));

        var items = cut.FindAll(".it");
        Assert.Equal(2, items.Count); // "a" and "c"
    }

    [Fact]
    public void Renders_zone_Footer_after_items()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")
                .Add(x => x.Footer, b => b.AddMarkupContent(0, "<button class='add-btn'>+</button>"))));

        Assert.NotNull(cut.Find("button.add-btn"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")
                .Add(x => x.Class, "zone-todo")));

        Assert.Contains("zone-todo", cut.Find("div.omni-dropzone").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")
                .Add(x => x.Style, "min-height: 200px")));

        Assert.Equal("min-height: 200px", cut.Find("div.omni-dropzone").GetAttribute("style"));
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = Render<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")
                .AddUnmatched("data-testid", "z1")));

        Assert.Equal("z1", cut.Find("div.omni-dropzone").GetAttribute("data-testid"));
    }
}
