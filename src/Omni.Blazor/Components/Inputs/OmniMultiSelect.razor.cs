using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Omni.Blazor.Models;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Multiple selection with chips and bounded local or cancellable server-side options.</summary>
public partial class OmniMultiSelect<TValue>
{
    private readonly ItemsProviderCoordinator<TValue> _providerCoordinator = new();
    private readonly ParameterState<Expression<Func<IEnumerable<TValue>?>>?> _valuesExpressionState;
    private readonly List<TValue> _availableItems = [];
    private FieldIdentifier _fieldIdentifier;
    private bool _open;
    private bool _loading;
    private string? _searchText;
    private int _providerTotalCount;
    private long _providerVersion;
    private int _disposeState;
    private Exception? _providerError;

    /// <summary>Initializes form-expression tracking.</summary>
    public OmniMultiSelect()
    {
        _valuesExpressionState = RegisterParameter<Expression<Func<IEnumerable<TValue>?>>?>(nameof(ValuesExpression))
            .WithParameter(() => ValuesExpression)
            .WithChangeHandler(RecomputeFieldIdentifier)
            .Attach();
    }

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

    /// <summary>Selected values.</summary>
    [Parameter] public IEnumerable<TValue>? Values { get; set; }

    /// <summary>Raised when <see cref="Values"/> changes.</summary>
    [Parameter] public EventCallback<IEnumerable<TValue>> ValuesChanged { get; set; }

    /// <summary>Expression used for EditContext integration.</summary>
    [Parameter] public Expression<Func<IEnumerable<TValue>?>>? ValuesExpression { get; set; }

    /// <summary>Logical form-component name.</summary>
    [Parameter] public string? Name { get; set; }

    [CascadingParameter] protected IOmniFormRegistry? FormRegistry { get; set; }

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

    /// <summary>Whether the component is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

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

    internal int RecomputeCount { get; private set; }

    string IOmniFormComponent.ResolvedName => Name ?? _fieldIdentifier.FieldName ?? string.Empty;
    FieldIdentifier IOmniFormComponent.FieldIdentifier => _fieldIdentifier;
    object? IOmniFormComponent.GetValue() => Values;
    bool IOmniFormComponent.HasValue => Values?.Any() == true;

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

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(((IOmniFormComponent)this).ResolvedName))
            FormRegistry?.RegisterComponent(this);
    }

    private void RecomputeFieldIdentifier()
    {
        if (ValuesExpression is not null)
            _fieldIdentifier = FieldIdentifier.Create(ValuesExpression);
        RecomputeCount++;
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

    private async Task OnListChangedAsync(IEnumerable<TValue> next)
    {
        Values = next;
        if (ValuesChanged.HasDelegate) await ValuesChanged.InvokeAsync(next);
    }

    private async Task RemoveAsync(TValue value)
    {
        var next = Values?.Where(item => !EqualityComparer<TValue>.Default.Equals(item, value)).ToArray()
            ?? [];
        Values = next;
        if (ValuesChanged.HasDelegate) await ValuesChanged.InvokeAsync(next);
    }

    private async Task ClearAllAsync()
    {
        TValue[] empty = [];
        Values = empty;
        if (ValuesChanged.HasDelegate) await ValuesChanged.InvokeAsync(empty);
    }

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
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        _providerCoordinator.Dispose();
        Interlocked.Increment(ref _providerVersion);
        FormRegistry?.UnregisterComponent(this);
        GC.SuppressFinalize(this);
    }
}
