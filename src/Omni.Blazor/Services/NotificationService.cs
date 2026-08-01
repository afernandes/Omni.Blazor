using Microsoft.Extensions.Logging;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Services;

/// <summary>
/// Stores toast notifications for the current scope and coordinates their expiration.
/// </summary>
public class NotificationService : IDisposable
{
    private readonly object _sync = new();
    private readonly List<NotificationMessage> _messages = new();
    private readonly List<ExpirationRegistration> _expirations = new();
    private readonly ILogger<NotificationService>? _logger;
    private NotificationMessage[] _snapshot = [];
    private int _position = (int)NotificationPosition.TopRight;
    private int _disposeState;

    /// <summary>
    /// Initializes a new notification service.
    /// </summary>
    public NotificationService()
    {
    }

    /// <summary>
    /// Initializes a new notification service with background-error logging.
    /// </summary>
    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a stable snapshot of the active notifications.
    /// </summary>
    public IReadOnlyList<NotificationMessage> Messages => Volatile.Read(ref _snapshot);

    /// <summary>
    /// Gets or sets where notifications are rendered.
    /// </summary>
    public NotificationPosition Position
    {
        get => (NotificationPosition)Volatile.Read(ref _position);
        set => Volatile.Write(ref _position, (int)value);
    }

    /// <summary>
    /// Raised after the observable notification state changes.
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Adds a notification and schedules its expiration when a positive duration is configured.
    /// </summary>
    public void Notify(NotificationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        CancellationTokenSource? expirationSource = null;
        if (message.Duration > 0)
        {
            expirationSource = new CancellationTokenSource();
        }

        try
        {
            lock (_sync)
            {
                ThrowIfDisposed();
                _messages.Add(message);

                if (expirationSource is not null)
                {
                    _expirations.Add(new ExpirationRegistration(message, expirationSource));
                }

                PublishSnapshot();
            }
        }
        catch
        {
            expirationSource?.Dispose();
            throw;
        }

        if (expirationSource is not null)
        {
            TaskObserver.Observe(
                ExpireAsync(message, expirationSource),
                operation: "NotificationService.Expire");
        }

        OnChange?.Invoke();
    }

    /// <summary>
    /// Adds a notification from its common values.
    /// </summary>
    public void Notify(
        NotificationSeverity severity,
        string? summary,
        string? detail = null,
        double duration = 4000)
        => Notify(new NotificationMessage
        {
            Severity = severity,
            Summary = summary,
            Detail = detail,
            Duration = duration
        });

    /// <summary>
    /// Adds an informational notification.
    /// </summary>
    public void Info(string summary, string? detail = null, double duration = 4000)
        => Notify(NotificationSeverity.Info, summary, detail, duration);

    /// <summary>
    /// Adds a success notification.
    /// </summary>
    public void Success(string summary, string? detail = null, double duration = 4000)
        => Notify(NotificationSeverity.Success, summary, detail, duration);

    /// <summary>
    /// Adds a warning notification.
    /// </summary>
    public void Warning(string summary, string? detail = null, double duration = 4500)
        => Notify(NotificationSeverity.Warning, summary, detail, duration);

    /// <summary>
    /// Adds an error notification.
    /// </summary>
    public void Error(string summary, string? detail = null, double duration = 6000)
        => Notify(NotificationSeverity.Error, summary, detail, duration);

    /// <summary>
    /// Removes one occurrence of a notification.
    /// </summary>
    public void Remove(NotificationMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        CancellationTokenSource? expirationSource;
        lock (_sync)
        {
            ThrowIfDisposed();
            if (!RemoveMessage(message))
            {
                return;
            }

            expirationSource = RemoveExpiration(message);
            PublishSnapshot();
        }

        CancelSafely(expirationSource);

        try
        {
            message.OnClose?.Invoke(message);
        }
        finally
        {
            OnChange?.Invoke();
        }
    }

    /// <summary>
    /// Removes all notifications and cancels their pending expirations.
    /// </summary>
    public void Clear()
    {
        CancellationTokenSource[] expirationSources;
        lock (_sync)
        {
            ThrowIfDisposed();
            _messages.Clear();
            expirationSources = DrainExpirations();
            PublishSnapshot();
        }

        Cancel(expirationSources);
        OnChange?.Invoke();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            return;
        }

        CancellationTokenSource[] expirationSources;
        lock (_sync)
        {
            _messages.Clear();
            expirationSources = DrainExpirations();
            PublishSnapshot();
            OnChange = null;
        }

        Cancel(expirationSources);
        GC.SuppressFinalize(this);
    }

    private async Task ExpireAsync(
        NotificationMessage message,
        CancellationTokenSource expirationSource)
    {
        try
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(message.Duration),
                expirationSource.Token).ConfigureAwait(false);

            var removed = false;
            lock (_sync)
            {
                if (_disposeState == 0
                    && RemoveExpiration(expirationSource)
                    && RemoveMessage(message))
                {
                    PublishSnapshot();
                    removed = true;
                }
            }

            if (removed)
            {
                try
                {
                    message.OnClose?.Invoke(message);
                }
                finally
                {
                    OnChange?.Invoke();
                }
            }
        }
        catch (OperationCanceledException) when (expirationSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "An exception occurred while expiring a notification.");
        }
        finally
        {
            expirationSource.Dispose();
        }
    }

    private bool RemoveMessage(NotificationMessage message)
    {
        for (var index = 0; index < _messages.Count; index++)
        {
            if (ReferenceEquals(_messages[index], message))
            {
                _messages.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    private CancellationTokenSource? RemoveExpiration(NotificationMessage message)
    {
        for (var index = 0; index < _expirations.Count; index++)
        {
            var registration = _expirations[index];
            if (ReferenceEquals(registration.Message, message))
            {
                _expirations.RemoveAt(index);
                return registration.Source;
            }
        }

        return null;
    }

    private bool RemoveExpiration(CancellationTokenSource source)
    {
        for (var index = 0; index < _expirations.Count; index++)
        {
            if (ReferenceEquals(_expirations[index].Source, source))
            {
                _expirations.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    private CancellationTokenSource[] DrainExpirations()
    {
        if (_expirations.Count == 0)
        {
            return [];
        }

        var sources = new CancellationTokenSource[_expirations.Count];
        for (var index = 0; index < _expirations.Count; index++)
        {
            sources[index] = _expirations[index].Source;
        }

        _expirations.Clear();
        return sources;
    }

    private static void Cancel(CancellationTokenSource[] sources)
    {
        foreach (var source in sources)
        {
            CancelSafely(source);
        }
    }

    private static void CancelSafely(CancellationTokenSource? source)
    {
        if (source is null) return;

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The expiration task owns disposal and may have completed concurrently.
        }
    }

    private void PublishSnapshot() => Volatile.Write(ref _snapshot, [.. _messages]);

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposeState != 0, this);

    private sealed record ExpirationRegistration(
        NotificationMessage Message,
        CancellationTokenSource Source);
}
