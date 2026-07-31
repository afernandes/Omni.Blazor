using System.Diagnostics;

namespace Omni.Blazor.Utilities;

/// <summary>Observes intentionally detached work so faults never become unobserved.</summary>
internal static class TaskObserver
{
    internal static void Observe(
        Task task,
        Func<Exception, Task>? exceptionHandler = null,
        string? operation = null)
    {
        ArgumentNullException.ThrowIfNull(task);
        if (task.IsCompletedSuccessfully) return;
        _ = ObserveCoreAsync(task, exceptionHandler, operation);
    }

    internal static void Observe(
        ValueTask task,
        Func<Exception, Task>? exceptionHandler = null,
        string? operation = null)
    {
        if (task.IsCompletedSuccessfully) return;
        Observe(task.AsTask(), exceptionHandler, operation);
    }

    private static async Task ObserveCoreAsync(
        Task task,
        Func<Exception, Task>? exceptionHandler,
        string? operation)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (exceptionHandler is not null)
            {
                try
                {
                    await exceptionHandler(exception).ConfigureAwait(false);
                    return;
                }
                catch (Exception handlerException)
                {
                    Trace.TraceError(
                        "Detached operation '{0}' and its exception handler failed: {1}",
                        operation ?? "unknown",
                        new AggregateException(exception, handlerException));
                    return;
                }
            }

            Trace.TraceError(
                "Detached operation '{0}' failed: {1}",
                operation ?? "unknown",
                exception);
        }
    }
}
