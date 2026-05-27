using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.Models;

public class NotificationMessage
{
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
    public string? Summary { get; set; }
    public string? Detail { get; set; }
    public RenderFragment? DetailContent { get; set; }
    public double Duration { get; set; } = 4000;
    public bool ShowProgress { get; set; } = true;
    public bool CloseOnClick { get; set; } = true;
    public Action<NotificationMessage>? OnClick { get; set; }
    public Action<NotificationMessage>? OnClose { get; set; }
    public object? Payload { get; set; }
    internal Guid Id { get; } = Guid.NewGuid();
}
