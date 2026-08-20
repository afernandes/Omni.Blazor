using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Multiple selection with chips and bounded local or cancellable server-side options.</summary>
public partial class OmniMultiSelect<TValue>
{
    private readonly ItemsProviderCoordinator<TValue> _providerCoordinator = new();
    private readonly List<TValue> _availableItems = [];
    private bool _open;
    private bool _loading;
    private string? _searchText;
    private int _providerTotalCount;
    private long _providerVersion;
    private int _disposeState;
    private Exception? _providerError;

    /// <summary>Synchronous options. Mutually exclusive with <see cref="ItemsProvider"/>.</summary>
    [Parameter] public IEnumerable<TValue>? Items { get; set; }

    /// <summary>Asynchronous, cancellable and paged option source.</summary>
    [Parameter] public OmniItemsProvider<TValue>? ItemsProvider { get; set; }

    /// <summary>Maximum options requested per provider page.</summary>
    [Parameter] public int ProviderPageSize { get; set; } = 50;

    /// <summary>Hard limit for options retained from one provider search.</summary>
    [Parameter] public int MaxProviderItems { get; set; } = 500;

    /// <summary>Debounce applied to provider-backed searches.</summary>
    [Parameter] public int DebounceMs { get; set; } = 220;

    /// <summary>Raised when an uncancelled provider request fails.</summary>
    [Parameter] public EventCallback<Exception> ItemsProviderFailed { get; set; }

    // Value / ValueChanged / ValueExpression (the selected values), Name, Disabled,
    // Required, Validation and the EditContext wiring all come from
    // FormComponent<IEnumerable<TValue>> — the shared multi-value contract.

    /// <summary>Extracts the bound value from an option.</summary>
    [Parameter] public Func<TValue?, TValue?>? ValueSelector { get; set; }

    /// <summary>Maps an option to primary text.</summary>
    [Parameter] public Func<TValue?, string>? TextSelector { get; set; }

    /// <summary>Maps an option to secondary text.</summary>
    [Parameter] public Func<TValue?, string?>? DescriptionSelector { get; set; }

    /// <summary>Maps an option to an icon name.</summary>
    [Parameter] public Func<TValue?, string?>? IconSelector { get; set; }

    /// <summary>Determines whether an option is disabled.</summary>
    [Parameter] public Func<TValue?, bool>? DisabledSelector { get; set; }

    /// <summary>Text displayed when nothing is selected.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Whether to display a search input.</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>Search input placeholder.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>Whether to display the clear-all action.</summary>
    [Parameter] public bool ShowClearAll { get; set; } = true;

    /// <summary>Popover width.</summary>
    [Parameter] public string? PopoverWidth { get; set; }

    /// <summary>Maximum list height.</summary>
    [Parameter] public string PopoverMaxHeight { get; set; } = "280px";

    /// <summary>Text shown while options are loading.</summary>
    [Parameter] public string? LoadingText { get; set; }

    /// <summary>Text shown when the provider fails.</summary>
    [Parameter] public string? LoadErrorText { get; set; }

    /// <summary>Provider retry action text.</summary>
    [Parameter] public string? RetryText { get; set; }

    /// <summary>Action text for requesting another provider page.</summary>
    [Parameter] public string? LoadMoreText { get; set; }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;
    private bool CanLoadMore => ItemsProvider is not null
        && _availableItems.Count < Math.Min(_providerTotalCount, Math.Max(1, MaxProviderItems));
    private string EffectiveLoadingText => LoadingText ?? Texts.Loading;
    private string EffectiveLoadErrorText => LoadErrorText ?? Texts.LoadOptionsError;
    private string EffectiveRetryText => RetryText ?? Texts.Retry;
    private string EffectiveLoadMoreText => LoadMoreText ?? Texts.LoadMore;

    private string RootCss => CssBuilder.Default("omni-multiselect")
        .AddClass("omni-multiselect-disabled", Disabled)
        .AddClass(Class)
        .Build();

    private string TriggerCss => CssBuilder.Default("omni-multiselect-trigger")
        .AddClass("omni-multiselect-trigger-open", _open)
        .AddClass("omni-multiselect-trigger-disabled", Disabled)
        .Build();

    private IEnumerable<TValue>? FilteredItems
    {
        get
        {
            if (ItemsProvider is not null) return _availableItems;
            if (Items is null) return null;
            if (string.IsNullOrWhiteSpace(_searchText)) return Items;
            var search = _searchText.Trim();
            return Items.Where(item => ItemText(item)
                .Contains(search, StringComparison.OrdinalIgnoreCase));
        }
    }

    private string ItemText(TValue? value) =>
        (TextSelector is not null ? TextSelector(value) : value?.ToString()) ?? string.Empty;

    private string ItemTextFor(TValue? value)
    {
        var source = ItemsProvider is null ? Items : _availableItems;
        if (source is not null)
        {
            foreach (var item in source)
            {
                var selectedValue = ValueSelector is not null ? ValueSelector(item) : item;
                if (EqualityComparer<TValue>.Default.Equals(selectedValue, value))
                    return ItemText(item);
            }
        }
        return ItemText(value);
    }

    private async Task OnOpenChangedAsync(bool open)
    {
        _open = open;
        if (!open)
        {
            CancelProviderRequest();
            return;
        }

        if (ItemsProvider is not null)
            await LoadProviderPageAsync(append: false, applyDebounce: false);
    }

    private async Task OnSearchChangedAsync(string? search)
    {
        _searchText = search;
        if (ItemsProvider is not null && _open)
            await LoadProviderPageAsync(append: false, applyDebounce: true);
    }

    private Task OnListChangedAsync(IEnumerable<TValue>? next) => SetValueAsync(next ?? []);

    private Task RemoveAsync(TValue value)
    {
        var next = Value?.Where(item => !EqualityComparer<TValue>.Default.Equals(item, value)).ToArray()
            ?? [];
        return SetValueAsync(next);
    }

    private Task ClearAllAsync() => SetValueAsync([]);

    private Task LoadMoreAsync() => LoadProviderPageAsync(append: true, applyDebounce: false);

    private Task RetryProviderAsync() => LoadProviderPageAsync(append: false, applyDebounce: false);

    private async Task LoadProviderPageAsync(bool append, bool applyDebounce)
    {
        var provider = ItemsProvider;
        if (provider is null || IsDisposed) return;
        var maxItems = Math.Clamp(MaxProviderItems, 1, 10_000);
        var skip = append ? _availableItems.Count : 0;
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
            if (!append) _availableItems.Clear();
            _availableItems.AddRange(page.Items);
            _providerTotalCount = Math.Min(maxItems, page.TotalCount);
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

    /// <inheritdoc />
    public override void Dispose()
    {
        // Guarded so a second Dispose doesn't re-cancel the coordinator; the base
        // handles form-registry removal, EditContext detach and validation cancellation.
        if (Interlocked.Exchange(ref _disposeState, 1) == 0)
        {
            _providerCoordinator.Dispose();
            Interlocked.Increment(ref _providerVersion);
        }

        base.Dispose();
    }
}
