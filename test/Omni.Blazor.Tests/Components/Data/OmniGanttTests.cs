using Bunit;
using Microsoft.AspNetCore.Components;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Tests.Components.Data;

/// <summary>
/// Behavioural contract for <see cref="OmniGantt{TItem}"/>: split view root,
/// hierarchy flattening, task bars, zoom, dependencies, critical path and the
/// cross-cutting splat.
/// </summary>
public class OmniGanttTests : TestContextBase
{
    public class GTask
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Name { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public double? Progress { get; set; }
    }

    private static List<GTask> Sample()
    {
        var t = DateTime.Today;
        return new()
        {
            new() { Id = 1, Name = "Phase", Start = t, End = t.AddDays(10), Progress = 30 },
            new() { Id = 2, ParentId = 1, Name = "A", Start = t, End = t.AddDays(4), Progress = 100 },
            new() { Id = 3, ParentId = 1, Name = "B", Start = t.AddDays(4), End = t.AddDays(10), Progress = 0 },
        };
    }

    private static RenderFragment Columns() => builder =>
    {
        builder.OpenComponent<OmniGanttColumn<GTask>>(0);
        builder.AddAttribute(1, nameof(OmniGanttColumn<GTask>.Title), "Tarefa");
        builder.AddAttribute(2, nameof(OmniGanttColumn<GTask>.Property), (Func<GTask, object?>)(t => t.Name));
        builder.CloseComponent();
    };

    private IRenderedComponent<OmniGantt<GTask>> RenderGantt(
        Action<ComponentParameterCollectionBuilder<OmniGantt<GTask>>>? extra = null,
        List<GTask>? data = null)
        => RenderComponent<OmniGantt<GTask>>(p =>
        {
            p.Add(g => g.Data, data ?? Sample());
            p.Add(g => g.IdProperty, "Id");
            p.Add(g => g.ParentIdProperty, "ParentId");
            p.Add(g => g.TextProperty, "Name");
            p.Add(g => g.StartProperty, "Start");
            p.Add(g => g.EndProperty, "End");
            p.Add(g => g.ProgressProperty, "Progress");
            p.Add(g => g.ChildContent, Columns());
            extra?.Invoke(p);
        });

    // ─── Cross-cutting ────────────────────────────────────────────────────

    [Fact]
    public void Renders_root_nav_and_panes()
    {
        var cut = RenderGantt();
        Assert.NotNull(cut.Find("div.omni-gantt"));
        Assert.NotNull(cut.Find(".omni-gantt-nav"));
        Assert.NotNull(cut.Find(".omni-gantt-left"));
        Assert.NotNull(cut.Find(".omni-gantt-timeline"));
    }

    [Fact]
    public void Appends_consumer_Class_to_root()
    {
        var cut = RenderGantt(p => p.Add(g => g.Class, "my-gantt"));
        Assert.Contains("my-gantt", cut.Find("div.omni-gantt").ClassName);
    }

    [Fact]
    public void Forwards_consumer_Style_to_root()
    {
        var cut = RenderGantt(p => p.Add(g => g.Style, "height:400px"));
        Assert.Contains("height:400px", cut.Find("div.omni-gantt").GetAttribute("style") ?? "");
    }

    [Fact]
    public void Splats_unmatched_Attributes_onto_root()
    {
        var cut = RenderGantt(p => p.AddUnmatched("data-testid", "g1"));
        Assert.Equal("g1", cut.Find("div.omni-gantt").GetAttribute("data-testid"));
    }

    // ─── Hierarchy / rows ─────────────────────────────────────────────────

    [Fact]
    public void Flattens_hierarchy_expanded_by_default()
    {
        var cut = RenderGantt();
        // Parent + 2 children, all expanded.
        Assert.Equal(3, cut.FindAll(".omni-gantt-left-row").Count);
        Assert.Equal(3, cut.Instance.RowCountInternal);
    }

    [Fact]
    public void Collapsing_a_parent_hides_its_children()
    {
        var cut = RenderGantt();
        cut.Find(".omni-gantt-chevron").Click();
        Assert.Equal(1, cut.FindAll(".omni-gantt-left-row").Count);
    }

    [Fact]
    public void Renders_a_bar_per_task()
    {
        var cut = RenderGantt();
        Assert.Equal(3, cut.FindAll(".omni-gantt-bar").Count);
    }

    [Fact]
    public void Renders_progress_fill()
    {
        var cut = RenderGantt();
        Assert.NotEmpty(cut.FindAll(".omni-gantt-bar-progress"));
    }

    [Fact]
    public void Renders_a_milestone_for_zero_duration_task()
    {
        var t = DateTime.Today;
        var data = new List<GTask>
        {
            new() { Id = 1, Name = "Kickoff", Start = t.AddDays(2), End = t.AddDays(2) },
        };
        var cut = RenderGantt(data: data);
        Assert.Single(cut.FindAll(".omni-gantt-milestone"));
    }

    // ─── Zoom ─────────────────────────────────────────────────────────────

    [Fact]
    public void Renders_four_zoom_buttons_with_week_active_by_default()
    {
        var cut = RenderGantt();
        var buttons = cut.FindAll(".omni-gantt-zoom-btn");
        Assert.Equal(4, buttons.Count);
        Assert.Contains(buttons, b => b.ClassName.Contains("omni-active") && b.TextContent.Trim() == "Semana");
    }

    [Fact]
    public void Year_zoom_uses_coarser_columns_than_week_zoom()
    {
        // Week → day columns; Year → month columns (far fewer, like Radzen).
        var week = RenderGantt(p => p.Add(g => g.ZoomLevel, GanttZoomLevel.Week));
        var year = RenderGantt(p => p.Add(g => g.ZoomLevel, GanttZoomLevel.Year));

        var weekCols = week.FindAll(".omni-gantt-tl-dayhead").Count;
        var yearCols = year.FindAll(".omni-gantt-tl-dayhead").Count;

        Assert.True(yearCols < weekCols, $"year={yearCols} should be < week={weekCols}");
        Assert.True(yearCols <= 12, $"year columns should be months (<=12), got {yearCols}");
    }

    [Fact]
    public void Clicking_a_zoom_button_activates_it()
    {
        var cut = RenderGantt();
        var dia = cut.FindAll(".omni-gantt-zoom-btn").First(b => b.TextContent.Trim() == "Dia");
        dia.Click();
        Assert.Contains("omni-active", cut.FindAll(".omni-gantt-zoom-btn").First(b => b.TextContent.Trim() == "Dia").ClassName);
    }

    // ─── Dependencies / critical path ─────────────────────────────────────

    [Fact]
    public void Renders_dependency_links()
    {
        var data = Sample();
        var deps = new List<GanttDependency<GTask>> { new() { From = data[1], To = data[2] } };
        var cut = RenderGantt(p => p.Add(g => g.Dependencies, deps), data: data);
        Assert.NotEmpty(cut.FindAll("path.omni-gantt-link"));
    }

    [Fact]
    public void Critical_path_marks_bars()
    {
        var data = Sample();
        var deps = new List<GanttDependency<GTask>> { new() { From = data[1], To = data[2] } };
        var cut = RenderGantt(p => p
            .Add(g => g.Dependencies, deps)
            .Add(g => g.ShowCriticalPath, true), data: data);

        Assert.NotEmpty(cut.FindAll(".omni-gantt-bar.omni-critical"));
    }

    // ─── Callbacks ────────────────────────────────────────────────────────

    [Fact]
    public void Clicking_a_bar_fires_TaskClick()
    {
        GTask? captured = null;
        var cut = RenderGantt(p => p.Add(g => g.TaskClick,
            EventCallback.Factory.Create<GTask>(this, t => captured = t)));

        cut.Find(".omni-gantt-bar").Click();
        Assert.NotNull(captured);
    }

    [Fact]
    public void TaskMove_handler_makes_bars_draggable()
    {
        var cut = RenderGantt(p => p.Add(g => g.TaskMove,
            EventCallback.Factory.Create<GanttTaskMovedEventArgs<GTask>>(this, _ => { })));
        Assert.Equal("true", cut.Find(".omni-gantt-bar").GetAttribute("draggable"));
    }
}
