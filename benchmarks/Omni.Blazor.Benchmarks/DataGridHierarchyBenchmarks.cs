using BenchmarkDotNet.Attributes;
using Omni.Blazor.Components;

namespace Omni.Blazor.Benchmarks;

/// <summary>
/// <c>HierarchyState</c> is the shared engine behind <c>OmniDataGrid</c> and
/// <c>OmniTreeGrid</c>. Every expand, collapse and items change rebuilds the flat
/// row list, so flattening is the grid's hot path — and the one place where row
/// count multiplies allocation.
///
/// Benchmarked directly rather than through a rendered grid: bUnit would fold the
/// renderer, DI and the diff algorithm into the number, which is not what this
/// measures. The engine is <c>internal</c>, reached via InternalsVisibleTo.
/// </summary>
[MemoryDiagnoser]
public class DataGridHierarchyBenchmarks
{
    /// <summary>Row count of the flattened tree, not of the roots.</summary>
    [Params(100, 1_000)]
    public int RowCount { get; set; }

    private Node[] _flat = [];
    private Node[] _tree = [];
    private HashSet<object> _allKeys = [];

    [GlobalSetup]
    public void Setup()
    {
        // Flat: the ordinary grid, no children at all.
        _flat = Enumerable.Range(0, RowCount).Select(i => new Node(i, [])).ToArray();

        // Tree: 10 children per root, so RowCount total rows once fully expanded.
        int roots = Math.Max(1, RowCount / 10);
        _tree = Enumerable.Range(0, roots)
            .Select(r => new Node(
                r,
                Enumerable.Range(0, 9).Select(c => new Node(r * 100 + c + 1, [])).ToArray()))
            .ToArray();

        _allKeys = _tree.Select(n => (object)n.Id).ToHashSet();
    }

    private static HierarchyState<Node> CreateState()
    {
        // The grid supplies renderer-bound callbacks; flattening never invokes them,
        // so completed tasks keep the measurement to the engine itself.
        var state = new HierarchyState<Node>(
            () => Task.CompletedTask,
            _ => Task.CompletedTask,
            _ => Task.CompletedTask,
            action => action());

        state.Configure(
            keySelector: static n => n.Id,
            children: static n => n.Children,
            hasChildren: static n => n.Children.Length > 0,
            childrenProvider: null,
            maxChildrenPerNode: 10_000,
            maxCachedNodes: 10_000,
            maxCachedItems: 100_000,
            maxVisibleRows: 100_000,
            maxDepth: 256,
            maxConcurrentLoads: 32);

        return state;
    }

    [Benchmark(Baseline = true)]
    public int FlatRows()
    {
        using var state = CreateState();
        state.ResetData(_flat, expandedKeys: null, initiallyExpanded: null);
        return state.Rows.Count;
    }

    [Benchmark]
    public int TreeCollapsed()
    {
        using var state = CreateState();
        state.ResetData(_tree, expandedKeys: null, initiallyExpanded: null);
        return state.Rows.Count;
    }

    [Benchmark]
    public int TreeFullyExpanded()
    {
        using var state = CreateState();
        state.ResetData(_tree, _allKeys, initiallyExpanded: null);
        return state.Rows.Count;
    }

    /// <summary>
    /// Re-flattening on an items change, which is what a sorted or filtered grid
    /// does on every user interaction — the state object is reused, unlike above.
    /// </summary>
    [Benchmark]
    public int RebuildOnItemsChange()
    {
        using var state = CreateState();
        state.ResetData(_tree, _allKeys, initiallyExpanded: null);
        state.UpdateItems(_tree);
        return state.Rows.Count;
    }

    public sealed record Node(int Id, Node[] Children);
}
