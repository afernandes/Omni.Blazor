using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Forms;

public sealed class OmniPropertyGridTests : TestContextBase
{
    private sealed class Settings
    {
        public string Name { get; set; } = "Default";
    }

    [Fact]
    public void Renders_compact_typed_inspector_and_cross_cutting_attributes()
    {
        DataFormSchema<Settings> schema = DataFormSchema<Settings>.Create(form => form.Field(model => model.Name));

        var cut = Render<OmniPropertyGrid<Settings>>(parameters => parameters
            .Add(component => component.Model, new Settings())
            .Add(component => component.Schema, schema)
            .Add(component => component.Title, "Settings")
            .Add(component => component.Class, "custom-grid")
            .Add(component => component.Style, "min-width:300px")
            .AddUnmatched("data-testid", "property-grid"));

        Assert.Contains("custom-grid", cut.Find(".omni-property-grid").ClassName);
        Assert.Contains("min-width:300px", cut.Find(".omni-property-grid").GetAttribute("style"));
        Assert.Equal("property-grid", cut.Find(".omni-property-grid").GetAttribute("data-testid"));
        Assert.Equal("Settings", cut.Find(".omni-property-grid-header h2").TextContent);
        Assert.True(cut.Find("input").HasAttribute("readonly"));
    }

    [Fact]
    public void Field_subscription_moves_to_the_active_context_and_is_removed_on_dispose()
    {
        Settings model = new();
        EditContext context = new(model);
        int changes = 0;
        var cut = Render<OmniPropertyGrid<Settings>>(parameters => parameters
            .Add(component => component.Model, model)
            .Add(component => component.EditContext, context)
            .Add(component => component.Editable, true)
            .Add(component => component.PropertyChanged, _ => changes++));

        cut.InvokeAsync(() => context.NotifyFieldChanged(context.Field(nameof(Settings.Name))));
        Assert.Equal(1, changes);

        cut.Instance.Dispose();
        context.NotifyFieldChanged(context.Field(nameof(Settings.Name)));
        Assert.Equal(1, changes);
    }
}
