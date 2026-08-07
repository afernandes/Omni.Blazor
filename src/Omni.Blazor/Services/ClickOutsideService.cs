using Microsoft.JSInterop;

namespace Omni.Blazor.Services;

/// <summary>Typed ownership boundary for one global click-outside registration.</summary>
public sealed class ClickOutsideService
{
    private readonly IOmniOverlayJsModule _js;

    internal ClickOutsideService(IOmniOverlayJsModule js) => _js = js;

    /// <summary>Registers a callback and returns an idempotent handle that unregisters it.</summary>
    public async ValueTask<IAsyncDisposable?> RegisterAsync<TReceiver>(
        string id,
        string selector,
        DotNetObjectReference<TReceiver> receiver,
        string callback,
        CancellationToken cancellationToken = default)
        where TReceiver : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(selector);
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentException.ThrowIfNullOrWhiteSpace(callback);

        try
        {
            await _js.InvokeVoidAsync(
                "registerClickOutside",
                cancellationToken,
                id,
                selector,
                receiver,
                callback);
            return new Handle(_js, id);
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("prerender", StringComparison.OrdinalIgnoreCase)
            || exception.Message.Contains("statically rendered", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
    }

    private sealed class Handle : IAsyncDisposable
    {
        private IOmniOverlayJsModule? _js;
        private readonly string _id;

        internal Handle(IOmniOverlayJsModule js, string id)
        {
            _js = js;
            _id = id;
        }

        public async ValueTask DisposeAsync()
        {
            var js = Interlocked.Exchange(ref _js, null);
            if (js is null) return;

            try
            {
                await js.InvokeVoidAsync("unregisterClickOutside", _id);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}
