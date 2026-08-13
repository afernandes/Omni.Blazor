using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Omni.Blazor.Models;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Search-as-you-type selection with bounded local or cancellable server-side data.</summary>
public partial class OmniAutoComplete<TItem>
{
    private readonly string _id = $"ac-{Guid.NewGuid():N}";
    private readonly ItemsProviderCoordinator<TItem> _providerCoordinator = new();
    private readonly ParameterState<TItem?> _valueState;
    private DotNetObjectReference<OmniAutoComplete<TItem>>? _dotnetReference;
    private IAsyncDisposable? _clickOutsideHandle;
    private bool _open;
    private bool _loading;
    private string _searchText = string.Empty;
    private List<TItem> _filtered = [];
    private int _highlightedIndex;
    private int _providerTotalCount;
    private long _providerVersion;
    private int _disposeState;
    private Exception? _providerError;

    /// <summary>Initializes parameter tracking and the JavaScript callback reference.</summary>
    public OmniAutoComplete()
    {
        _dotnetReference = DotNetObjectReference.Create(this);
        _valueState = RegisterParameter<TItem?>(nameof(Value))
            .WithParameter(() => Value)
            .WithChangeHandler(SyncSearchTextFromValue)
            .Attach();
    }

    /// <summary>Placeholder text shown in the search input.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Visual size of the input.</summary>
    [Parameter] public ComponentSize Size { get; set; } = ComponentSize.Md;

    /// <summary>Whether to show a clear button when a value is selected.</summary>
    [Parameter] public bool Clearable { get; set; } = true;

    /// <summary>Text shown when no option matches.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>Text shown while an asynchronous page is loading.</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Text shown when the asynchronous provider fails.</summary>
    [Parameter] public string? LoadErrorText { get; set; }

    /// <summary>Text of the provider retry action.</summary>
    [Parameter] public string? RetryText { get; set; }

    /// <summary>Text of the action that requests the next provider page.</summary>
    [Parameter] public string? LoadMoreText { get; set; }

    /// <summary>Synchronous option source. Mutually exclusive with <see cref="ItemsProvider"/>.</summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Asynchronous, cancellable and paged option source.</summary>
    [Parameter] public OmniItemsProvider<TItem>? ItemsProvider { get; set; }

    /// <summary>Maximum number of options requested per provider page.</summary>
    [Parameter] public int ProviderPageSize { get; set; } = 50;

    /// <summary>Hard limit for options retained from one provider search.</summary>
    [Parameter] public int MaxProviderItems { get; set; } = 500;

    /// <summary>Raised when an uncancelled provider request fails.</summary>
    [Parameter] public EventCallback<Exception> ItemsProviderFailed { get; set; }

    /// <summary>Required mapping from an option to its display text.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string> TextSelector { get; set; } = item => item?.ToString() ?? string.Empty;

    /// <summary>Optional option template.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Minimum characters required before searching.</summary>
    [Parameter] public int MinSearchLength { get; set; }

    /// <summary>Debounce window applied to asynchronous searches.</summary>
    [Parameter] public int DebounceMs { get; set; } = 220;

    internal int RecomputeCount { get; private set; }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;
    private bool CanLoadMore => ItemsProvider is not null
        && _filtered.Count < Math.Min(_providerTotalCount, Math.Max(1, MaxProviderItems));
    private string EffectivePlaceholder => Placeholder ?? Texts.SearchPlaceholder;
    private string EffectiveEmptyText => EmptyText ?? Texts.NoResults;
    private string EffectiveLoadingText => LoadingText ?? Texts.Searching;
    private string EffectiveLoadErrorText => LoadErrorText ?? Texts.LoadOptionsError;
    private string EffectiveRetryText => RetryText ?? Texts.Retry;
    private string EffectiveLoadMoreText => LoadMoreText ?? Texts.LoadMore;

    private string RootCss => CssBuilder.Default("omni-input-group")
        .AddClass("omni-input-group-right")
        .AddClass("omni-autocomplete")
        .AddClass("omni-input-sm", Size == ComponentSize.Sm)
        .AddClass("omni-input-lg", Size == ComponentSize.Lg)
        .AddClass("omni-autocomplete-open", _open)
        .AddClass("omni-invalid", IsInvalid)
        .AddClass(Class)
        .Build();

    private static string AutoOptionCss(bool active, bool selected) =>
        CssBuilder.Default("omni-autocomplete-option")
            .AddClass("omni-active", active)
            .AddClass("omni-selected", selected)
            .Build();

    private string ResolveText(TItem item) => TextSelector(item);

    private void SyncSearchTextFromValue()
    {
        if (Value is not null && string.IsNullOrEmpty(_searchText))
            _searchText = TextSelector(Value);
        RecomputeCount++;
    }

    private async Task OnInputAsync(ChangeEventArgs args)
    {
        _searchText = args.Value?.ToString() ?? string.Empty;
        _highlightedIndex = 0;
        if (!_open) await OpenAsync();
        else await TriggerSearchAsync();
    }

    private async Task TriggerSearchAsync()
    {
        if (_searchText.Length < Math.Max(0, MinSearchLength))
        {
            CancelProviderRequest();
            _filtered.Clear();
            _providerTotalCount = 0;
            _providerError = null;
            return;
        }

        if (ItemsProvider is not null)
        {
            await LoadProviderPageAsync(append: false, applyDebounce: true);
            return;
        }

        CancelProviderRequest();
        if (Items is null)
        {
            _filtered.Clear();
            return;
        }

        _filtered = string.IsNullOrWhiteSpace(_searchText)
            ? [.. Items]
            : [.. Items.Where(item => TextSelector(item)
                .Contains(_searchText, StringComparison.OrdinalIgnoreCase))];
    }

    private Task LoadMoreAsync() => LoadProviderPageAsync(append: true, applyDebounce: false);

    private Task RetryProviderAsync() => LoadProviderPageAsync(append: false, applyDebounce: false);

    private async Task LoadProviderPageAsync(bool append, bool applyDebounce)
    {
        var provider = ItemsProvider;
        if (provider is null || IsDisposed) return;

        var maxItems = Math.Clamp(MaxProviderItems, 1, 10_000);
        var skip = append ? _filtered.Count : 0;
        if (skip >= maxItems) return;
        var take = Math.Min(Math.Clamp(ProviderPageSize, 1, 1000), maxItems - skip);
        var request = new OmniItemsRequest(
            string.IsNullOrWhiteSpace(_searchText) ? null : _searchText.Trim(),
            skip,
            take);
        var version = Interlocked.Increment(ref _providerVersion);
        _loading = true;
        _providerError = null;
        StateHasChanged();

        try
        {
            var page = await _providerCoordinator.LoadAsync(
                provider,
                request,
                applyDebounce ? TimeSpan.FromMilliseconds(Math.Max(0, DebounceMs)) : TimeSpan.Zero);
            if (page is null || version != Volatile.Read(ref _providerVersion) || IsDisposed) return;

            if (!append) _filtered.Clear();
            _filtered.AddRange(page.Items);
            _providerTotalCount = Math.Min(maxItems, page.TotalCount);
            _highlightedIndex = Math.Clamp(_highlightedIndex, 0, Math.Max(0, _filtered.Count - 1));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (version != Volatile.Read(ref _providerVersion) || IsDisposed) return;
            _providerError = exception;
            if (ItemsProviderFailed.HasDelegate)
                await ItemsProviderFailed.InvokeAsync(exception);
        }
        finally
        {
            if (version == Volatile.Read(ref _providerVersion) && !IsDisposed)
            {
                _loading = false;
                StateHasChanged();
            }
        }
    }

    private async Task SelectAsync(TItem item)
    {
        _searchText = TextSelector(item);
        await SetValueAsync(item);
        await CloseAsync();
    }

    private async Task ClearAsync()
    {
        _searchText = string.Empty;
        _filtered.Clear();
        await SetValueAsync(default);
        await CloseAsync();
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (!_open && args.Key is "ArrowDown" or "Enter")
        {
            await OpenAsync();
            return;
        }
        if (!_open) return;

        switch (args.Key)
        {
            case "ArrowDown":
                _highlightedIndex = Math.Min(_filtered.Count - 1, _highlightedIndex + 1);
                break;
            case "ArrowUp":
                _highlightedIndex = Math.Max(0, _highlightedIndex - 1);
                break;
            case "Home":
                _highlightedIndex = 0;
                break;
            case "End":
                _highlightedIndex = Math.Max(0, _filtered.Count - 1);
                break;
            case "Enter":
                if ((uint)_highlightedIndex < (uint)_filtered.Count)
                    await SelectAsync(_filtered[_highlightedIndex]);
                break;
            case "Escape":
                await CloseAsync();
                break;
        }
    }

    private async Task OpenAsync()
    {
        if (Disabled || ReadOnly || _open || IsDisposed) return;
        _open = true;
        StateHasChanged();

        await ReleaseClickOutsideAsync();
        var receiver = _dotnetReference;
        if (receiver is null) return;
        var handle = await ClickOutside.RegisterAsync(
            _id,
            $"[data-omni-acid=\"{_id}\"]",
            receiver,
            nameof(OnClickOutsideAsync));
        if (!_open || IsDisposed)
        {
            if (handle is not null) await handle.DisposeAsync();
            return;
        }
        _clickOutsideHandle = handle;
        await TriggerSearchAsync();
    }

    [JSInvokable]
    public Task OnClickOutsideAsync() => CloseAsync();

    private async Task CloseAsync()
    {
        if (!_open && _clickOutsideHandle is null) return;
        _open = false;
        CancelProviderRequest();
        await ReleaseClickOutsideAsync();
        if (!IsDisposed) StateHasChanged();
    }

    private void CancelProviderRequest()
    {
        Interlocked.Increment(ref _providerVersion);
        _providerCoordinator.CancelCurrent();
        _loading = false;
    }

    private async ValueTask ReleaseClickOutsideAsync()
    {
        var handle = Interlocked.Exchange(ref _clickOutsideHandle, null);
        if (handle is not null) await handle.DisposeAsync();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        if (!TryDispose(out var handle, out var reference)) return;
        base.Dispose();
        TaskObserver.Observe(
            ReleaseOwnedResourcesAsync(handle, reference),
            operation: "OmniAutoComplete.Dispose");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!TryDispose(out var handle, out var reference)) return;
        base.Dispose();
        await ReleaseOwnedResourcesAsync(handle, reference);
    }

    private bool TryDispose(
        out IAsyncDisposable? handle,
        out DotNetObjectReference<OmniAutoComplete<TItem>>? reference)
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0)
        {
            handle = null;
            reference = null;
            return false;
        }

        _providerCoordinator.Dispose();
        Interlocked.Increment(ref _providerVersion);
        handle = Interlocked.Exchange(ref _clickOutsideHandle, null);
        reference = Interlocked.Exchange(ref _dotnetReference, null);
        return true;
    }

    private static async Task ReleaseOwnedResourcesAsync(
        IAsyncDisposable? handle,
        DotNetObjectReference<OmniAutoComplete<TItem>>? reference)
    {
        try
        {
            if (handle is not null) await handle.DisposeAsync();
        }
        finally
        {
            reference?.Dispose();
        }
    }
}
