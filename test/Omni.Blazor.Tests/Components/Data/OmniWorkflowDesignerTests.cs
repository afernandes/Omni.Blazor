using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniWorkflowDesignerTests : TestContextBase
{
    private sealed class Step
    {
        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public void Renders_typed_inspector_validation_and_cross_cutting_attributes()
    {
        DiagramNode node = new() { Id = "start", Title = "Start", Data = new Step { Name = "Start" } };
        DataFormSchema<Step> inspector = DataFormSchema<Step>.Create(form => form.Field(step => step.Name));

        var cut = Render<OmniWorkflowDesigner<Step>>(parameters => parameters
            .Add(component => component.Nodes, new[] { node })
            .Add(component => component.Selection, new DiagramSelection(["start"]))
            .Add(component => component.InspectorSchema, inspector)
            .Add(component => component.Validator, static (_, _) => ["Missing terminal step."])
            .Add(component => component.Class, "custom-workflow")
            .Add(component => component.Style, "min-height:600px")
            .AddUnmatched("data-testid", "workflow"));

        Assert.Contains("custom-workflow", cut.Find(".omni-workflow-designer").ClassName);
        Assert.Equal("workflow", cut.Find(".omni-workflow-designer").GetAttribute("data-testid"));
        Assert.Contains("Missing terminal step.", cut.Find("[role=alert]").TextContent);
        Assert.NotNull(cut.Find(".omni-property-grid"));
    }

    [Fact]
    public async Task Move_undo_and_redo_publish_immutable_snapshots()
    {
        DiagramNode node = new() { Id = "start", Title = "Start", X = 10, Y = 20 };
        List<IReadOnlyList<DiagramNode>> snapshots = [];
        var cut = Render<OmniWorkflowDesigner<Step>>(parameters => parameters
            .Add(component => component.Nodes, new[] { node })
            .Add(component => component.NodesChanged, nodes => snapshots.Add(nodes)));
        OmniDiagramCanvas canvas = cut.FindComponent<OmniDiagramCanvas>().Instance;

        await cut.InvokeAsync(() => canvas.NodesMoved.InvokeAsync([new DiagramNodeMove("start", 100, 120)]));
        Assert.True(cut.Instance.CanUndo);
        Assert.Equal(100, snapshots[^1][0].X);

        await cut.InvokeAsync(cut.Instance.UndoAsync);
        Assert.True(cut.Instance.CanRedo);
        Assert.Equal(10, snapshots[^1][0].X);

        await cut.InvokeAsync(cut.Instance.RedoAsync);
        Assert.Equal(100, snapshots[^1][0].X);
    }

    [Fact]
    public void Rejects_unbounded_history_values()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Render<OmniWorkflowDesigner<Step>>(parameters => parameters
            .Add(component => component.MaximumHistory, 201)));
    }
}
