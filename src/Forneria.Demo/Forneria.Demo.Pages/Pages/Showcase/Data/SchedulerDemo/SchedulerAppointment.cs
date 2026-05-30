namespace Forneria.Demo.Pages.Pages.Showcase.Data.SchedulerDemo;

/// <summary>
/// Mutable appointment model used by the Scheduler showcase (mirrors Radzen's
/// demo <c>Appointment</c>). Mutable so the Edit dialog and drag-and-drop can
/// update an existing instance in place.
/// </summary>
public class SchedulerAppointment
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public string Text { get; set; } = "";
}
