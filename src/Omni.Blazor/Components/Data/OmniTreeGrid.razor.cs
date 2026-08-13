using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Models;
using Omni.Blazor.State;
using Omni.Blazor.Utilities;

namespace Omni.Blazor.Components;

/// <summary>Accessible hierarchical data table with bounded lazy loading and selection.</summary>
public partial class OmniTreeGrid<TItem>
{
    private readonly HierarchyState<TItem> _hierarchy;
    private readonly List<OmniTreeGridColumn<TItem>> _columns = [];
    private ParameterState<(IEnumerable<TItem>? Items,
        Func<TItem, object>? KeySelector,
        Func<TItem, IEnumerable<TItem>?>? Children,
        Func<TItem, bool>? HasChildren,
        HierarchyChildrenProvider<TItem>? Provider,
        Func<TItem, bool>? InitiallyExpanded)> _sourceState = null!;
    private ParameterState<IReadOnlyCollection<object>?> _expandedState = null!;
    private ParameterState<(int MaxChildren,
        int MaxNodes,
        int MaxItems,
        int MaxRows,
        int MaxDepth,
        int MaxConcurrentLoads)> _limitsState = null!;
    private int _disposeState;
    private bool _expandedStateInitialized;

    /// <summary>Initializes the component and its owned hierarchy state.</summary>
    public OmniTreeGrid()
        => _hierarchy = new(
            RefreshAsync,
            NotifyExpandedAsync,
            NotifyLoadFailedAsync,
            DispatchHierarchyAsync);

    /// <summary>Root items in the hierarchy.</summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>
    /// Stable unique key selector. Keys identify expansion, cache and loading state;
    /// changing an item's key requires <see cref="ReloadAsync()"/>.
    /// </summary>
    [Parameter] public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Synchronous child selector for fully in-memory trees.</summary>
    [Parameter] public Func<TItem, IEnumerable<TItem>?>? Children { get; set; }

    /// <summary>Predicate indicating whether an item can be expanded.</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>Asynchronous, cancellable lazy child source.</summary>
    [Parameter] public HierarchyChildrenProvider<TItem>? ChildrenProvider { get; set; }

    /// <summary>Declarative <c>OmniTreeGridColumn</c> definitions.</summary>
    [Parameter] public RenderFragment? Columns { get; set; }

    /// <summary>Currently selected row item for two-way binding.</summary>
    [Parameter] public TItem? SelectedItem { get; set; }

    /// <summary>Raised when <see cref="SelectedItem"/> changes.</summary>
    [Parameter] public EventCallback<TItem?> SelectedItemChanged { get; set; }

    /// <summary>Raised after a row is selected.</summary>
    [Parameter] public EventCallback<TItem> RowSelected { get; set; }

    /// <summary>Externally controlled expanded key set. Replace the collection when updating it.</summary>
    [Parameter] public IReadOnlyCollection<object>? ExpandedKeys { get; set; }

    /// <summary>Raised with an immutable snapshot after expansion changes.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<object>?> ExpandedKeysChanged { get; set; }

    /// <summary>Optional predicate applied once when a data source is reset.</summary>
    [Parameter] public Func<TItem, bool>? InitiallyExpanded { get; set; }

    /// <summary>Optional row CSS class selector.</summary>
    [Parameter] public Func<TItem, string?>? RowClass { get; set; }

    /// <summary>Optional empty-state content.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Indentation per hierarchy level in CSS pixels.</summary>
    [Parameter] public int IndentSize { get; set; } = 20;

    /// <summary>Maximum children retained from a single lazy-load response.</summary>
    [Parameter] public int MaxChildrenPerNode { get; set; } = 1000;

    /// <summary>Maximum lazy-loaded parent nodes retained in the LRU cache.</summary>
    [Parameter] public int MaxCachedNodes { get; set; } = 500;

    /// <summary>Maximum total lazy-loaded child items retained in the LRU cache.</summary>
    [Parameter] public int MaxCachedItems { get; set; } = 10_000;

    /// <summary>Maximum rows flattened and rendered at once.</summary>
    [Parameter] public int MaxVisibleRows { get; set; } = 5000;

    /// <summary>Maximum traversed hierarchy depth, protecting against cycles and pathological input.</summary>
    [Parameter] public int MaxDepth { get; set; } = 64;

    /// <summary>Maximum number of lazy child requests executed concurrently.</summary>
    [Parameter] public int MaxConcurrentLoads { get; set; } = 4;

    /// <summary>Whether a row double-click toggles its expansion state.</summary>
    [Parameter] public bool ExpandOnRowDoubleClick { get; set; } = true;

    /// <summary>Accessible label for the tree grid.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Empty-state text.</summary>
    [Parameter] public string? EmptyText { get; set; }

    /// <summary>Accessible label for expanding a row.</summary>
    [Parameter] public string? ExpandText { get; set; }

    /// <summary>Accessible label for collapsing a row.</summary>
    [Parameter] public string? CollapseText { get; set; }

    /// <summary>Message shown when lazy child loading fails.</summary>
    [Parameter] public string? LoadErrorText { get; set; }

    /// <summary>Label for retrying a failed lazy load.</summary>
    [Parameter] public string? RetryText { get; set; }

    /// <summary>Template shown when the visible-row limit is reached.</summary>
    [Parameter] public string? LimitReachedText { get; set; }

    private string EffectiveAriaLabel => AriaLabel ?? Texts.HierarchicalTable;
    private string EffectiveEmptyText => EmptyText ?? Texts.NoRecords;
    private string EffectiveExpandText => ExpandText ?? Texts.Expand;
    private string EffectiveCollapseText => CollapseText ?? Texts.Collapse;
    private string EffectiveLoadErrorText => LoadErrorText ?? Texts.HierarchyLoadError;
    private string EffectiveRetryText => RetryText ?? Texts.Retry;
    private string EffectiveLimitReachedText => LimitReachedText ?? Texts.HierarchyLimitReached;

    /// <summary>Raised when an uncancelled lazy-load operation fails.</summary>
    [Parameter] public EventCallback<Exception> LoadFailed { get; set; }

    /// <summary>Number of currently flattened visible rows.</summary>
    public int VisibleRowCount => Rows.Count;

    /// <summary>Whether at least one lazy child request is active.</summary>
    public bool IsLoading => _hierarchy.IsLoading;

    internal int CachedNodeCount => _hierarchy.CachedNodeCount;
    internal int CachedItemCount => _hierarchy.CachedItemCount;
    internal int ErrorCount => _hierarchy.ErrorCount;

    private IReadOnlyList<HierarchyRow<TItem>> Rows => _hierarchy.Rows;
    private int ColumnSpan => Math.Max(1, _columns.Count);
    private bool LimitReached => _hierarchy.LimitReached;
    private OmniTreeGridColumn<TItem>? HierarchyColumn =>
        _columns.FirstOrDefault(column => column.IsHierarchyAnchor) ?? _columns.FirstOrDefault();

    private string RootCss => CssBuilder.Default("omni-tree-grid")
        .AddClass("omni-tree-grid-loading", IsLoading)
        .AddClass(Class)
        .Build();

    protected override void OnInitialized()
    {
        _sourceState = RegisterParameter<(IEnumerable<TItem>?,
            Func<TItem, object>?,
            Func<TItem, IEnumerable<TItem>?>?,
            Func<TItem, bool>?,
            HierarchyChildrenProvider<TItem>?,
            Func<TItem, bool>?)>("TreeSource")
            .WithParameter(() => (Items, KeySelector, Children, HasChildren, ChildrenProvider, InitiallyExpanded))
            .WithChangeHandler(ResetData)
            .Attach();

        _expandedState = RegisterParameter<IReadOnlyCollection<object>?>(nameof(ExpandedKeys))
            .WithParameter(() => ExpandedKeys)
            .WithEventCallback(() => ExpandedKeysChanged)
            .WithChangeHandler(SynchronizeExpandedKeys)
            .Attach();

        _limitsState = RegisterParameter<(int, int, int, int, int, int)>("TreeLimits")
            .WithParameter(() => (
                MaxChildrenPerNode,
                MaxCachedNodes,
                MaxCachedItems,
                MaxVisibleRows,
                MaxDepth,
                MaxConcurrentLoads))
            .WithChangeHandler(ApplyLimitsAsync)
            .Attach();
    }

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (ChildrenProvider is not null && !IsDisposed)
            ObserveNodeLoad(_hierarchy.LoadPendingExpandedAsync());
        return Task.CompletedTask;
    }

    internal void Register(OmniTreeGridColumn<TItem> column)
    {
        if (_columns.Contains(column)) return;
        _columns.Add(column);
        StateHasChanged();
    }

    internal void Unregister(OmniTreeGridColumn<TItem> column)
    {
        if (_columns.Remove(column) && !IsDisposed) StateHasChanged();
    }

    /// <summary>Expands an item and awaits lazy loading when necessary.</summary>
    public Task ExpandAsync(TItem item, CancellationToken cancellationToken = default)
        => _hierarchy.ExpandAsync(item, cancellationToken);

    /// <summary>Collapses an item and cancels pending requests in its visible subtree.</summary>
    public Task CollapseAsync(TItem item) => _hierarchy.CollapseAsync(item);

    /// <summary>Toggles one item's expanded state.</summary>
    public Task ToggleAsync(TItem item, CancellationToken cancellationToken = default)
        => _hierarchy.ToggleAsync(item, cancellationToken);

    /// <summary>Expands all reachable nodes within the configured safety limits.</summary>
    public Task ExpandAllAsync(CancellationToken cancellationToken = default)
        => _hierarchy.ExpandAllAsync(cancellationToken);

    /// <summary>Collapses every expanded node and cancels pending requests.</summary>
    public Task CollapseAllAsync() => _hierarchy.CollapseAllAsync();

    /// <summary>Clears all lazy caches and rebuilds the hierarchy.</summary>
    public Task ReloadAsync() => _hierarchy.ReloadAsync();

    /// <summary>Evicts and reloads one expanded item's lazy children.</summary>
    public Task ReloadAsync(TItem item, CancellationToken cancellationToken = default)
        => _hierarchy.ReloadAsync(item, cancellationToken);

    private void ResetData()
    {
        ConfigureHierarchy();
        _hierarchy.ResetData(Items, ExpandedKeys, InitiallyExpanded);
    }

    private void SynchronizeExpandedKeys()
    {
        if (!_expandedStateInitialized)
        {
            _expandedStateInitialized = true;
            if (ExpandedKeys is null) return;
        }
        _hierarchy.SynchronizeExpandedKeys(ExpandedKeys);
    }

    private async Task ApplyLimitsAsync(ParameterChangedEventArgs<(int, int, int, int, int, int)> _)
    {
        ConfigureHierarchy();
        await _hierarchy.ApplyLimitsAsync();
    }

    private void ConfigureHierarchy()
        => _hierarchy.Configure(
            KeySelector,
            Children,
            HasChildren,
            ChildrenProvider,
            MaxChildrenPerNode,
            MaxCachedNodes,
            MaxCachedItems,
            MaxVisibleRows,
            MaxDepth,
            MaxConcurrentLoads);

    private async Task SelectAsync(TItem item)
    {
        SelectedItem = item;
        if (SelectedItemChanged.HasDelegate) await SelectedItemChanged.InvokeAsync(item);
        if (IsDisposed) return;
        if (RowSelected.HasDelegate) await RowSelected.InvokeAsync(item);
    }

    private async Task RowDoubleClickAsync(HierarchyRow<TItem> row)
    {
        if (ExpandOnRowDoubleClick && row.HasChildren)
            await ToggleAsync(row.Item);
    }

    private async Task RowKeyDownAsync(HierarchyRow<TItem> row, KeyboardEventArgs args)
    {
        switch (args.Key)
        {
            case "ArrowRight" when row.HasChildren && !IsExpanded(row.Key):
                await ExpandAsync(row.Item);
                break;
            case "ArrowLeft" when row.HasChildren && IsExpanded(row.Key):
                await CollapseAsync(row.Item);
                break;
            case "Enter":
            case " ":
                await SelectAsync(row.Item);
                break;
        }
    }

    private Task RetryAsync(TItem item) => _hierarchy.RetryAsync(item);

    private bool IsExpanded(object key) => _hierarchy.IsExpanded(key);
    private bool IsNodeLoading(object key) => _hierarchy.IsNodeLoading(key);
    private bool TryGetError(object key, out string error) => _hierarchy.TryGetError(key, out error);

    private bool IsSelected(TItem item) =>
        SelectedItem is not null && EqualityComparer<TItem>.Default.Equals(SelectedItem, item);

    private int RowTabIndex(HierarchyRow<TItem> row) =>
        IsSelected(row.Item) || (SelectedItem is null && row.Index == 0) ? 0 : -1;

    private string? ExpandedAria(HierarchyRow<TItem> row) =>
        row.HasChildren ? IsExpanded(row.Key) ? "true" : "false" : null;

    private string RowCss(HierarchyRow<TItem> row) => CssBuilder.Default("omni-tree-grid-row")
        .AddClass("omni-tree-grid-row-selected", IsSelected(row.Item))
        .AddClass(RowClass?.Invoke(row.Item))
        .Build();

    private static string? ColumnWidth(OmniTreeGridColumn<TItem> column) =>
        string.IsNullOrWhiteSpace(column.Width) ? null : $"width:{column.Width}";

    private static object ErrorRowKey(object key) => new ErrorKey(key);

    private async Task RefreshAsync()
    {
        if (IsDisposed) return;
        await InvokeAsync(StateHasChanged);
    }

    private async Task NotifyExpandedAsync(IReadOnlyCollection<object> snapshot)
    {
        ExpandedKeys = snapshot;
        if (ExpandedKeysChanged.HasDelegate)
            await ExpandedKeysChanged.InvokeAsync(snapshot);
    }

    private Task NotifyLoadFailedAsync(Exception exception)
        => LoadFailed.HasDelegate ? LoadFailed.InvokeAsync(exception) : Task.CompletedTask;

    private async Task DispatchHierarchyAsync(Func<Task> action)
    {
        if (IsDisposed) return;
        try
        {
            await InvokeAsync(action);
        }
        catch (ObjectDisposedException) when (IsDisposed)
        {
        }
        catch (InvalidOperationException) when (IsDisposed)
        {
        }
    }

    private void ObserveNodeLoad(Task task)
        => ObserveTask(ObserveNodeLoadAsync(task), "OmniTreeGrid.NodeLoad");

    private async Task ObserveNodeLoadAsync(Task task)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (IsDisposed) return;
            try
            {
                await DispatchExceptionAsync(exception);
            }
            catch when (IsDisposed)
            {
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        _hierarchy.Dispose();
        _columns.Clear();
        GC.SuppressFinalize(this);
    }

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private sealed record ErrorKey(object Key);
}
