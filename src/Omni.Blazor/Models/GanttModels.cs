namespace Omni.Blazor.Models;

/// <summary>Zoom level (time scale) of an <c>OmniGantt</c> timeline.</summary>
public enum GanttZoomLevel
{
    /// <summary>One wide cell per day.</summary>
    Day,
    /// <summary>Day cells, sized for a few weeks.</summary>
    Week,
    /// <summary>Narrow day cells, sized for a few months.</summary>
    Month,
    /// <summary>Very narrow day cells, sized for a year+.</summary>
    Year,
}

/// <summary>Type of a Gantt dependency link.</summary>
public enum GanttDependencyType
{
    /// <summary>Finish-to-Start: successor starts after predecessor finishes.</summary>
    FinishToStart,
    /// <summary>Start-to-Start: successor starts after predecessor starts.</summary>
    StartToStart,
    /// <summary>Finish-to-Finish: successor finishes after predecessor finishes.</summary>
    FinishToFinish,
    /// <summary>Start-to-Finish: successor finishes after predecessor starts.</summary>
    StartToFinish,
}

/// <summary>A dependency between two Gantt tasks.</summary>
public class GanttDependency<TItem>
{
    public TItem From { get; set; } = default!;
    public TItem To { get; set; } = default!;
    public GanttDependencyType Type { get; set; } = GanttDependencyType.FinishToStart;
}

/// <summary>A named vertical line drawn on the Gantt timeline (deadline, release, …).</summary>
public class GanttMarker
{
    public DateTime Date { get; set; }
    public string? Label { get; set; }
    /// <summary>CSS color for the line/label. Defaults to the danger color.</summary>
    public string? Color { get; set; }
}

/// <summary>Per-bar render customization hook for <c>OmniGantt.TaskRender</c>.</summary>
public class GanttBarRenderEventArgs<TItem>
{
    public TItem Data { get; set; } = default!;
    /// <summary>Extra CSS classes for the task bar.</summary>
    public string CssClass { get; set; } = string.Empty;
    /// <summary>Extra HTML attributes (e.g. <c>style</c>) for the bar element.</summary>
    public Dictionary<string, object> Attributes { get; set; } = new();
}

/// <summary>Args for <c>OmniGantt.TaskMove</c> / <c>TaskResize</c>.</summary>
public class GanttTaskMovedEventArgs<TItem>
{
    public TItem Data { get; set; } = default!;
    public DateTime NewStart { get; set; }
    public DateTime NewEnd { get; set; }
}

/// <summary>Args for <c>OmniGantt.TaskMouseEnter</c> / <c>TaskMouseLeave</c> (for tooltips).</summary>
public class GanttTaskMouseEventArgs<TItem>
{
    public TItem? Data { get; set; }
    public double ClientX { get; set; }
    public double ClientY { get; set; }
}
