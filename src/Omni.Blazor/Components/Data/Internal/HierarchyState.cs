using Omni.Blazor.Models;

namespace Omni.Blazor.Components;

/// <summary>
/// Owns hierarchy identity, expansion, flattening, lazy loading and bounded cache state.
/// Component state is renderer-owned; only the in-flight operation registry is shared
/// across provider continuations and therefore protected by <see cref="_operationSync"/>.
/// </summary>
internal sealed class HierarchyState<TItem> : IDisposable
{
    private readonly object _operationSync = new();
    private readonly CancellationTokenSource _lifetimeSource = new();
    private readonly CancellationToken _lifetimeToken;
    private readonly Func<Task> _stateChangedAsync;
    private readonly Func<IReadOnlyCollection<object>, Task> _expandedChangedAsync;
    private readonly Func<Exception, Task> _loadFailedAsync;
    private readonly Func<Func<Task>, Task> _dispatchAsync;
    private readonly List<TItem> _roots = [];
    private readonly List<HierarchyRow<TItem>> _rows = [];
    private readonly HashSet<object> _expanded = [];
    private readonly HashSet<object> _loadingKeys = [];
    private readonly Dictionary<object, NodeLoadOperation> _loads = [];
    private readonly Dictionary<object, CacheEntry> _childrenCache = [];
    private readonly Dictionary<object, string> _nodeErrors = [];
    private readonly LinkedList<object> _cacheLru = [];
    private readonly Dictionary<object, LinkedListNode<object>> _cacheLruNodes = [];
    private Func<TItem, object>? _keySelector;
    private Func<TItem, IEnumerable<TItem>?>? _children;
    private Func<TItem, bool>? _hasChildren;
    private HierarchyChildrenProvider<TItem>? _childrenProvider;
    private Task? _pendingBatch;
    private int _maxChildrenPerNode = 1000;
    private int _maxCachedNodes = 500;
    private int _maxCachedItems = 10_000;
    private int _maxVisibleRows = 5000;
    private int _maxDepth = 64;
    private int _maxConcurrentLoads = 4;
    private int _cachedItemCount;
    private int _disposeState;
    private bool _limitReached;
    private bool _sourceTruncated;
    private bool _applyInitialExpansion;
    private Func<TItem, bool>? _initiallyExpanded;

    internal HierarchyState(
        Func<Task> stateChangedAsync,
        Func<IReadOnlyCollection<object>, Task> expandedChangedAsync,
        Func<Exception, Task> loadFailedAsync,
        Func<Func<Task>, Task> dispatchAsync)
    {
        _stateChangedAsync = stateChangedAsync;
        _expandedChangedAsync = expandedChangedAsync;
        _loadFailedAsync = loadFailedAsync;
        _dispatchAsync = dispatchAsync;
        _lifetimeToken = _lifetimeSource.Token;
    }

    internal IReadOnlyList<HierarchyRow<TItem>> Rows => _rows;
    internal ICollection<HierarchyRow<TItem>> RowsCollection => _rows;
    internal bool LimitReached => _limitReached;
    internal int CachedNodeCount => _childrenCache.Count;
    internal int CachedItemCount => _cachedItemCount;
    internal int ErrorCount => _nodeErrors.Count;
    internal bool IsEnabled => _children is not null || _childrenProvider is not null;

    internal bool IsLoading
    {
        get
        {
            lock (_operationSync)
            {
                return _loadingKeys.Count > 0;
            }
        }
    }

    internal void Configure(
        Func<TItem, object>? keySelector,
        Func<TItem, IEnumerable<TItem>?>? children,
        Func<TItem, bool>? hasChildren,
        HierarchyChildrenProvider<TItem>? childrenProvider,
        int maxChildrenPerNode,
        int maxCachedNodes,
        int maxCachedItems,
        int maxVisibleRows,
        int maxDepth,
        int maxConcurrentLoads)
    {
        _keySelector = keySelector;
        _children = children;
        _hasChildren = hasChildren;
        _childrenProvider = childrenProvider;
        _maxChildrenPerNode = Math.Clamp(maxChildrenPerNode, 1, 10_000);
        _maxCachedNodes = Math.Clamp(maxCachedNodes, 1, 10_000);
        _maxCachedItems = Math.Clamp(maxCachedItems, 1, 100_000);
        _maxVisibleRows = Math.Clamp(maxVisibleRows, 1, 100_000);
        _maxDepth = Math.Clamp(maxDepth, 1, 256);
        _maxConcurrentLoads = Math.Clamp(maxConcurrentLoads, 1, 32);
    }

    internal void ResetData(
        IEnumerable<TItem>? items,
        IReadOnlyCollection<object>? expandedKeys,
        Func<TItem, bool>? initiallyExpanded)
    {
        if (IsDisposed) return;

        CancelAllLoads();
        ClearCache();
        _nodeErrors.Clear();
        _expanded.Clear();
        AddExpandedKeys(expandedKeys);
        _initiallyExpanded = expandedKeys is null ? initiallyExpanded : null;
        _applyInitialExpansion = _initiallyExpanded is not null;
        ReplaceRoots(items);
        RebuildRows();
        _applyInitialExpansion = false;
    }

    internal void UpdateItems(IEnumerable<TItem>? items)
    {
        if (IsDisposed) return;
        ReplaceRoots(items);
        RebuildRows();
    }

    internal void SynchronizeExpandedKeys(IReadOnlyCollection<object>? expandedKeys)
    {
        if (IsDisposed) return;

        object[] previous = [.. _expanded];
        _expanded.Clear();
        AddExpandedKeys(expandedKeys);
        foreach (var key in previous)
        {
            if (!_expanded.Contains(key)) CancelSubtreeLoads(key);
        }
        RebuildRows();
    }

    internal async Task ApplyLimitsAsync()
    {
        if (IsDisposed) return;

        var expansionChanged = TrimCache();
        TItem[] roots = [.. _roots];
        ReplaceRoots(roots);
        RebuildRows();
        if (expansionChanged) await NotifyExpandedAsync();
        await NotifyStateChangedAsync();
    }

    internal async Task ExpandAsync(TItem item, CancellationToken cancellationToken = default)
    {
        if (IsDisposed || item is null) return;

        var key = GetKey(item);
        var changed = _expanded.Add(key);
        _nodeErrors.Remove(key);
        RebuildRows();
        if (changed) await NotifyExpandedAsync();
        if (IsDisposed || !_expanded.Contains(key)) return;

        await NotifyStateChangedAsync();
        if (IsDisposed || !_expanded.Contains(key)) return;
        await EnsureChildrenLoadedAsync(item, key, cancellationToken).ConfigureAwait(false);
    }

    internal async Task CollapseAsync(TItem item)
    {
        if (IsDisposed || item is null) return;

        var key = GetKey(item);
        if (!_expanded.Remove(key)) return;
        CancelSubtreeLoads(key);
        RebuildRows();
        await NotifyExpandedAsync();
        await NotifyStateChangedAsync();
    }

    internal Task ToggleAsync(TItem item, CancellationToken cancellationToken = default)
        => IsExpanded(GetKey(item))
            ? CollapseAsync(item)
            : ExpandAsync(item, cancellationToken);

    internal async Task ExpandAllAsync(CancellationToken cancellationToken = default)
    {
        if (IsDisposed || !IsEnabled) return;

        var visited = new HashSet<object>();
        var stack = new Stack<(TItem Item, int Level)>(Math.Min(_roots.Count, _maxVisibleRows));
        for (var index = _roots.Count - 1; index >= 0; index--)
            stack.Push((_roots[index], 0));

        var changed = false;
        while (stack.Count > 0 && visited.Count < _maxVisibleRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (item, level) = stack.Pop();
            var key = GetKey(item);
            if (!visited.Add(key) || !ResolveHasChildren(item, key)) continue;

            changed |= _expanded.Add(key);
            _nodeErrors.Remove(key);
            if (_childrenProvider is not null)
                await EnsureChildrenLoadedAsync(item, key, cancellationToken);

            if (IsDisposed || level + 1 >= _maxDepth) continue;
            IReadOnlyList<TItem> children = GetChildrenSnapshot(item, key);
            for (var index = children.Count - 1; index >= 0; index--)
                stack.Push((children[index], level + 1));
        }

        RebuildRows();
        if (changed) await NotifyExpandedAsync();
        await NotifyStateChangedAsync();
    }

    internal async Task CollapseAllAsync()
    {
        if (IsDisposed || _expanded.Count == 0) return;

        CancelAllLoads();
        _expanded.Clear();
        RebuildRows();
        await NotifyExpandedAsync();
        await NotifyStateChangedAsync();
    }

    internal async Task ReloadAsync()
    {
        if (IsDisposed) return;

        CancelAllLoads();
        ClearCache();
        _nodeErrors.Clear();
        RebuildRows();
        await NotifyStateChangedAsync();
    }

    internal async Task ReloadAsync(TItem item, CancellationToken cancellationToken = default)
    {
        if (IsDisposed || item is null) return;

        var key = GetKey(item);
        CancelLoad(key);
        RemoveCache(key);
        _nodeErrors.Remove(key);
        RebuildRows();
        await NotifyStateChangedAsync();
        if (_expanded.Contains(key))
            await EnsureChildrenLoadedAsync(item, key, cancellationToken).ConfigureAwait(false);
    }

    internal async Task RetryAsync(TItem item, CancellationToken cancellationToken = default)
    {
        if (IsDisposed || item is null) return;

        var key = GetKey(item);
        _nodeErrors.Remove(key);
        RemoveCache(key);
        await EnsureChildrenLoadedAsync(item, key, cancellationToken).ConfigureAwait(false);
    }

    internal Task LoadPendingExpandedAsync()
    {
        if (IsDisposed || _childrenProvider is null) return Task.CompletedTask;

        lock (_operationSync)
        {
            if (_pendingBatch is { IsCompleted: false }) return _pendingBatch;
        }

        List<(TItem Item, object Key)>? pending = null;
        foreach (var row in _rows)
        {
            if (!_expanded.Contains(row.Key)
                || _childrenCache.ContainsKey(row.Key)
                || _nodeErrors.ContainsKey(row.Key)
                || IsNodeLoading(row.Key))
            {
                continue;
            }
            (pending ??= []).Add((row.Item, row.Key));
        }

        if (pending is null) return Task.CompletedTask;
        var batch = LoadPendingCoreAsync(pending);
        lock (_operationSync)
        {
            _pendingBatch = batch;
        }
        return batch;
    }

    internal bool IsExpanded(object key) => _expanded.Contains(key);

    internal bool IsNodeLoading(object key)
    {
        lock (_operationSync)
        {
            return _loadingKeys.Contains(key);
        }
    }

    internal bool TryGetError(object key, out string error)
        => _nodeErrors.TryGetValue(key, out error!);

    internal object GetKey(TItem item)
        => _keySelector?.Invoke(item)
            ?? (object?)item
            ?? throw new InvalidOperationException("Hierarchy items and keys cannot be null.");

    private async Task LoadPendingCoreAsync(IReadOnlyList<(TItem Item, object Key)> pending)
    {
        try
        {
            var next = -1;
            var workerCount = Math.Min(_maxConcurrentLoads, pending.Count);
            var workers = new Task[workerCount];
            for (var index = 0; index < workerCount; index++)
                workers[index] = LoadPendingWorkerAsync(pending, () => Interlocked.Increment(ref next));
            await Task.WhenAll(workers).ConfigureAwait(false);
        }
        finally
        {
            lock (_operationSync)
            {
                _pendingBatch = null;
            }
        }
    }

    private async Task LoadPendingWorkerAsync(
        IReadOnlyList<(TItem Item, object Key)> pending,
        Func<int> nextIndex)
    {
        while (!IsDisposed)
        {
            var index = nextIndex();
            if ((uint)index >= (uint)pending.Count) return;
            var request = pending[index];
            await _dispatchAsync(
                () => EnsureChildrenLoadedAsync(request.Item, request.Key, CancellationToken.None))
                .ConfigureAwait(false);
        }
    }

    private async Task EnsureChildrenLoadedAsync(
        TItem item,
        object key,
        CancellationToken cancellationToken)
    {
        if (_childrenProvider is null
            || _nodeErrors.ContainsKey(key)
            || TryGetCachedChildren(key, out _))
        {
            return;
        }

        NodeLoadOperation operation;
        var isOwner = false;
        lock (_operationSync)
        {
            if (IsDisposed) return;
            if (!_loads.TryGetValue(key, out operation!))
            {
                var source = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    _lifetimeToken);
                operation = new NodeLoadOperation(item, source);
                _loads[key] = operation;
                _loadingKeys.Add(key);
                isOwner = true;
            }
        }

        if (isOwner)
        {
            await NotifyStateChangedAsync();
            if (IsDisposed || !_expanded.Contains(key))
                CancelSafely(operation.Source);
            operation.Runner = ObserveLoadAndPublishAsync(key, operation);
        }

        await operation.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ObserveLoadAndPublishAsync(object key, NodeLoadOperation operation)
    {
        try
        {
            await LoadAndPublishChildrenAsync(key, operation).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            operation.Completion.TrySetException(
                new InvalidOperationException(
                    "An unexpected hierarchy failure occurred while completing a lazy load.",
                    exception));

            try
            {
                await _dispatchAsync(
                    () => IsDisposed ? Task.CompletedTask : _loadFailedAsync(exception))
                    .ConfigureAwait(false);
            }
            catch
            {
                // The provider and callback failures are already observed by this runner.
            }
        }
    }

    private async Task LoadAndPublishChildrenAsync(object key, NodeLoadOperation operation)
    {
        Exception? callbackException = null;
        try
        {
            var loaded = await _childrenProvider!(operation.Item, operation.Source.Token).ConfigureAwait(false);
            operation.Source.Token.ThrowIfCancellationRequested();
            if (!IsCurrentOperation(key, operation)) return;

            await _dispatchAsync(async () =>
            {
                if (IsDisposed || !IsCurrentOperation(key, operation) || !_expanded.Contains(key)) return;

                var limit = Math.Min(_maxChildrenPerNode, _maxCachedItems);
                var bounded = new List<TItem>(Math.Min(limit, loaded?.Count ?? 0));
                if (loaded is not null)
                {
                    foreach (var child in loaded)
                    {
                        if (bounded.Count >= limit) break;
                        if (child is not null) bounded.Add(child);
                    }
                }

                var expansionChanged = AddCache(key, bounded);
                _nodeErrors.Remove(key);
                if (expansionChanged) await NotifyExpandedAsync();
            }).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (operation.Source.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (IsCurrentOperation(key, operation))
            {
                try
                {
                    await _dispatchAsync(async () =>
                    {
                        if (IsDisposed || !IsCurrentOperation(key, operation)) return;
                        AddError(key, exception.Message);
                        await _loadFailedAsync(exception);
                    }).ConfigureAwait(false);
                }
                catch (Exception eventException)
                {
                    callbackException = eventException;
                }
            }
        }
        finally
        {
            lock (_operationSync)
            {
                if (_loads.TryGetValue(key, out var current) && ReferenceEquals(current, operation))
                {
                    _loads.Remove(key);
                    _loadingKeys.Remove(key);
                }
            }

            operation.Source.Dispose();
            try
            {
                if (!IsDisposed)
                {
                    await _dispatchAsync(async () =>
                    {
                        if (IsDisposed) return;
                        RebuildRows();
                        await NotifyStateChangedAsync();
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception dispatchException)
            {
                callbackException ??= dispatchException;
            }

            if (callbackException is null)
                operation.Completion.TrySetResult();
            else
                operation.Completion.TrySetException(
                    new InvalidOperationException(
                        "A hierarchy callback failed while completing a lazy load.",
                        callbackException));
        }
    }

    private void ReplaceRoots(IEnumerable<TItem>? items)
    {
        if (ReferenceEquals(items, _roots)) return;

        _roots.Clear();
        _sourceTruncated = false;
        if (items is null) return;

        foreach (var item in items)
        {
            if (item is null) continue;
            if (_roots.Count >= _maxVisibleRows)
            {
                _sourceTruncated = true;
                break;
            }
            _roots.Add(item);
        }
    }

    private void RebuildRows()
    {
        _rows.Clear();
        _limitReached = _sourceTruncated;
        var visibleKeys = new HashSet<object>();
        var ancestors = new HashSet<object>();
        foreach (var item in _roots)
        {
            AppendRow(item, 0, ancestors, visibleKeys);
            if (_limitReached && _rows.Count >= _maxVisibleRows) break;
        }
    }

    private void AppendRow(
        TItem item,
        int level,
        HashSet<object> ancestors,
        HashSet<object> visibleKeys)
    {
        if (_rows.Count >= _maxVisibleRows)
        {
            _limitReached = true;
            return;
        }

        var key = GetKey(item);
        if (!visibleKeys.Add(key) || !ancestors.Add(key)) return;

        if (_applyInitialExpansion && _initiallyExpanded?.Invoke(item) == true)
            _expanded.Add(key);

        var hasChildren = ResolveHasChildren(item, key);
        _rows.Add(new(item, key, level, hasChildren, _rows.Count));
        if (level + 1 < _maxDepth && hasChildren && _expanded.Contains(key))
        {
            foreach (var child in ResolveChildren(item, key))
            {
                if (child is null) continue;
                AppendRow(child, level + 1, ancestors, visibleKeys);
                if (_limitReached) break;
            }
        }
        ancestors.Remove(key);
    }

    private bool ResolveHasChildren(TItem item, object key)
    {
        if (_hasChildren is not null) return _hasChildren(item);
        if (_childrenProvider is not null)
            return !TryGetCachedChildren(key, out var loaded) || loaded.Count > 0;
        if (_children is null) return false;

        var children = _children(item);
        if (children is null) return false;
        if (children.TryGetNonEnumeratedCount(out var count)) return count > 0;
        using var enumerator = children.GetEnumerator();
        return enumerator.MoveNext();
    }

    private IEnumerable<TItem> ResolveChildren(TItem item, object key)
    {
        if (_children is not null) return _children(item) ?? [];
        return TryGetCachedChildren(key, out var children) ? children : [];
    }

    private IReadOnlyList<TItem> GetChildrenSnapshot(TItem item, object key)
    {
        IEnumerable<TItem> source = ResolveChildren(item, key);
        if (source is IReadOnlyList<TItem> list && list.Count <= _maxChildrenPerNode)
            return list;

        var result = new List<TItem>(Math.Min(_maxChildrenPerNode, 64));
        foreach (var child in source)
        {
            if (result.Count >= _maxChildrenPerNode) break;
            if (child is not null) result.Add(child);
        }
        return result;
    }

    private bool TryGetCachedChildren(object key, out IReadOnlyList<TItem> children)
    {
        if (_childrenCache.TryGetValue(key, out var entry))
        {
            TouchCache(key);
            children = entry.Children;
            return true;
        }
        children = [];
        return false;
    }

    private bool AddCache(object key, IReadOnlyList<TItem> children)
    {
        RemoveCache(key);
        _childrenCache[key] = new(children);
        _cachedItemCount += children.Count;
        _cacheLruNodes[key] = _cacheLru.AddLast(key);
        return TrimCache();
    }

    private void TouchCache(object key)
    {
        if (!_cacheLruNodes.TryGetValue(key, out var node)
            || ReferenceEquals(node, _cacheLru.Last))
        {
            return;
        }
        _cacheLru.Remove(node);
        _cacheLru.AddLast(node);
    }

    private bool TrimCache()
    {
        var expansionChanged = false;
        while ((_childrenCache.Count > _maxCachedNodes || _cachedItemCount > _maxCachedItems)
               && _cacheLru.First is { } oldest)
        {
            var key = oldest.Value;
            RemoveCache(key);
            if (_expanded.Remove(key))
            {
                CancelSubtreeLoads(key);
                expansionChanged = true;
            }
        }
        return expansionChanged;
    }

    private void RemoveCache(object key)
    {
        if (_childrenCache.Remove(key, out var entry))
            _cachedItemCount -= entry.Children.Count;
        if (_cacheLruNodes.Remove(key, out var node))
            _cacheLru.Remove(node);
    }

    private void ClearCache()
    {
        _childrenCache.Clear();
        _cacheLru.Clear();
        _cacheLruNodes.Clear();
        _cachedItemCount = 0;
    }

    private void AddExpandedKeys(IReadOnlyCollection<object>? keys)
    {
        if (keys is null) return;
        foreach (var key in keys)
        {
            if (key is null) continue;
            _expanded.Add(key);
            if (_expanded.Count >= _maxVisibleRows) break;
        }
    }

    private void AddError(object key, string error)
    {
        if (!_nodeErrors.ContainsKey(key) && _nodeErrors.Count >= _maxCachedNodes)
        {
            using var enumerator = _nodeErrors.Keys.GetEnumerator();
            if (enumerator.MoveNext()) _nodeErrors.Remove(enumerator.Current);
        }
        _nodeErrors[key] = error;
    }

    private async Task NotifyExpandedAsync()
    {
        if (IsDisposed) return;
        object[] snapshot = [.. _expanded];
        await _expandedChangedAsync(snapshot);
    }

    private async Task NotifyStateChangedAsync()
    {
        if (IsDisposed) return;
        try
        {
            await _stateChangedAsync();
        }
        catch (ObjectDisposedException) when (IsDisposed)
        {
        }
        catch (InvalidOperationException) when (IsDisposed)
        {
        }
    }

    private bool IsCurrentOperation(object key, NodeLoadOperation operation)
    {
        lock (_operationSync)
        {
            return !IsDisposed
                && _loads.TryGetValue(key, out var current)
                && ReferenceEquals(current, operation)
                && !operation.Source.IsCancellationRequested;
        }
    }

    private void CancelSubtreeLoads(object key)
    {
        CancelLoad(key);
        var rowIndex = -1;
        var level = -1;
        for (var index = 0; index < _rows.Count; index++)
        {
            if (!Equals(_rows[index].Key, key)) continue;
            rowIndex = index;
            level = _rows[index].Level;
            break;
        }
        if (rowIndex < 0) return;

        for (var index = rowIndex + 1; index < _rows.Count; index++)
        {
            var row = _rows[index];
            if (row.Level <= level) break;
            CancelLoad(row.Key);
        }
    }

    private void CancelLoad(object key)
    {
        NodeLoadOperation? operation;
        lock (_operationSync)
        {
            if (_loads.Remove(key, out operation))
                _loadingKeys.Remove(key);
        }
        CancelSafely(operation?.Source);
        operation?.Completion.TrySetResult();
    }

    private void CancelAllLoads()
    {
        NodeLoadOperation[] operations;
        lock (_operationSync)
        {
            operations = [.. _loads.Values];
            _loads.Clear();
            _loadingKeys.Clear();
        }
        foreach (var operation in operations)
        {
            CancelSafely(operation.Source);
            operation.Completion.TrySetResult();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        CancelAllLoads();
        CancelSafely(_lifetimeSource);
        _lifetimeSource.Dispose();
        _roots.Clear();
        _rows.Clear();
        _expanded.Clear();
        _nodeErrors.Clear();
        ClearCache();
        lock (_operationSync)
        {
            _pendingBatch = null;
        }
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

    private sealed record CacheEntry(IReadOnlyList<TItem> Children);

    private sealed class NodeLoadOperation
    {
        internal NodeLoadOperation(TItem item, CancellationTokenSource source)
        {
            Item = item;
            Source = source;
        }

        internal TItem Item { get; }
        internal CancellationTokenSource Source { get; }
        internal Task? Runner { get; set; }
        internal TaskCompletionSource Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

internal readonly record struct HierarchyRow<TItem>(
    TItem Item,
    object Key,
    int Level,
    bool HasChildren,
    int Index);
