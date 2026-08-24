using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class FluentSchemaIntegrationTests : TestContextBase
{
    private sealed record Product(int Id, string Name, decimal Price);

    private sealed record Appointment(DateTime BeginsAt, DateTime FinishesAt, string Subject);

    private sealed record TaskItem(int Id, int? ParentId, string Name, DateTime BeginsAt, DateTime FinishesAt, double Completion);

    private sealed class WorkCard
    {
        public int Id { get; init; }

        public string State { get; set; } = "todo";

        public string Title { get; init; } = string.Empty;
    }

    private sealed record Metric(string Month, double Amount, string? Color = null);

    [Fact]
    public void DataGrid_schema_renders_typed_columns_and_behavior()
    {
        DataGridSchema<Product> schema = DataGridSchema<Product>.Create(grid => grid
            .Key(product => product.Id)
            .Column(product => product.Name, column => column.Title("Product").Width("220px"))
            .Column(product => product.Price, column => column.Title("Price").Format("C2"))
            .Search(placeholder: "Find products")
            .ColumnResize()
            .KeyboardNavigation(selectionFollowsFocus: false));

        var cut = Render<OmniDataGrid<Product>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Data, [new Product(1, "Keyboard", 125m)]));

        Assert.Contains("Product", cut.Find("thead").TextContent);
        Assert.Contains("Price", cut.Find("thead").TextContent);
        Assert.Contains("Keyboard", cut.Find("tbody").TextContent);
        Assert.Equal("Find products", cut.Find(".omni-grid-search input").GetAttribute("placeholder"));
        Assert.NotEmpty(cut.FindAll(".omni-grid-resizer"));
        Assert.Equal("grid", cut.Find("table").GetAttribute("role"));
        Assert.False(cut.Instance.SelectionFollowsFocus);
    }

    [Fact]
    public void DataGrid_schema_rejects_duplicate_columns_and_builder_reuse()
    {
        DataGridSchemaBuilder<Product> builder = DataGridSchema<Product>.Builder();
        builder.Column(product => product.Name);

        Assert.Throws<InvalidOperationException>(() => builder.Column(product => product.Name));
        _ = builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Search());
    }

    [Fact]
    public void DataGrid_schema_reference_change_replaces_generated_columns()
    {
        DataGridSchema<Product> first = DataGridSchema<Product>.Create(grid => grid
            .Column(product => product.Name, column => column.Title("Before")));
        DataGridSchema<Product> second = DataGridSchema<Product>.Create(grid => grid
            .Column(product => product.Name, column => column.Title("After")));
        Product[] data = [new(1, "Keyboard", 125m)];
        var cut = Render<OmniDataGrid<Product>>(parameters => parameters
            .Add(component => component.Schema, first)
            .Add(component => component.Data, data));

        cut.Render(parameters => parameters
            .Add(component => component.Schema, second)
            .Add(component => component.Data, data));

        cut.WaitForAssertion(() => Assert.Contains("After", cut.Find("thead").TextContent));
        Assert.DoesNotContain("Before", cut.Find("thead").TextContent);
    }

    [Fact]
    public void Scheduler_schema_projects_appointments_without_reflection_names()
    {
        DateTime today = DateTime.Today;
        SchedulerSchema<Appointment> schema = SchedulerSchema<Appointment>.Create(scheduler => scheduler
            .Range(item => item.BeginsAt, item => item.FinishesAt)
            .Text(item => item.Subject)
            .Height("420px"));

        var cut = Render<OmniScheduler<Appointment>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Data,
                [new Appointment(today.AddHours(9), today.AddHours(10), "Schema planning")])
            .Add(component => component.ChildContent, SchedulerViews()));

        Assert.Contains("Schema planning", cut.Markup);
        Assert.Contains("height:420px", cut.Find(".omni-scheduler").GetAttribute("style"));
    }

    [Fact]
    public void Gantt_schema_projects_tasks_and_hierarchy_without_property_names()
    {
        DateTime today = DateTime.Today;
        GanttSchema<TaskItem> schema = GanttSchema<TaskItem>.Create(gantt => gantt
            .Hierarchy(item => item.Id, item => item.ParentId)
            .Task(item => item.Name, item => item.BeginsAt, item => item.FinishesAt)
            .Progress(item => item.Completion)
            .Timeline(rowHeight: 40, showNavigation: false));

        var cut = Render<OmniGantt<TaskItem>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Data,
            [
                new TaskItem(1, null, "Release", today, today.AddDays(4), 50),
                new TaskItem(2, 1, "Verification", today.AddDays(1), today.AddDays(2), 25)
            ]));

        Assert.Equal(2, cut.FindAll(".omni-gantt-left-row").Count);
        Assert.NotEmpty(cut.FindAll(".omni-gantt-bar"));
        Assert.Empty(cut.FindAll(".omni-gantt-nav"));
    }

    [Fact]
    public void Kanban_schema_renders_typed_workflow_and_default_cards()
    {
        KanbanSchema<WorkCard> schema = KanbanSchema<WorkCard>.Create(kanban => kanban
            .ColumnState(card => card.State, (card, state) => card.State = state)
            .Key(card => card.Id)
            .Column("todo", "To do")
            .Column("done", "Done")
            .Card(card => card.Title(item => item.Title)));

        var cut = Render<OmniKanban<WorkCard>>(parameters => parameters
            .Add(component => component.Schema, schema)
            .Add(component => component.Items,
                [new WorkCard { Id = 1, State = "todo", Title = "Typed card" }]));

        Assert.Equal(2, cut.FindAll(".omni-kanban-col").Count);
        Assert.Contains("Typed card", cut.Find(".omni-kanban-card").TextContent);
    }

    [Fact]
    public void Chart_schema_snapshots_typed_data_and_renders_it()
    {
        List<Metric> source =
        [
            new("Jan", 10, "#123456"),
            new("Feb", 15, "#654321")
        ];
        ChartSchema schema = ChartSchema.Create(chart => chart
            .Series("Revenue", source, item => item.Month, item => item.Amount,
                series => series.Type(ChartSeriesType.Column).PointColor(item => item.Color))
            .Size("180px", "480px")
            .Labels(ariaLabel: "Monthly revenue"));
        source.Clear();

        var cut = Render<OmniChart>(parameters => parameters.Add(component => component.Schema, schema));

        Assert.Equal(2, schema.Series[0].Points.Count);
        Assert.Equal(2, cut.FindAll("rect").Count(rect => rect.QuerySelector("title") is not null));
        Assert.Equal("Monthly revenue", cut.Find("svg").GetAttribute("aria-label"));
        Assert.Contains("height:180px", cut.Find(".omni-chart").GetAttribute("style"));
    }

    [Fact]
    public void Diagram_schema_validates_connections_and_renders_graph()
    {
        DiagramSchema schema = DiagramSchema.Create(diagram => diagram
            .Node("source", node => node.Text("Source").Output("Done", DiagramPortKind.Success))
            .Node("target", new { Kind = "Sink" }, node => node.Text("Target"))
            .Connect("edge", "source", "Done", "target")
            .AutoLayout()
            .Behavior(showMinimap: false));

        var cut = Render<OmniDiagramCanvas>(parameters => parameters.Add(component => component.Schema, schema));

        Assert.Equal(2, cut.FindAll("[data-dgnode]").Count);
        Assert.Single(cut.FindAll("[data-dgedge]"));
        Assert.Empty(cut.FindAll(".omni-diagram-minimap"));

        Assert.Throws<InvalidOperationException>(() => DiagramSchema.Create(diagram => diagram
            .Node("source", node => node.Output("Done"))
            .Connect("invalid", "source", "Missing", "unknown")));
    }

    private static RenderFragment SchedulerViews() => builder =>
    {
        builder.OpenComponent<OmniDayView>(0);
        builder.CloseComponent();
        builder.OpenComponent<OmniWeekView>(1);
        builder.CloseComponent();
    };
}
