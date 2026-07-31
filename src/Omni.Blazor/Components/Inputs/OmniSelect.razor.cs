using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Single-choice select with bounded local or cancellable server-side options.</summary>
public partial class OmniSelect<TValue>
{
    private readonly string _id = $"sel-{Guid.NewGuid():N}";
    private readonly ItemsProviderCoordinator<TValue> _providerCoordinator = new();
    private DotNetObjectReference<OmniSelect<TValue>>? _dotnetReference;
    private IAsyncDisposable? _clickOutsideHandle;
    private List<TValue> _items = [];
    private OmniItemsProvider<TValue>? _lastItemsProvider;
    private long _lastProviderVersion;
    private bool _open;
    private bool _loading;
    private bool _suppressNextTriggerClick;
    private int _highlightedIndex;
    private int _providerTotalCount;
    private string _typeBuffer = string.Empty;
    private DateTime _lastTypeUtc = DateTime.MinValue;
    private Exception? _providerError;
    private long _providerVersion;
    private int _disposeState;

    /// <summary>Initializes the JavaScript callback reference.</summary>
    public OmniSelect() => _dotnetReference = DotNetObjectReference.Create(this);

    /// <summary>Text shown when no value is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Synchronous options. Mutually exclusive with <see cref="ItemsProvider"/>.</summary>
    [Parameter] public IEnumerable<TValue>? Items { get; set; }

    /// <summary>Asynchronous, cancellable and paged option source.</summary>
    [Parameter] public OmniItemsProvider<TValue>? ItemsProvider { get; set; }

    /// <summary>
    /// Explicit provider invalidation version. Changing it cancels the current
    /// request and clears retained provider items.
    /// </summary>
    [Parameter] public long ProviderVersion { get; set; }

    /// <summary>Maximum options requested per provider page.</summary>
    [Parameter] public int ProviderPageSize { get; set; } = 50;

    /// <summary>Hard limit for options retained from the provider.</summary>
    [Parameter] public int MaxProviderItems { get; set; } = 500;

    /// <summary>Raised when an uncancelled provider request fails.</summary>
    [Parameter] public EventCallback<Exception> ItemsProviderFailed { get; set; }

    /// <summary>Extracts the bound value from an option.</summary>
    [Parameter] public Func<TValue?, TValue?>? ValueSelector { get; set; }

    /// <summary>Maps an option to display text.</summary>
    [Parameter] public Func<TValue?, string>? TextSelector { get; set; }

    /// <summary>Visual size of the input.</summary>
    [Parameter] public ComponentSize Size { get; set; } = ComponentSize.Md;

    /// <summary>Whether a selected value can be cleared.</summary>
    [Parameter] public bool Clearable { get; set; }

    /// <summary>Text shown when there are no options.</summary>
    [Parameter] public string EmptyText { get; set; } = "Sem opções";

    /// <summary>Text shown while options are loading.</summary>
    [Parameter] public string LoadingText { get; set; } = "Carregando...";

    /// <summary>Text shown when the provider fails.</summary>
    [Parameter] public string LoadErrorText { get; set; } = "Não foi possível carregar as opções.";

    /// <summary>Provider retry action text.</summary>
    [Parameter] public string RetryText { get; set; } = "Tentar novamente";

    /// <summary>Action text for requesting another provider page.</summary>
    [Parameter] public string LoadMoreText { get; set; } = "Carregar mais";

    /// <summary>Custom content shown while the first provider page is loading.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Custom content shown when the provider has no options.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Custom retry-capable content shown after a provider failure.</summary>
    [Parameter] public RenderFragment<OmniItemsProviderErrorContext>? ErrorTemplate { get; set; }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;
    private bool CanLoadMore => ItemsProvider is not null
        && _items.Count < Math.Min(_providerTotalCount, Math.Max(1, MaxProviderItems));

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_lastItemsProvider, ItemsProvider)
            || _lastProviderVersion != ProviderVersion)
        {
            CancelProviderRequest();
            _items.Clear();
            _providerTotalCount = 0;
            _providerError = null;
            _lastItemsProvider = ItemsProvider;
            _lastProviderVersion = ProviderVersion;
        }

        if (ItemsProvider is null)
            _items = Items is null ? [] : [.. Items];
    }

    private string RootCss => CssBuilder.Default("omni-input-group")
        .AddClass("omni-input-group-right")
        .AddClass("omni-select")
        .AddClass("omni-input-sm", Size == ComponentSize.Sm)
        .AddClass("omni-input-lg", Size == ComponentSize.Lg)
        .AddClass("omni-select-open", _open)
        .AddClass("omni-invalid", IsInvalid)
        .AddClass(Class)
        .Build();

    private static string OptionCss(bool active, bool selected) =>
        CssBuilder.Default("omni-select-option")
            .AddClass("omni-active", active)
            .AddClass("omni-selected", selected)
            .Build();

    private string TextOf(TValue? item) =>
        (TextSelector is not null ? TextSelector(item) : item?.ToString()) ?? string.Empty;

    private string? SelectedText
    {
        get
        {
            if (Value is null) return null;
            foreach (var item in _items)
            {
                var value = ValueSelector is not null ? ValueSelector(item) : item;
                if (EqualityComparer<TValue>.Default.Equals(value, Value)) return TextOf(item);
            }
            return TextOf(Value);
        }
    }

    private Task ToggleAsync()
    {
        if (_suppressNextTriggerClick)
        {
            _suppressNextTriggerClick = false;
            return Task.CompletedTask;
        }
        return _open ? CloseAsync() : OpenAsync();
    }

    private async Task OpenAsync()
    {
        if (Disabled || ReadOnly || _open || IsDisposed) return;
        _open = true;
        StateHasChanged();

        await RegisterClickOutsideAsync();
        if (ItemsProvider is not null && _open && !IsDisposed)
            await LoadProviderPageAsync(append: false);
    }

    private async Task RegisterClickOutsideAsync()
    {
        await ReleaseClickOutsideAsync();
        var receiver = _dotnetReference;
        if (receiver is null) return;
        var handle = await ClickOutside.RegisterAsync(
            _id,
            $"[data-omni-scid=\"{_id}\"]",
            receiver,
            nameof(OnClickOutsideAsync));
        if (!_open || IsDisposed)
        {
            if (handle is not null) await handle.DisposeAsync();
            return;
        }
        _clickOutsideHandle = handle;
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

    private async Task SelectAsync(TValue? value)
    {
        await SetValueAsync(value);
        await CloseAsync();
    }

    private async Task ClearAsync()
    {
        await SetValueAsync(default);
        await CloseAsync();
    }

    private async Task OnKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled) return;
        if (!_open)
        {
            if (args.Key is "Enter" or " ") _suppressNextTriggerClick = true;
            if (args.Key is "ArrowDown" or "ArrowUp" or "Enter" or " ") await OpenAsync();
            return;
        }

        switch (args.Key)
        {
            case "ArrowDown":
                _highlightedIndex = Math.Min(_items.Count - 1, _highlightedIndex + 1);
                break;
            case "ArrowUp":
                _highlightedIndex = Math.Max(0, _highlightedIndex - 1);
                break;
            case "Home":
                _highlightedIndex = 0;
                break;
            case "End":
                _highlightedIndex = Math.Max(0, _items.Count - 1);
                break;
            case "Enter":
            case " ":
                _suppressNextTriggerClick = true;
                if ((uint)_highlightedIndex < (uint)_items.Count)
                {
                    var item = _items[_highlightedIndex];
                    await SelectAsync(ValueSelector is not null ? ValueSelector(item) : item);
                }
                return;
            case "Escape":
            case "Tab":
                await CloseAsync();
                return;
            default:
                if (args.Key.Length == 1 && !char.IsControl(args.Key[0])) TypeAhead(args.Key);
                else return;
                break;
        }
        StateHasChanged();
    }

    private void TypeAhead(string key)
    {
        var now = DateTime.UtcNow;
        if ((now - _lastTypeUtc).TotalMilliseconds > 700) _typeBuffer = string.Empty;
        _lastTypeUtc = now;
        _typeBuffer += char.ToLowerInvariant(key[0]);
        var match = _items.FindIndex(item => TextOf(item)
            .StartsWith(_typeBuffer, StringComparison.OrdinalIgnoreCase));
        if (match >= 0) _highlightedIndex = match;
    }

    private Task LoadMoreAsync() => LoadProviderPageAsync(append: true);

    private Task RetryProviderAsync() => LoadProviderPageAsync(append: false);

    private async Task LoadProviderPageAsync(bool append)
    {
        var provider = ItemsProvider;
        if (provider is null || IsDisposed) return;
        var maxItems = Math.Clamp(MaxProviderItems, 1, 10_000);
        var skip = append ? _items.Count : 0;
        if (skip >= maxItems) return;
        var take = Math.Min(Math.Clamp(ProviderPageSize, 1, 1000), maxItems - skip);
        var version = Interlocked.Increment(ref _providerVersion);
        _loading = true;
        _providerError = null;
        StateHasChanged();

        try
        {
            var page = await _providerCoordinator.LoadAsync(
                provider,
                new OmniItemsRequest(null, skip, take),
                TimeSpan.Zero);
            if (page is null || version != Volatile.Read(ref _providerVersion) || IsDisposed) return;
            if (!append) _items.Clear();
            _items.AddRange(page.Items);
            _providerTotalCount = Math.Min(maxItems, page.TotalCount);
            _highlightedIndex = Math.Clamp(_highlightedIndex, 0, Math.Max(0, _items.Count - 1));
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
            operation: "OmniSelect.Dispose");
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
        out DotNetObjectReference<OmniSelect<TValue>>? reference)
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
        DotNetObjectReference<OmniSelect<TValue>>? reference)
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
