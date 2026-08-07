using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniEntityEditorHostTests : TestContextBase
{
    private sealed record Item(int Id);

    [Fact]
    public void Renders_internal_surface_and_cross_cutting_attributes()
    {
        EntityEditorSchema<Item, int> schema = EntityEditorSchema<Item, int>.Create(editor => editor
            .Key(item => item.Id));

        var cut = Render<OmniEntityEditorHost<Item, int>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items, new List<Item>())
            .Add(component => component.Class, "host-class")
            .AddUnmatched("data-testid", "entity-host"));

        Assert.Contains("host-class", cut.Find(".omni-entity-editor").ClassName);
        Assert.Equal("entity-host", cut.Find(".omni-entity-editor").GetAttribute("data-testid"));
    }
}
