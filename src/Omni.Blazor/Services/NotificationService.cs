using Omni.Blazor.Models;

namespace Omni.Blazor.Services;

public class NotificationService
{
    private readonly List<NotificationMessage> _messages = new();

    public IReadOnlyList<NotificationMessage> Messages => _messages;
    public NotificationPosition Position { get; set; } = NotificationPosition.TopRight;

    public event Action? OnChange;

    public void Notify(NotificationMessage message)
    {
        _messages.Add(message);
        OnChange?.Invoke();

        if (message.Duration > 0)
        {
            _ = Task.Delay(TimeSpan.FromMilliseconds(message.Duration))
                .ContinueWith(_ => Remove(message), TaskScheduler.Default);
        }
    }

    public void Notify(NotificationSeverity severity, string? summary, string? detail = null, double duration = 4000)
        => Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary,
            Detail = detail,
            Duration = duration
        });

    public void Info(string summary, string? detail = null, double duration = 4000)
        => Notify(NotificationSeverity.Info, summary, detail, duration);
    public void Success(string summary, string? detail = null, double duration = 4000)
        => Notify(NotificationSeverity.Success, summary, detail, duration);
    public void Warning(string summary, string? detail = null, double duration = 4500)
        => Notify(NotificationSeverity.Warning, summary, detail, duration);
    public void Error(string summary, string? detail = null, double duration = 6000)
        => Notify(NotificationSeverity.Error, summary, detail, duration);

    public void Remove(NotificationMessage message)
    {
        if (_messages.Remove(message))
        {
            message.OnClose?.Invoke(message);
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        _messages.Clear();
        OnChange?.Invoke();
    }
}
