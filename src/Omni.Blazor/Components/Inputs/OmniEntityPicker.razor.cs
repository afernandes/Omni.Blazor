using Microsoft.AspNetCore.Components;
using Omni.Blazor.Models;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>
/// Selects a local or server-side entity through <see cref="OmniDataGrid{TItem}"/>
/// while binding only its stable key to the surrounding EditContext.
/// </summary>
public partial class OmniEntityPicker<TItem, TKey>
    where TItem : class
    where TKey : notnull
{
    private readonly object _resolveSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Func<TItem, object?> _gridKeySelector;
    private CancellationTokenSource? _resolveOperation;
    private TItem? _selectedItem;
    private TItem? _lastSelectedItemParameter;
    private TKey? _lastValue;
    private Exception? _resolveError;
    private bool _hasSynchronizedValue;
    private bool _resolving;
    private bool _open;
    private long _resolveVersion;
    private int _pickerDisposeState;

    /// <summary>Creates stable delegates once per picker instance.</summary>
    public OmniEntityPicker() => _gridKeySelector = item => KeySelector(item);

    /// <summary>Local entity source. Supply either Items or DataProvider.</summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Cancellable server-side DataGrid source. Supply either DataProvider or Items.</summary>
    [Parameter]
    public GridDataProvider<TItem>? DataProvider { get; set; }

    /// <summary>Stable key selector stored by the form-bound value.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, TKey> KeySelector { get; set; } = default!;

    /// <summary>Human-readable selected entity text.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string?> TextSelector { get; set; } = default!;

    /// <summary>Optional externally supplied entity corresponding to Value.</summary>
    [Parameter]
    public TItem? SelectedItem { get; set; }

    /// <summary>Raised after a user selects or clears an entity.</summary>
    [Parameter]
    public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Resolves an externally supplied key not present in local Items.</summary>
    [Parameter]
    public EntityPickerResolver<TItem, TKey>? ResolveItem { get; set; }

    /// <summary>Raised after an observed entity-resolution failure.</summary>
    [Parameter]
    public EventCallback<Exception> ResolveFailed { get; set; }

    /// <summary>Determines whether a key represents no selection. Defaults to default(TKey).</summary>
    [Parameter]
    public Func<TKey?, bool>? EmptyKey { get; set; }

    /// <summary>Custom typed DataGrid columns. A text column is generated when omitted.</summary>
    [Parameter]
    public RenderFragment? Columns { get; set; }

    /// <summary>Optional reusable typed schema for the embedded entity DataGrid.</summary>
    [Parameter]
    public DataGridSchema<TItem>? GridSchema { get; set; }

    /// <summary>Custom DataGrid loading content.</summary>
    [Parameter]
    public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Text shown when no entity is selected.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    /// <summary>Selection dialog title.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>Generated default column title.</summary>
    [Parameter]
    public string? ColumnTitle { get; set; }

    /// <summary>DataGrid search placeholder.</summary>
    [Parameter]
    public string? SearchPlaceholder { get; set; }

    /// <summary>DataGrid empty-state text.</summary>
    [Parameter]
    public string? EmptyText { get; set; }

    /// <summary>Overrides the clear action text.</summary>
    [Parameter]
    public string? ClearText { get; set; }

    /// <summary>Overrides the close action text.</summary>
    [Parameter]
    public string? CloseText { get; set; }

    /// <summary>Whether the selected value can be cleared. Default true.</summary>
    [Parameter]
    public bool AllowClear { get; set; } = true;

    /// <summary>Whether the entity grid exposes search. Default true.</summary>
    [Parameter]
    public bool AllowSearch { get; set; } = true;

    /// <summary>Whether the entity grid exposes paging. Default true.</summary>
    [Parameter]
    public bool AllowPaging { get; set; } = true;

    /// <summary>Entity grid page size. Default ten.</summary>
    [Parameter]
    public int PageSize { get; set; } = 10;

    /// <summary>Dialog or drawer presentation.</summary>
    [Parameter]
    public EntityPickerPresentation Presentation { get; set; }

    /// <summary>Picker panel width.</summary>
    [Parameter]
    public string Width { get; set; } = "760px";

    /// <summary>Currently resolved selected entity.</summary>
    public TItem? CurrentItem => _selectedItem;

    /// <summary>Opens the selection surface.</summary>
    public Task OpenAsync()
    {
        if (!Disabled && !ReadOnly) _open = true;
        return Task.CompletedTask;
    }

    /// <summary>Closes the selection surface.</summary>
    public Task CloseAsync()
    {
        _open = false;
        return Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        ArgumentNullException.ThrowIfNull(KeySelector);
        ArgumentNullException.ThrowIfNull(TextSelector);
        if ((Items is null) == (DataProvider is null))
            throw new InvalidOperationException("OmniEntityPicker requires exactly one source: Items or DataProvider.");
        if (PageSize < 1) throw new ArgumentOutOfRangeException(nameof(PageSize));
        SynchronizeSelectedItem();
    }

    private void SynchronizeSelectedItem()
    {
        bool sameValue = _hasSynchronizedValue
                         && EqualityComparer<TKey?>.Default.Equals(_lastValue, Value);
        if (sameValue && ReferenceEquals(_lastSelectedItemParameter, SelectedItem)) return;
        _hasSynchronizedValue = true;
        _lastValue = Value;
        _lastSelectedItemParameter = SelectedItem;
        _resolveError = null;

        if (IsEmpty(Value))
        {
            CancelResolveOperation();
            _selectedItem = null;
            return;
        }
        if (SelectedItem is not null && KeysEqual(KeySelector(SelectedItem), Value!))
        {
            CancelResolveOperation();
            _selectedItem = SelectedItem;
            return;
        }
        if (TryFindLocal(Value!, out TItem? local))
        {
            CancelResolveOperation();
            _selectedItem = local;
            return;
        }
        if (ResolveItem is null)
        {
            CancelResolveOperation();
            _selectedItem = null;
            return;
        }
        StartResolve(Value!);
    }

    private bool TryFindLocal(TKey key, out TItem? item)
    {
        if (Items is not null)
        {
            foreach (TItem candidate in Items)
            {
                if (!KeysEqual(KeySelector(candidate), key)) continue;
                item = candidate;
                return true;
            }
        }
        item = null;
        return false;
    }

    private void StartResolve(TKey key)
    {
        CancellationTokenSource operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        CancellationTokenSource? previous;
        long version;
        lock (_resolveSync)
        {
            previous = _resolveOperation;
            _resolveOperation = operation;
            version = ++_resolveVersion;
            _resolving = true;
        }
        CancelAndDispose(previous);
        ObserveTask(ResolveSelectedItemAsync(key, operation, version), "OmniEntityPicker.ResolveItem");
    }

    private async Task ResolveSelectedItemAsync(
        TKey key,
        CancellationTokenSource operation,
        long version)
    {
        try
        {
            TItem? item = await ResolveItem!(key, operation.Token);
            if (!IsCurrentResolve(operation, version)) return;
            if (item is not null && !KeysEqual(KeySelector(item), key))
                throw new InvalidOperationException("OmniEntityPicker resolver returned an item with a different key.");
            _selectedItem = item;
            _resolveError = null;
        }
        catch (OperationCanceledException) when (operation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!IsCurrentResolve(operation, version)) return;
            _resolveError = exception;
            _selectedItem = null;
            if (ResolveFailed.HasDelegate) await ResolveFailed.InvokeAsync(exception);
        }
        finally
        {
            bool render = false;
            lock (_resolveSync)
            {
                if (ReferenceEquals(_resolveOperation, operation))
                {
                    _resolveOperation = null;
                    _resolving = false;
                    render = true;
                }
            }
            operation.Dispose();
            if (render && Volatile.Read(ref _pickerDisposeState) == 0)
                await InvokeAsync(StateHasChanged);
        }
    }

    private bool IsCurrentResolve(CancellationTokenSource operation, long version)
    {
        lock (_resolveSync)
            return ReferenceEquals(_resolveOperation, operation) && _resolveVersion == version;
    }

    private async Task SelectAsync(TItem item)
    {
        if (Disabled || ReadOnly) return;
        TKey key = KeySelector(item);
        _selectedItem = item;
        _lastValue = key;
        _lastSelectedItemParameter = SelectedItem;
        _hasSynchronizedValue = true;
        _resolveError = null;
        CancelResolveOperation();
        await SetValueAsync(key);
        if (SelectedItemChanged.HasDelegate) await SelectedItemChanged.InvokeAsync(item);
        _open = false;
    }

    private async Task ClearAsync()
    {
        if (Disabled || ReadOnly || !AllowClear) return;
        _selectedItem = null;
        _lastValue = default;
        _lastSelectedItemParameter = null;
        _hasSynchronizedValue = true;
        _resolveError = null;
        CancelResolveOperation();
        await SetValueAsync(default);
        if (SelectedItemChanged.HasDelegate) await SelectedItemChanged.InvokeAsync(null);
    }

    private bool IsEmpty(TKey? key)
        => EmptyKey?.Invoke(key) ?? EqualityComparer<TKey?>.Default.Equals(key, default);

    private static bool KeysEqual(TKey left, TKey right)
        => EqualityComparer<TKey>.Default.Equals(left, right);

    private bool HasSelection => _selectedItem is not null;
    private string DisplayText => _selectedItem is null
        ? Placeholder ?? Texts.EntityPickerPlaceholder
        : TextSelector(_selectedItem) ?? string.Empty;
    private string DisplayCss => CssBuilder.Default("omni-entity-picker-text")
        .AddClass("omni-entity-picker-placeholder", _selectedItem is null)
        .Build();
    private string RootCss => CssBuilder.Default("omni-entity-picker")
        .AddClass("omni-invalid", IsInvalid)
        .AddClass("omni-disabled", Disabled)
        .AddClass(Class)
        .Build();
    private string PanelCss => CssBuilder.Default("omni-entity-picker-panel")
        .AddClass("omni-entity-picker-drawer", Presentation == EntityPickerPresentation.Drawer)
        .Build();
    private string PanelStyle => Presentation == EntityPickerPresentation.Drawer
        ? $"width:min({Width}, 100vw)"
        : $"width:min({Width}, calc(100vw - 24px))";
    private string TitleId => $"{Id}-title";
    private Func<TItem, object?> GridKeySelector => _gridKeySelector;

    private void CancelResolveOperation()
    {
        CancellationTokenSource? operation;
        lock (_resolveSync)
        {
            operation = _resolveOperation;
            _resolveOperation = null;
            ++_resolveVersion;
            _resolving = false;
        }
        CancelAndDispose(operation);
    }

    private static void CancelAndDispose(CancellationTokenSource? operation)
    {
        if (operation is null) return;
        try { operation.Cancel(); }
        catch (ObjectDisposedException) { }
        operation.Dispose();
    }

    /// <summary>Cancels entity resolution and releases form subscriptions.</summary>
    public override void Dispose()
    {
        if (Interlocked.Exchange(ref _pickerDisposeState, 1) != 0) return;
        _lifetime.Cancel();
        CancelResolveOperation();
        _lifetime.Dispose();
        base.Dispose();
    }
}
