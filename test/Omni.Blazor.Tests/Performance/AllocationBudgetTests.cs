
namespace Omni.Blazor.Tests.Performance;

/// <summary>
/// Allocation budgets for the hot paths measured by
/// <c>benchmarks/Omni.Blazor.Benchmarks</c>. BenchmarkDotNet produces the evidence
/// but takes minutes, so it cannot gate a push; these assertions run with the normal
/// suite and fail when a change makes a hot path allocate materially more.
///
/// Each budget is the measured figure (recorded in the benchmarks README) rounded up
/// with generous headroom — they exist to catch a regression in kind, such as a LINQ
/// chain or a closure appearing in a render-loop path, not to police a few bytes.
/// Raising one is a legitimate outcome; doing it silently is not. Re-run the
/// benchmark, confirm the new cost is understood and intended, and update the
/// README's table in the same commit.
/// </summary>
public class AllocationBudgetTests
{
    /// <summary>
    /// Allocated bytes per iteration, JIT warmed up first so the measurement covers
    /// the work rather than one-time compilation. Single-threaded by construction:
    /// GetAllocatedBytesForCurrentThread only sees this thread.
    /// </summary>
    private static long MeasureBytesPerOperation(Action operation, int iterations = 100)
    {
        for (int i = 0; i < 10; i++) operation();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++) operation();
        long after = GC.GetAllocatedBytesForCurrentThread();

        return (after - before) / iterations;
    }

    private static void AssertWithinBudget(string what, long budgetBytes, Action operation, int iterations = 100)
    {
        long actual = MeasureBytesPerOperation(operation, iterations);

        Assert.True(
            actual <= budgetBytes,
            $"{what} allocated {actual} B/op, over its {budgetBytes} B budget. "
            + "Re-run benchmarks/Omni.Blazor.Benchmarks; if the new cost is intended, "
            + "raise the budget and the README table together.");
    }

    // ── CssBuilder: every component builds its root class on every render, so this
    //    is the single most-executed allocation in the library. Measured: 128 B.
    [Fact]
    public void CssBuilder_typical_component_chain_stays_within_budget()
    {
        AssertWithinBudget("CssBuilder typical chain", budgetBytes: 320, operation: static () =>
        {
            _ = CssBuilder.Default("omni-btn")
                .AddClass("omni-btn-primary")
                .AddClass("omni-btn-icon", true)
                .AddClass("omni-btn-block", false)
                .AddClass("omni-btn-loading", false)
                .AddClass("my-app-button")
                .Build();
        });
    }

    // Measured: 40 B — the common case, where most modifiers are off. Budgeted
    // separately so a change that starts allocating for skipped classes is caught
    // even if the typical chain still fits.
    [Fact]
    public void CssBuilder_with_all_conditions_false_barely_allocates()
    {
        AssertWithinBudget("CssBuilder with no conditions met", budgetBytes: 120, operation: static () =>
        {
            _ = CssBuilder.Default("omni-btn")
                .AddClass("omni-btn-icon", false)
                .AddClass("omni-btn-block", false)
                .AddClass("omni-btn-loading", false)
                .Build();
        });
    }

    // Measured: 184 B.
    [Fact]
    public void StyleBuilder_typical_chain_stays_within_budget()
    {
        AssertWithinBudget("StyleBuilder typical chain", budgetBytes: 460, operation: static () =>
        {
            _ = StyleBuilder.Default("display:flex")
                .AddStyle("gap", "8px")
                .AddStyle("min-width", "0", true)
                .AddStyle("max-height", "240px", false)
                .Build();
        });
    }

    // ── Markdown. OmniMarkdown memoises per (source, allowHtml), so a re-render is
    //    free — but a streaming reply feeds a different source on every chunk, which
    //    the cache cannot help with. This budget guards the uncached path.
    //    Measured: ~84 KB for a short reply.
    [Fact]
    public void Markdown_short_reply_stays_within_budget()
    {
        const string source = "A short **assistant** reply with `code` and a [link](https://example.com).";

        AssertWithinBudget(
            "MarkdownRenderer short reply",
            budgetBytes: 200 * 1024,
            operation: static () => _ = MarkdownRenderer.ToHtml(source),
            iterations: 50);
    }

    // ── DataGrid/TreeGrid flattening: rebuilt on every expand, collapse and items
    //    change. Measured: ~20.7 KB for 100 flat rows.
    [Fact]
    public void DataGrid_flattening_100_rows_stays_within_budget()
    {
        var rows = Enumerable.Range(0, 100).Select(i => new Row(i)).ToArray();

        AssertWithinBudget(
            "HierarchyState flatten (100 rows)",
            budgetBytes: 64 * 1024,
            operation: () =>
            {
                using var state = new HierarchyState<Row>(
                    () => Task.CompletedTask,
                    _ => Task.CompletedTask,
                    _ => Task.CompletedTask,
                    action => action());

                state.Configure(
                    keySelector: static r => r.Id,
                    children: static _ => null,
                    hasChildren: static _ => false,
                    childrenProvider: null,
                    maxChildrenPerNode: 10_000,
                    maxCachedNodes: 10_000,
                    maxCachedItems: 100_000,
                    maxVisibleRows: 100_000,
                    maxDepth: 256,
                    maxConcurrentLoads: 32);

                state.ResetData(rows, expandedKeys: null, initiallyExpanded: null);
            },
            iterations: 50);
    }

    private sealed record Row(int Id);
}
