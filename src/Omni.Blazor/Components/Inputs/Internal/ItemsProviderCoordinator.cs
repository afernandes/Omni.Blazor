using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Coordinates latest-wins option requests. Each operation owns and disposes its
/// linked token source; superseding callers only request cancellation.
/// </summary>
internal sealed class ItemsProviderCoordinator<TItem> : IDisposable
{
    private readonly object _sync = new();
    private readonly CancellationTokenSource _lifetimeSource = new();
    private CancellationTokenSource? _currentSource;
    private long _version;
    private int _disposeState;

    internal async Task<OmniItemsPage<TItem>?> LoadAsync(
        OmniItemsProvider<TItem> provider,
        OmniItemsRequest request,
        TimeSpan delay,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        if (request.Skip < 0) throw new ArgumentOutOfRangeException(nameof(request), "Skip cannot be negative.");
        if (request.Take <= 0) throw new ArgumentOutOfRangeException(nameof(request), "Take must be positive.");

        CancellationTokenSource source;
        CancellationTokenSource? previous;
        long version;
        lock (_sync)
        {
            if (IsDisposed) return null;
            source = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                _lifetimeSource.Token);
            previous = _currentSource;
            _currentSource = source;
            version = ++_version;
        }
        CancelSafely(previous);

        try
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, source.Token).ConfigureAwait(false);

            var page = await provider(request, source.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The items provider returned null.");
            source.Token.ThrowIfCancellationRequested();
            if (!IsCurrent(source, version)) return null;

            var items = page.Items
                ?? throw new InvalidOperationException("The items provider returned a null item page.");
            var count = Math.Min(request.Take, items.Count);
            IReadOnlyList<TItem> bounded = count == items.Count
                ? items
                : Copy(items, count);
            var minimumTotal = request.Skip + bounded.Count;
            return new(bounded, Math.Max(minimumTotal, page.TotalCount));
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_currentSource, source))
                    _currentSource = null;
            }
            source.Dispose();
        }
    }

    private bool IsCurrent(CancellationTokenSource source, long version)
    {
        lock (_sync)
        {
            return !IsDisposed
                && version == _version
                && ReferenceEquals(source, _currentSource)
                && !source.IsCancellationRequested;
        }
    }

    private static TItem[] Copy(IReadOnlyList<TItem> source, int count)
    {
        var result = new TItem[count];
        for (var index = 0; index < count; index++)
            result[index] = source[index];
        return result;
    }

    internal void CancelCurrent()
    {
        CancellationTokenSource? current;
        lock (_sync)
        {
            ++_version;
            current = _currentSource;
            _currentSource = null;
        }
        CancelSafely(current);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;

        CancellationTokenSource? current;
        lock (_sync)
        {
            ++_version;
            current = _currentSource;
            _currentSource = null;
        }
        CancelSafely(current);
        CancelSafely(_lifetimeSource);
        _lifetimeSource.Dispose();
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private static void CancelSafely(CancellationTokenSource? source)
    {
        if (source is null) return;
        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
