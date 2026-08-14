using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Rich data grid with sorting, filtering, grouping, editing, virtualization
/// and optional hierarchical rows.
/// </summary>
public partial class OmniDataGrid<TItem>
{
    private readonly HierarchyState<TItem> _hierarchy;
    private object? _lastHierarchySource;
    private Func<TItem, object>? _lastKeySelector;
    private Func<TItem, IEnumerable<TItem>?>? _lastChildren;
    private Func<TItem, bool>? _lastHasChildren;
    private HierarchyChildrenProvider<TItem>? _lastChildrenProvider;
    private Func<TItem, bool>? _lastInitiallyExpanded;
    private IReadOnlyCollection<object>? _lastExpandedKeys;
    private (int Children, int Nodes, int Items, int Rows, int Depth, int Concurrent) _lastHierarchyLimits;
    private object? _focusedHierarchyKey;
    private bool _hierarchyConfigured;
    private readonly object _exportSync = new();
    private CancellationTokenSource? _exportCts;
    private int _exporting;
    private readonly SemaphoreSlim _viewStatePersistGate = new(1, 1);
    private DataGridViewState? _initialViewState;
    private DataGridViewState? _pendingViewState;
    private DataGridViewState? _lastViewStateParameter;
    private string? _lastPersistKeyParameter;
    private bool _viewStateInitialized;
    private bool _applyingViewState;
    private int _viewStatePersistSequence;

    /// <summary>Initializes the grid and its owned hierarchy state.</summary>
    public OmniDataGrid()
        => _hierarchy = new(
            RefreshHierarchyAsync,
            NotifyHierarchyExpandedAsync,
            NotifyHierarchyLoadFailedAsync,
            DispatchHierarchyAsync);

    /// <summary>
    /// Optional streaming source used only by CSV export. When omitted, the grid
    /// pages through <see cref="DataProvider"/> or exports its in-memory source.
    /// </summary>
    [Parameter] public GridExportProvider<TItem>? ExportProvider { get; set; }

    /// <summary>Hard cap applied to every export, including custom streaming providers.</summary>
    [Parameter] public int MaxExportRows { get; set; } = 10_000;

    /// <summary>Page size used while exporting through <see cref="DataProvider"/>.</summary>
    [Parameter] public int ExportBatchSize { get; set; } = 500;

    /// <summary>Raised when an export reaches <see cref="MaxExportRows"/>.</summary>
    [Parameter] public EventCallback<int> ExportTruncated { get; set; }

    /// <summary>Raised when an uncancelled export fails.</summary>
    [Parameter] public EventCallback<Exception> ExportFailed { get; set; }

    /// <summary>Whether a CSV export is currently being produced.</summary>
    public bool IsExporting => Volatile.Read(ref _exporting) != 0;

    /// <summary>Whether the most recently completed export reached the configured row cap.</summary>
    public bool LastExportWasTruncated { get; private set; }

    /// <summary>Stable unique key selector used by hierarchy expansion, cache and loading state.</summary>
    [Parameter] public Func<TItem, object>? KeySelector { get; set; }

    /// <summary>Synchronous child selector that enables hierarchy mode for in-memory trees.</summary>
    [Parameter] public Func<TItem, IEnumerable<TItem>?>? Children { get; set; }

    /// <summary>Predicate indicating whether a hierarchy item can be expanded.</summary>
    [Parameter] public Func<TItem, bool>? HasChildren { get; set; }

    /// <summary>Asynchronous, cancellable child source that enables lazy hierarchy mode.</summary>
    [Parameter] public HierarchyChildrenProvider<TItem>? ChildrenProvider { get; set; }

    /// <summary>Externally controlled expanded key set. Replace the collection when updating it.</summary>
    [Parameter] public IReadOnlyCollection<object>? ExpandedKeys { get; set; }

    /// <summary>Raised with an immutable snapshot after hierarchy expansion changes.</summary>
    [Parameter] public EventCallback<IReadOnlyCollection<object>?> ExpandedKeysChanged { get; set; }

    /// <summary>Optional predicate applied once when the hierarchy source changes.</summary>
    [Parameter] public Func<TItem, bool>? InitiallyExpanded { get; set; }

    /// <summary>Pixels of indentation per hierarchy level.</summary>
    [Parameter] public int IndentSize { get; set; } = 20;

    /// <summary>Maximum children retained from one lazy-load response.</summary>
    [Parameter] public int MaxChildrenPerNode { get; set; } = 1000;

    /// <summary>Maximum lazy-loaded parent nodes retained in the LRU cache.</summary>
    [Parameter] public int MaxCachedNodes { get; set; } = 500;

    /// <summary>Maximum total lazy-loaded child items retained in the LRU cache.</summary>
    [Parameter] public int MaxCachedItems { get; set; } = 10_000;

    /// <summary>Maximum hierarchy rows flattened before rendering or virtualization.</summary>
    [Parameter] public int MaxVisibleRows { get; set; } = 5000;

    /// <summary>Maximum hierarchy depth traversed, protecting against cycles and pathological input.</summary>
    [Parameter] public int MaxDepth { get; set; } = 64;

    /// <summary>Maximum number of lazy child requests executed concurrently.</summary>
    [Parameter] public int MaxConcurrentLoads { get; set; } = 4;

    /// <summary>Accessible label used when the table is rendered as a tree grid.</summary>
    [Parameter] public string? HierarchyAriaLabel { get; set; }

    /// <summary>Accessible label for expanding a hierarchy row.</summary>
    [Parameter] public string? ExpandText { get; set; }

    /// <summary>Accessible label for collapsing a hierarchy row.</summary>
    [Parameter] public string? CollapseText { get; set; }

    /// <summary>Message displayed when lazy child loading fails.</summary>
    [Parameter] public string? HierarchyLoadErrorText { get; set; }

    /// <summary>Label for retrying a failed lazy hierarchy load.</summary>
    [Parameter] public string? HierarchyRetryText { get; set; }

    /// <summary>Message displayed when the configured hierarchy row limit is reached.</summary>
    [Parameter] public string? HierarchyLimitReachedText { get; set; }

    private string EffectiveHierarchyAriaLabel => HierarchyAriaLabel ?? Texts.HierarchicalTable;
    private string EffectiveExpandText => ExpandText ?? Texts.Expand;
    private string EffectiveCollapseText => CollapseText ?? Texts.Collapse;
    private string EffectiveHierarchyLoadErrorText => HierarchyLoadErrorText ?? Texts.HierarchyLoadError;
    private string EffectiveHierarchyRetryText => HierarchyRetryText ?? Texts.Retry;
    private string EffectiveGroupPanelText => GroupPanelText ?? Texts.GroupPanel;

    /// <summary>Raised when an uncancelled lazy hierarchy load fails.</summary>
    [Parameter] public EventCallback<Exception> HierarchyLoadFailed { get; set; }

    /// <summary>Number of currently flattened hierarchy rows.</summary>
    public int VisibleHierarchyRowCount => _hierarchy.Rows.Count;

    /// <summary>Whether at least one lazy hierarchy request is active.</summary>
    public bool IsHierarchyLoading => _hierarchy.IsLoading;

    private IReadOnlyList<HierarchyRow<TItem>> HierarchyRows => _hierarchy.Rows;
    private ICollection<HierarchyRow<TItem>> HierarchyRowsCollection => _hierarchy.RowsCollection;
    private bool IsHierarchyMode => Children is not null || ChildrenProvider is not null;
    private bool HierarchyLimitReached => _hierarchy.LimitReached;
    private object? HierarchySourceIdentity => DataProvider is null ? Data : DataProvider;

    private OmniDataGridColumn<TItem>? HierarchyAnchorColumn =>
        IsHierarchyMode
            ? VisibleColumns.FirstOrDefault(column => column.IsHierarchyAnchor)
                ?? VisibleColumns.FirstOrDefault()
            : null;

    /// <summary>Expands one hierarchy item and awaits lazy loading when necessary.</summary>
    public Task ExpandAsync(TItem item, CancellationToken cancellationToken = default)
        => _hierarchy.ExpandAsync(item, cancellationToken);

    /// <summary>Collapses one hierarchy item and cancels requests in its visible subtree.</summary>
    public Task CollapseAsync(TItem item) => CollapseHierarchyItemAsync(item);

    /// <summary>Toggles one hierarchy item's expanded state.</summary>
    public Task ToggleAsync(TItem item, CancellationToken cancellationToken = default)
        => ToggleHierarchyItemAsync(item, cancellationToken);

    /// <summary>Expands all reachable hierarchy nodes within the configured safety limits.</summary>
    public Task ExpandAllAsync(CancellationToken cancellationToken = default)
        => _hierarchy.ExpandAllAsync(cancellationToken);

    /// <summary>Collapses every expanded hierarchy node and cancels pending requests.</summary>
    public Task CollapseAllAsync() => _hierarchy.CollapseAllAsync();

    /// <summary>Clears lazy hierarchy caches while preserving expansion state.</summary>
    public Task ReloadHierarchyAsync() => _hierarchy.ReloadAsync();

    /// <summary>Evicts and reloads one expanded hierarchy item's lazy children.</summary>
    public Task ReloadHierarchyAsync(TItem item, CancellationToken cancellationToken = default)
        => _hierarchy.ReloadAsync(item, cancellationToken);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (ChildrenProvider is not null && Volatile.Read(ref _disposeState) == 0)
            ObserveHierarchyTask(_hierarchy.LoadPendingExpandedAsync());
        await InitializeOrApplyViewStateAsync();
    }

    private async Task InitializeOrApplyViewStateAsync()
    {
        if (Volatile.Read(ref _disposeState) != 0 || _columns.Count == 0) return;
        if (ViewState is null
            && _pendingViewState is null
            && string.IsNullOrWhiteSpace(PersistKey)
            && !ViewStateChanged.HasDelegate)
            return;
        _initialViewState ??= CaptureViewState();

        if (_pendingViewState is { } controlled)
        {
            _pendingViewState = null;
            _viewStateInitialized = true;
            await ApplyViewStateCoreAsync(controlled);
            return;
        }
        if (_viewStateInitialized) return;
        _viewStateInitialized = true;
        if (ViewState is not null || string.IsNullOrWhiteSpace(PersistKey)) return;

        string persistKey = PersistKey;
        DataGridViewState? persisted = await StateStorage.LoadAsync(persistKey);
        if (persisted is not null
            && Volatile.Read(ref _disposeState) == 0
            && ViewState is null
            && _pendingViewState is null
            && string.Equals(persistKey, PersistKey, StringComparison.Ordinal))
            await ApplyViewStateCoreAsync(persisted);
    }

    /// <summary>Captures normalized column, sort, filter, grouping and search preferences.</summary>
    public DataGridViewState CaptureViewState()
    {
        _ = BuildColumnMap();
        var columnStates = new DataGridColumnViewState[_columns.Count];
        for (int index = 0; index < _columns.Count; index++)
        {
            OmniDataGridColumn<TItem> column = _columns[index];
            columnStates[index] = new DataGridColumnViewState(
                column.ResolvedPropertyName,
                index,
                column.EffectiveWidth,
                column.VisibleInternal,
                column.EffectiveFrozen);
        }

        SortDescriptor[] sorts = [.. _sorts.Select(sort =>
            new SortDescriptor(sort.Col.ResolvedPropertyName, sort.Dir))];
        DataGridFilterViewState[] filters = [.. _filters.Select(filter =>
            new DataGridFilterViewState(
                filter.Key.ResolvedPropertyName,
                filter.Value.Operator,
                Convert.ToString(filter.Value.Value, System.Globalization.CultureInfo.InvariantCulture),
                Convert.ToString(filter.Value.SecondValue, System.Globalization.CultureInfo.InvariantCulture)))];
        DataGridGroupViewState[] groups = [.. _groupLevels.Select(group =>
            new DataGridGroupViewState(group.Column.ResolvedPropertyName, group.Interval))];
        return new DataGridViewState(columnStates, sorts, filters, groups, _search);
    }

    /// <summary>Applies a view state and raises <see cref="ViewStateChanged"/>.</summary>
    public async Task ApplyViewStateAsync(DataGridViewState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _initialViewState ??= CaptureViewState();
        await ApplyViewStateCoreAsync(state);
        await NotifyViewStateChangedAsync();
    }

    /// <summary>Restores the initial declared layout and optionally clears browser persistence.</summary>
    public async Task ResetViewStateAsync(bool clearPersisted = true)
    {
        if (_columns.Count == 0) return;
        _initialViewState ??= CaptureViewState();
        await ApplyViewStateCoreAsync(_initialViewState);
        if (clearPersisted && !string.IsNullOrWhiteSpace(PersistKey))
            await StateStorage.RemoveAsync(PersistKey);
        if (ViewStateChanged.HasDelegate)
            await ViewStateChanged.InvokeAsync(CaptureViewState());
    }

    private async Task ApplyViewStateCoreAsync(DataGridViewState state)
    {
        if (_applyingViewState || Volatile.Read(ref _disposeState) != 0) return;
        if (state.Version != DataGridViewState.CurrentVersion)
            throw new ArgumentOutOfRangeException(
                nameof(state),
                state.Version,
                $"Unsupported DataGrid view-state version. Expected {DataGridViewState.CurrentVersion}.");
        Dictionary<string, OmniDataGridColumn<TItem>> columns = BuildColumnMap();
        var originalOrder = new Dictionary<OmniDataGridColumn<TItem>, int>(_columns.Count);
        for (int index = 0; index < _columns.Count; index++) originalOrder.Add(_columns[index], index);

        var requestedOrder = new Dictionary<OmniDataGridColumn<TItem>, int>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (DataGridColumnViewState columnState in state.Columns)
        {
            if (!seen.Add(columnState.Property))
                throw new InvalidOperationException($"DataGrid view state contains duplicate column '{columnState.Property}'.");
            if (!columns.TryGetValue(columnState.Property, out OmniDataGridColumn<TItem>? column)) continue;
            column.SetWidth(columnState.Width);
            column.SetFrozen(columnState.Frozen);
            column.VisibleInternal = !column.CanHide || columnState.Visible;
            requestedOrder[column] = columnState.Order;
        }
        _columns.Sort((left, right) =>
        {
            int leftOrder = requestedOrder.TryGetValue(left, out int leftRequested)
                ? leftRequested
                : int.MaxValue;
            int rightOrder = requestedOrder.TryGetValue(right, out int rightRequested)
                ? rightRequested
                : int.MaxValue;
            int comparison = leftOrder.CompareTo(rightOrder);
            return comparison != 0 ? comparison : originalOrder[left].CompareTo(originalOrder[right]);
        });

        _sorts.Clear();
        foreach (SortDescriptor sort in state.Sort)
        {
            if (sort.Direction == SortDirection.None
                || !columns.TryGetValue(sort.Property, out OmniDataGridColumn<TItem>? column)
                || !column.Sortable)
                continue;
            _sorts.Add((column, sort.Direction));
        }

        _filters.Clear();
        foreach (DataGridFilterViewState filter in state.Filters)
        {
            if (!columns.TryGetValue(filter.Property, out OmniDataGridColumn<TItem>? column)
                || !column.Filterable
                || string.IsNullOrWhiteSpace(filter.Value))
                continue;
            _filters[column] = new FilterDescriptor(
                filter.Property,
                filter.Operator,
                filter.Value,
                filter.SecondValue);
        }

        _groupLevels.Clear();
        if (!IsHierarchyMode)
        {
            foreach (DataGridGroupViewState group in state.Groups)
            {
                if (columns.TryGetValue(group.Property, out OmniDataGridColumn<TItem>? column)
                    && column.Groupable)
                    _groupLevels.Add(new GroupLevelSpec(column, group.Interval));
            }
        }
        _search = state.Search;
        _page = 0;
        _shapeMutation++;
        _hasShape = false;
        _collapsedGroups.Clear();
        _autoCollapsePending = true;
        _applyingViewState = true;
        try
        {
            await ApplyAndRenderAsync();
        }
        finally
        {
            _applyingViewState = false;
        }
    }

    private Dictionary<string, OmniDataGridColumn<TItem>> BuildColumnMap()
    {
        var columns = new Dictionary<string, OmniDataGridColumn<TItem>>(_columns.Count, StringComparer.Ordinal);
        foreach (OmniDataGridColumn<TItem> column in _columns)
        {
            string property = column.ResolvedPropertyName;
            if (string.IsNullOrWhiteSpace(property))
                throw new InvalidOperationException(
                    "DataGrid view state requires a stable PropertyName on every column.");
            if (!columns.TryAdd(property, column))
                throw new InvalidOperationException(
                    $"DataGrid view state requires unique PropertyName values; '{property}' is duplicated.");
        }
        return columns;
    }

    private async Task NotifyViewStateChangedAsync()
    {
        if (_applyingViewState || Volatile.Read(ref _disposeState) != 0) return;

        // Capturing requires a stable PropertyName on every column. Only grids that opted
        // into view state should pay that: a template or actions column legitimately has no
        // property, and sorting one used to throw even when nothing consumed the state.
        // This mirrors the guard in InitializeOrApplyViewStateAsync.
        string? persistKey = PersistKey;
        bool notifies = ViewStateChanged.HasDelegate;
        if (!notifies && string.IsNullOrWhiteSpace(persistKey)) return;

        DataGridViewState state = CaptureViewState();
        if (notifies) await ViewStateChanged.InvokeAsync(state);
        if (string.IsNullOrWhiteSpace(persistKey)) return;

        int sequence = Interlocked.Increment(ref _viewStatePersistSequence);
        await _viewStatePersistGate.WaitAsync();
        try
        {
            if (sequence == Volatile.Read(ref _viewStatePersistSequence)
                && Volatile.Read(ref _disposeState) == 0)
                await StateStorage.SaveAsync(persistKey, state);
        }
        finally
        {
            _viewStatePersistGate.Release();
        }
    }

    private async Task PrepareHierarchyAsync()
    {
        ConfigureHierarchy();

        var sourceChanged = !_hierarchyConfigured
            || !ReferenceEquals(_lastHierarchySource, HierarchySourceIdentity)
            || !Equals(_lastKeySelector, KeySelector)
            || !Equals(_lastChildren, Children)
            || !Equals(_lastHasChildren, HasChildren)
            || !Equals(_lastChildrenProvider, ChildrenProvider)
            || !Equals(_lastInitiallyExpanded, InitiallyExpanded);

        var limits = (
            MaxChildrenPerNode,
            MaxCachedNodes,
            MaxCachedItems,
            MaxVisibleRows,
            MaxDepth,
            MaxConcurrentLoads);
        var limitsChanged = _hierarchyConfigured && limits != _lastHierarchyLimits;
        var expandedChanged = _hierarchyConfigured && !ReferenceEquals(_lastExpandedKeys, ExpandedKeys);

        _lastHierarchySource = HierarchySourceIdentity;
        _lastKeySelector = KeySelector;
        _lastChildren = Children;
        _lastHasChildren = HasChildren;
        _lastChildrenProvider = ChildrenProvider;
        _lastInitiallyExpanded = InitiallyExpanded;
        _lastHierarchyLimits = limits;
        _hierarchyConfigured = true;

        if (sourceChanged)
        {
            _hierarchy.ResetData(_view, ExpandedKeys, InitiallyExpanded);
        }
        else
        {
            if (expandedChanged) _hierarchy.SynchronizeExpandedKeys(ExpandedKeys);
            _hierarchy.UpdateItems(_view);
            if (limitsChanged) await _hierarchy.ApplyLimitsAsync();
        }

        _lastExpandedKeys = ExpandedKeys;
        EnsureHierarchyFocusKey();
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

    private Task DisableHierarchyAsync()
    {
        if (!_hierarchyConfigured) return Task.CompletedTask;
        _hierarchy.Configure(null, null, null, null, 1, 1, 1, 1, 1, 1);
        _hierarchy.ResetData(null, null, null);
        _hierarchyConfigured = false;
        _focusedHierarchyKey = null;
        return Task.CompletedTask;
    }

    private async Task CollapseHierarchyItemAsync(TItem item)
    {
        _focusedHierarchyKey = _hierarchy.GetKey(item);
        await _hierarchy.CollapseAsync(item);
    }

    private async Task ToggleHierarchyItemAsync(TItem item, CancellationToken cancellationToken = default)
    {
        _focusedHierarchyKey = _hierarchy.GetKey(item);
        await _hierarchy.ToggleAsync(item, cancellationToken);
    }

    private async Task HandleHierarchyRowClickAsync(HierarchyRow<TItem> row)
    {
        _focusedHierarchyKey = row.Key;
        await HandleRowClick(row.Item);
    }

    private async Task HandleHierarchyRowDoubleClickAsync(HierarchyRow<TItem> row)
    {
        if (row.HasChildren) await ToggleHierarchyItemAsync(row.Item);
    }

    private async Task HandleHierarchyKeyDownAsync(HierarchyRow<TItem> row, KeyboardEventArgs args)
    {
        _focusedHierarchyKey = row.Key;
        switch (args.Key)
        {
            case "ArrowRight" when row.HasChildren && !_hierarchy.IsExpanded(row.Key):
                await _hierarchy.ExpandAsync(row.Item);
                break;
            case "ArrowLeft" when row.HasChildren && _hierarchy.IsExpanded(row.Key):
                await _hierarchy.CollapseAsync(row.Item);
                break;
            case " " when AllowMultiSelection:
                await OnRowSelectChangedAsync(row.Item, !Selection.Contains(row.Item));
                break;
            case "Enter":
            case " ":
                await HandleRowClick(row.Item);
                break;
        }
    }

    private int HierarchyRowTabIndex(HierarchyRow<TItem> row)
        => Equals(_focusedHierarchyKey, row.Key) ? 0 : -1;

    private string? HierarchyExpandedAria(HierarchyRow<TItem> row)
        => row.HasChildren
            ? _hierarchy.IsExpanded(row.Key) ? "true" : "false"
            : null;

    private string? HierarchySelectedAria(TItem item)
        => AllowMultiSelection
            ? Selection.Contains(item) ? "true" : "false"
            : null;

    private void EnsureHierarchyFocusKey()
    {
        if (HierarchyRows.Count == 0)
        {
            _focusedHierarchyKey = null;
            return;
        }

        if (_focusedHierarchyKey is not null)
        {
            foreach (var row in HierarchyRows)
            {
                if (Equals(row.Key, _focusedHierarchyKey)) return;
            }
        }
        _focusedHierarchyKey = HierarchyRows[0].Key;
    }

    private Task RetryHierarchyAsync(TItem item) => _hierarchy.RetryAsync(item);

    private static object HierarchyErrorRowKey(object key) => new HierarchyErrorKey(key);

    private async Task ExportCsvAsync()
    {
        if (!TryBeginExport(out var exportSource)) return;

        var iteration = new ExportIterationState();
        try
        {
            LastExportWasTruncated = false;
            await InvokeAsync(StateHasChanged);

            var maxRows = Math.Clamp(MaxExportRows, 1, 1_000_000);
            var batchSize = Math.Clamp(ExportBatchSize, 1, Math.Min(maxRows, 10_000));
            OmniDataGridColumn<TItem>[] columns = [.. VisibleColumns];
            var state = CreateExportState(skip: 0, top: batchSize);
            var rows = CreateExportRows(columns, state, maxRows, batchSize, iteration, exportSource.Token);

            await DownloadCsvAsync(columns, rows, exportSource);

            LastExportWasTruncated = iteration.Truncated;
            if (iteration.Truncated && ExportTruncated.HasDelegate)
                await ExportTruncated.InvokeAsync(maxRows);
        }
        catch (OperationCanceledException) when (exportSource.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (ExportFailed.HasDelegate)
                await ExportFailed.InvokeAsync(exception);
            else if (Volatile.Read(ref _disposeState) == 0)
                await DispatchExceptionAsync(exception);
        }
        finally
        {
            CompleteExport(exportSource);
            if (Volatile.Read(ref _disposeState) == 0)
                await InvokeAsync(StateHasChanged);
        }
    }

    private bool TryBeginExport(out CancellationTokenSource source)
    {
        lock (_exportSync)
        {
            if (Volatile.Read(ref _disposeState) != 0 || _exportCts is not null)
            {
                source = null!;
                return false;
            }

            source = new CancellationTokenSource();
            _exportCts = source;
            Volatile.Write(ref _exporting, 1);
            return true;
        }
    }

    private void CompleteExport(CancellationTokenSource source)
    {
        lock (_exportSync)
        {
            if (ReferenceEquals(_exportCts, source))
                _exportCts = null;
            Volatile.Write(ref _exporting, 0);
        }
        source.Dispose();
    }

    private void CancelExport()
    {
        CancellationTokenSource? source;
        lock (_exportSync)
        {
            source = _exportCts;
        }
        CancelSafely(source);
    }

    private GridState<TItem> CreateExportState(int skip, int top)
    {
        SortDescriptor[] sorts = [.. _sorts
            .Where(sort => sort.Dir != SortDirection.None)
            .Select(sort => new SortDescriptor(sort.Col.ResolvedPropertyName, sort.Dir))];
        FilterDescriptor[] filters = [.. _filters.Values];
        return new(
            skip,
            top,
            string.IsNullOrWhiteSpace(_search) ? null : _search.Trim(),
            sorts,
            filters,
            []);
    }

    private IAsyncEnumerable<TItem> CreateExportRows(
        IReadOnlyList<OmniDataGridColumn<TItem>> columns,
        GridState<TItem> state,
        int maxRows,
        int batchSize,
        ExportIterationState iteration,
        CancellationToken cancellationToken)
    {
        if (ExportProvider is { } exportProvider)
            return EnumerateCustomExportAsync(
                exportProvider,
                state with { Top = maxRows + 1 },
                maxRows,
                iteration,
                cancellationToken);

        if (DataProvider is { } dataProvider)
            return EnumeratePagedExportAsync(dataProvider, state, maxRows, batchSize, iteration, cancellationToken);

        var snapshot = SnapshotInMemoryExport(columns, maxRows, iteration);
        return EnumerateSnapshotAsync(snapshot, cancellationToken);
    }

    private TItem[] SnapshotInMemoryExport(
        IReadOnlyList<OmniDataGridColumn<TItem>> columns,
        int maxRows,
        ExportIterationState iteration)
    {
        IEnumerable<TItem> source = Data ?? [];
        if (!string.IsNullOrWhiteSpace(_search))
        {
            var search = _search.Trim();
            source = source.Where(item => columns.Any(column =>
                column.GetCellText(item)?.Contains(search, StringComparison.OrdinalIgnoreCase) == true));
        }
        source = ApplyColumnFilters(source);
        source = ApplyMultiSort(source);

        var rows = new List<TItem>(Math.Min(maxRows, 1024));
        foreach (var item in source)
        {
            if (rows.Count >= maxRows)
            {
                iteration.Truncated = true;
                break;
            }
            rows.Add(item);
        }
        return [.. rows];
    }

    private static async IAsyncEnumerable<TItem> EnumerateSnapshotAsync(
        IReadOnlyList<TItem> rows,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await Task.CompletedTask.ConfigureAwait(false);
        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
        }
    }

    private static async IAsyncEnumerable<TItem> EnumerateCustomExportAsync(
        GridExportProvider<TItem> provider,
        GridState<TItem> state,
        int maxRows,
        ExportIterationState iteration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var source = provider(state, cancellationToken)
            ?? throw new InvalidOperationException("The grid export provider returned a null sequence.");
        await foreach (var item in source.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            if (iteration.Rows >= maxRows)
            {
                iteration.Truncated = true;
                yield break;
            }
            iteration.Rows++;
            yield return item;
        }
    }

    private static async IAsyncEnumerable<TItem> EnumeratePagedExportAsync(
        GridDataProvider<TItem> provider,
        GridState<TItem> baseState,
        int maxRows,
        int batchSize,
        ExportIterationState iteration,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var skip = 0;
        while (iteration.Rows < maxRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var take = Math.Min(batchSize, maxRows - iteration.Rows);
            var page = await provider(
                baseState with { Skip = skip, Top = take },
                cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("The grid data provider returned null.");
            var items = page.Items
                ?? throw new InvalidOperationException("The grid data provider returned a null item page.");
            if (items.Count == 0) yield break;

            var consumed = 0;
            foreach (var item in items)
            {
                if (iteration.Rows >= maxRows)
                {
                    iteration.Truncated = true;
                    yield break;
                }
                iteration.Rows++;
                consumed++;
                yield return item;
            }

            skip += consumed;
            var totalCount = Math.Max(0, page.TotalCount);
            if (skip >= totalCount || consumed < take) yield break;
            if (iteration.Rows >= maxRows)
            {
                iteration.Truncated = skip < totalCount;
                yield break;
            }
        }
    }

    private async Task DownloadCsvAsync(
        IReadOnlyList<OmniDataGridColumn<TItem>> columns,
        IAsyncEnumerable<TItem> rows,
        CancellationTokenSource exportSource)
    {
        var pipe = new Pipe(new PipeOptions(
            pauseWriterThreshold: 64 * 1024,
            resumeWriterThreshold: 32 * 1024,
            useSynchronizationContext: false));
        var producer = ProduceCsvAsync(pipe.Writer, columns, rows, exportSource.Token);
        Exception? downloadFailure = null;
        Exception? producerFailure = null;

        await using var stream = pipe.Reader.AsStream(leaveOpen: true);
        try
        {
            await Downloads.DownloadAsync(
                ExportFilename,
                stream,
                "text/csv;charset=utf-8",
                exportSource.Token);
        }
        catch (Exception exception)
        {
            downloadFailure = exception;
            CancelSafely(exportSource);
        }

        try
        {
            await producer.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            producerFailure = exception;
        }
        await pipe.Reader.CompleteAsync(producerFailure ?? downloadFailure).ConfigureAwait(false);

        var failure = producerFailure ?? downloadFailure;
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static async Task ProduceCsvAsync(
        PipeWriter destination,
        IReadOnlyList<OmniDataGridColumn<TItem>> columns,
        IAsyncEnumerable<TItem> rows,
        CancellationToken cancellationToken)
    {
        Exception? failure = null;
        try
        {
            await using var stream = destination.AsStream(leaveOpen: true);
            await using var writer = new StreamWriter(
                stream,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                bufferSize: 16 * 1024,
                leaveOpen: true);

            WriteCsvRow(writer, columns, static column => column.Title ?? string.Empty);
            var rowsSinceFlush = 0;
            await foreach (var item in rows.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                WriteCsvRow(writer, columns, column => column.GetCellText(item) ?? string.Empty);
                if (++rowsSinceFlush < 64) continue;
                rowsSinceFlush = 0;
                await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            await destination.CompleteAsync(failure).ConfigureAwait(false);
        }
    }

    private static void WriteCsvRow(
        TextWriter writer,
        IReadOnlyList<OmniDataGridColumn<TItem>> columns,
        Func<OmniDataGridColumn<TItem>, string> valueSelector)
    {
        for (var index = 0; index < columns.Count; index++)
        {
            if (index > 0) writer.Write(',');
            WriteCsvCell(writer, valueSelector(columns[index]));
        }
        writer.WriteLine();
    }

    private static void WriteCsvCell(TextWriter writer, string value)
    {
        var span = value.AsSpan();
        if (span.IndexOfAny(",\"\r\n".AsSpan()) < 0)
        {
            writer.Write(span);
            return;
        }

        writer.Write('"');
        var segmentStart = 0;
        for (var index = 0; index < span.Length; index++)
        {
            if (span[index] != '"') continue;
            writer.Write(span[segmentStart..(index + 1)]);
            writer.Write('"');
            segmentStart = index + 1;
        }
        writer.Write(span[segmentStart..]);
        writer.Write('"');
    }

    private sealed class ExportIterationState
    {
        internal int Rows { get; set; }
        internal bool Truncated { get; set; }
    }

    private async Task RefreshHierarchyAsync()
    {
        if (Volatile.Read(ref _disposeState) != 0) return;
        EnsureHierarchyFocusKey();
        await InvokeAsync(StateHasChanged);
    }

    private async Task NotifyHierarchyExpandedAsync(IReadOnlyCollection<object> snapshot)
    {
        ExpandedKeys = snapshot;
        _lastExpandedKeys = snapshot;
        if (ExpandedKeysChanged.HasDelegate)
            await ExpandedKeysChanged.InvokeAsync(snapshot);
    }

    private Task NotifyHierarchyLoadFailedAsync(Exception exception)
        => HierarchyLoadFailed.HasDelegate
            ? HierarchyLoadFailed.InvokeAsync(exception)
            : Task.CompletedTask;

    private async Task DispatchHierarchyAsync(Func<Task> action)
    {
        if (Volatile.Read(ref _disposeState) != 0) return;
        try
        {
            await InvokeAsync(action);
        }
        catch (ObjectDisposedException) when (Volatile.Read(ref _disposeState) != 0)
        {
        }
        catch (InvalidOperationException) when (Volatile.Read(ref _disposeState) != 0)
        {
        }
    }

    private void ObserveHierarchyTask(Task task)
        => ObserveTask(ObserveHierarchyTaskAsync(task), "OmniDataGrid.HierarchyLoad");

    private async Task ObserveHierarchyTaskAsync(Task task)
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
            if (Volatile.Read(ref _disposeState) != 0) return;
            try
            {
                await DispatchExceptionAsync(exception);
            }
            catch when (Volatile.Read(ref _disposeState) != 0)
            {
            }
        }
    }

    private sealed record HierarchyErrorKey(object Key);
}
