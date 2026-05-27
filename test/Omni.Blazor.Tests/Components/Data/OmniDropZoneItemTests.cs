using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniDropZoneItem{TItem}"/>: the internal
/// draggable rendered by <see cref="OmniDropZone{TItem}"/> for each item. We
/// only exercise it through the public DropZone tree because it cascades its
/// item/zone parameters.
/// </summary>
public class OmniDropZoneItemTests : TestContextBase
{
    private record Task1(string Title, string Status);

    private static readonly Task1[] Sample =
    {
        new("a", "todo"),
        new("b", "todo")
    };

    [Fact]
    public void Renders_item_div_with_base_class_per_item()
    {
        var cut = RenderComponent<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .AddChildContent<OmniDropZone<Task1>>(z => z
                .Add(x => x.Value, "todo")));

        var items = cut.FindAll("div.omni-dropzone-item");
        Assert.Equal(2, items.Count);
        foreach (var it in items)
        {
            Assert.Contains("omni-dropzone-item", it.ClassName);
            Assert.Equal("true", it.GetAttribute("draggable"));
        }
    }

    [Fact]
    public void Renders_container_Template_per_item()
    {
        var cut = RenderComponent<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .Add(c => c.Template, item => b => b.AddMarkupContent(0, $"<strong>{item.Title}</strong>"))
            .AddChildContent<OmniDropZone<Task1>>(z => z.Add(x => x.Value, "todo")));

        Assert.Equal(2, cut.FindAll("div.omni-dropzone-item strong").Count);
        Assert.Contains("a", cut.FindAll("div.omni-dropzone-item strong")[0].TextContent);
        Assert.Contains("b", cut.FindAll("div.omni-dropzone-item strong")[1].TextContent);
    }

    [Fact]
    public void ItemRender_hook_can_hide_item()
    {
        var cut = RenderComponent<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .Add(c => c.ItemRender, args =>
            {
                if (args.Item?.Title == "a") args.Visible = false;
            })
            .AddChildContent<OmniDropZone<Task1>>(z => z.Add(x => x.Value, "todo")));

        Assert.Single(cut.FindAll("div.omni-dropzone-item"));
    }

    [Fact]
    public void ItemRender_hook_can_add_attributes()
    {
        var cut = RenderComponent<OmniDropZoneContainer<Task1>>(p => p
            .Add(c => c.Data, Sample)
            .Add(c => c.ItemSelector, (item, zone) => item.Status == (string?)zone.Value)
            .Add(c => c.ItemRender, args =>
            {
                args.Attributes["data-title"] = args.Item?.Title ?? "";
            })
            .AddChildContent<OmniDropZone<Task1>>(z => z.Add(x => x.Value, "todo")));

        var items = cut.FindAll("div.omni-dropzone-item");
        Assert.Equal("a", items[0].GetAttribute("data-title"));
        Assert.Equal("b", items[1].GetAttribute("data-title"));
    }
}
