using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>Typed browser clipboard boundary used by Omni components.</summary>
public sealed class ClipboardService
{
    private readonly IOmniCoreJsModule _js;

    internal ClipboardService(IOmniCoreJsModule js) => _js = js;

    /// <summary>Attempts to copy text, returning false when clipboard access is unavailable.</summary>
    public async ValueTask<bool> TryWriteTextAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        try
        {
            await _js.InvokeVoidAsync("copyText", cancellationToken, text);
            return true;
        }
        catch (Exception exception) when (exception is JSException or JSDisconnectedException or
                                           InvalidOperationException or OperationCanceledException)
        {
            return false;
        }
    }
}
