using Bunit;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

public sealed class OmniEntityEditorAdapterTests : TestContextBase
{
    private sealed class Appointment
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    private sealed class Card
    {
        public int Id { get; set; }
        public string Status { get; set; } = "todo";
        public string Title { get; set; } = string.Empty;
    }

    private sealed class WorkItem
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }

    [Fact]
    public async Task SchedulerForm_opens_a_detached_DataForm_editor()
    {
        SchedulerSchema<Appointment> scheduler = SchedulerSchema<Appointment>.Create(builder => builder
            .Range(item => item.Start, item => item.End)
            .Text(item => item.Title));
        EntityEditorSchema<Appointment, int> editor = EditorFor(
            static () => new Appointment { Start = DateTime.Today.AddHours(9), End = DateTime.Today.AddHours(10) },
            static item => new Appointment { Id = item.Id, Title = item.Title, Start = item.Start, End = item.End });
        List<Appointment> items = [];

        var cut = Render<OmniSchedulerForm<Appointment, int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.SchedulerSchema, scheduler)
            .Add(component => component.EditorSchema, editor)
            .Add(component => component.Class, "scheduler-editor")
            .AddUnmatched("data-testid", "scheduler-form"));

        await cut.InvokeAsync(cut.Instance.BeginCreateAsync);

        Assert.Contains("scheduler-editor", cut.Find(".omni-entity-editor").ClassName);
        Assert.Equal("scheduler-form", cut.Find(".omni-entity-editor").GetAttribute("data-testid"));
        Assert.NotNull(cut.Find(".omni-data-grid-form-editor"));
    }

    [Fact]
    public async Task SchedulerForm_reprojects_dates_after_a_valid_local_edit()
    {
        DateTime originalStart = DateTime.Today.AddHours(9);
        DateTime expectedStart = DateTime.Today.AddDays(1).AddHours(11).AddMinutes(30);
        DateTime expectedEnd = expectedStart.AddHours(2);
        List<Appointment> items =
        [
            new() { Id = 1, Title = "Planning", Start = originalStart, End = originalStart.AddHours(1) }
        ];
        SchedulerSchema<Appointment> scheduler = SchedulerSchema<Appointment>.Create(builder => builder
            .Range(item => item.Start, item => item.End)
            .Text(item => item.Title));
        DataFormSchema<Appointment> form = DataFormSchema<Appointment>.Create(builder => builder
            .Field(item => item.Start)
            .Field(item => item.End));
        EntityEditorSchema<Appointment, int> editor = EntityEditorSchema<Appointment, int>.Create(builder => builder
            .Key(item => item.Id)
            .Form(form)
            .Edit(static item => new Appointment
            {
                Id = item.Id,
                Title = item.Title,
                Start = item.Start,
                End = item.End
            }, presentation: EntityEditorPresentation.Inline));
        var cut = Render<OmniSchedulerForm<Appointment, int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.SchedulerSchema, scheduler)
            .Add(component => component.EditorSchema, editor));

        await cut.InvokeAsync(() => cut.Instance.BeginEditAsync(items[0]));
        var inputs = cut.FindAll(".omni-datepicker-input");
        inputs[0].Input(expectedStart.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture));
        inputs[1].Input(expectedEnd.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Salvar", StringComparison.Ordinal)).Click();

        Assert.Equal(expectedStart, items[0].Start);
        Assert.Equal(expectedEnd, items[0].End);
        OmniScheduler<Appointment> renderedScheduler = cut.FindComponent<OmniScheduler<Appointment>>().Instance;
        Assert.Equal(expectedStart, renderedScheduler.AppointmentsInternal.Single().Start);
        Assert.Equal(expectedEnd, renderedScheduler.AppointmentsInternal.Single().End);
    }

    [Fact]
    public async Task KanbanForm_reuses_the_same_editor_contract()
    {
        KanbanSchema<Card> kanban = KanbanSchema<Card>.Create(builder => builder
            .ColumnState(item => item.Status, static (item, status) => item.Status = status)
            .Key(item => item.Id)
            .Column("todo", "To do")
            .Card(card => card.Title(item => item.Title)));
        EntityEditorSchema<Card, int> editor = EditorFor(
            static () => new Card(),
            static item => new Card { Id = item.Id, Status = item.Status, Title = item.Title });
        List<Card> items = [];

        var cut = Render<OmniKanbanForm<Card, int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.KanbanSchema, kanban)
            .Add(component => component.EditorSchema, editor));

        await cut.InvokeAsync(cut.Instance.BeginCreateAsync);

        Assert.NotNull(cut.Find(".omni-kanban"));
        Assert.NotNull(cut.Find(".omni-data-grid-form-editor"));
    }

    [Fact]
    public async Task GanttForm_reuses_the_same_editor_contract()
    {
        DateTime today = DateTime.Today;
        GanttSchema<WorkItem> gantt = GanttSchema<WorkItem>.Create(builder => builder
            .Hierarchy(item => item.Id, item => item.ParentId)
            .Task(item => item.Title, item => item.Start, item => item.End));
        EntityEditorSchema<WorkItem, int> editor = EditorFor(
            () => new WorkItem { Start = today, End = today.AddDays(1) },
            static item => new WorkItem { Id = item.Id, ParentId = item.ParentId, Title = item.Title, Start = item.Start, End = item.End });
        List<WorkItem> items = [];

        var cut = Render<OmniGanttForm<WorkItem, int>>(parameters => parameters
            .Add(component => component.Items, items)
            .Add(component => component.GanttSchema, gantt)
            .Add(component => component.EditorSchema, editor));

        await cut.InvokeAsync(cut.Instance.BeginCreateAsync);

        Assert.NotNull(cut.Find(".omni-gantt"));
        Assert.NotNull(cut.Find(".omni-data-grid-form-editor"));
    }

    private static EntityEditorSchema<TItem, int> EditorFor<TItem>(Func<TItem> factory, Func<TItem, TItem> clone)
        where TItem : class
        => EntityEditorSchema<TItem, int>.Create(editor => editor
            .Key(item => (int)(item.GetType().GetProperty("Id")!.GetValue(item) ?? 0))
            .Form(DataFormSchema<TItem>.Create(static _ => { }))
            .Create(factory, presentation: EntityEditorPresentation.Inline)
            .Edit(clone, presentation: EntityEditorPresentation.Inline));
}
