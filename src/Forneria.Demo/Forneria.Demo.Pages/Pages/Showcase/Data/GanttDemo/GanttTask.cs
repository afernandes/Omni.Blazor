using Omni.Blazor.Models;

namespace Forneria.Demo.Pages.Pages.Showcase.Data.GanttDemo;

/// <summary>Demo task model for the OmniGantt showcase (mirrors Radzen's GanttTask).</summary>
public class GanttTask
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string? Name { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public double? Progress { get; set; }
    public DateTime? BaselineStart { get; set; }
    public DateTime? BaselineEnd { get; set; }
}
