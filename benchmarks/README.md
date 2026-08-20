# Benchmarks — Omni.Blazor

BenchmarkDotNet suites for the library's hot paths, with the measured figures that
back the allocation budgets in
[`test/Omni.Blazor.Tests/Performance/AllocationBudgetTests.cs`](../test/Omni.Blazor.Tests/Performance/AllocationBudgetTests.cs).

`docs/engineering-quality.md` requires a benchmark or allocation profile *before*
optimising, and that the evidence is kept with the change. This directory is that
evidence.

## Running

```bash
dotnet run -c Release --project benchmarks/Omni.Blazor.Benchmarks -- --filter "*"
```

Release is mandatory — BenchmarkDotNet refuses to take a Debug build seriously, and
rightly so. Narrow with `--filter "*Css*"`, or add `--job short` for a quick read
(that is what produced the tables below; a full job takes considerably longer and
tightens the error bars, but does not move the allocation numbers, which are exact).

## What is measured, and why

| Suite | Why it is a hot path |
|---|---|
| `CssBuilderBenchmarks` | Every component builds its root class string on every render. Most-executed allocation in the library. |
| `MarkdownBenchmarks` | `OmniMarkdown` memoises per `(source, allowHtml)`, so re-renders are free — but a streaming reply feeds a *different* source per chunk, which the cache cannot help with. |
| `DataGridHierarchyBenchmarks` | `HierarchyState` backs both `OmniDataGrid` and `OmniTreeGrid`; every expand, collapse and items change rebuilds the flat row list. |
| `ManifestGenBenchmarks` | The generator reflects over the whole library and runs in CI on every push (the manifest drift check). |

Internals (`MarkdownRenderer`, `HierarchyState`) are benchmarked directly through
`InternalsVisibleTo` rather than through a rendered component: going via bUnit would
fold the renderer, DI and the diff algorithm into the number.

## Measured — 2026-08-20, `--job short`

Allocation figures are exact (not sampled); times carry the usual short-job variance.

### CssBuilder / StyleBuilder

| Benchmark | Mean | Allocated |
|---|---|---|
| `Typical` (OmniButton-shaped chain) | 138 ns | **128 B** |
| `AllConditionsFalse` | 126 ns | **40 B** |
| `LongChain` (OmniDataGrid-shaped) | 186 ns | **368 B** |
| `StyleTypical` | 160 ns | **184 B** |

Healthy: skipped classes cost nothing, and the builder scales with what is actually
appended rather than with chain length.

### Markdown

| Benchmark | Mean | Allocated |
|---|---|---|
| `ShortReply` | 39.6 µs | **84 KB** |
| `FullDocument` (12 sections) | 2.34 ms | **3.35 MB** |
| `StreamingChunk` (half a document) | 987 µs | **1.67 MB** |
| `SanitizeHostileHtml` | 7.9 µs | **6.3 KB** |

**The finding worth acting on.** ~32 regex passes over the source cost 3.35 MB for one
document render. Memoisation hides this for static content, but *not* for streaming:
`StreamingChunk` is the uncached path an assistant reply takes on every chunk, at
1.67 MB a chunk.

This is direct evidence against the reasoning that deferred roadmap item 19
(`[GeneratedRegex]` in `MarkdownRenderer`, deferred as "low ROI after memoisation").
The premise holds only where the cache applies. Item 19 should be reconsidered with
these numbers, and the AI/chat streaming path is the case to optimise for.

### DataGrid hierarchy

| Benchmark | 100 rows | 1 000 rows |
|---|---|---|
| `FlatRows` | 6.3 µs / **20.7 KB** | 57.6 µs / **176 KB** |
| `TreeCollapsed` | 1.0 µs / **3.2 KB** | 7.0 µs / **20.7 KB** |
| `TreeFullyExpanded` | 1.5 µs / **4.2 KB** | 53.8 µs / **132 KB** |
| `RebuildOnItemsChange` | 2.4 µs / **5.6 KB** | 98.2 µs / **191 KB** |

Scales linearly with visible rows, which is the expected shape — a collapsed tree
costs a fraction of an expanded one, so collapsing is a real saving rather than a
cosmetic one.

### Manifest generator

| Benchmark | Mean | Allocated |
|---|---|---|
| `BuildWholeLibrary` (210 components) | 22.1 ms | **3.16 MB** |
| `FriendlyTypeNames` (per catalog pass) | 11.8 µs | **10.5 KB** |
| `XmlIds` (per catalog pass) | 1.9 µs | **0 B** |

22 ms for the whole catalog is not worth optimising — it runs once per CI push and
nobody waits on it. `XmlIds` allocating nothing at all is a pleasant surprise worth
not regressing.

## Budgets

The numbers above are the baseline for the budgets asserted in the normal test suite.
BenchmarkDotNet takes minutes, so it cannot gate a push; the budget tests measure the
same paths with `GC.GetAllocatedBytesForCurrentThread()` and run in seconds. Both
agree — the `CssBuilder` typical chain reports 128 B under either.

Budgets carry generous headroom on purpose: they exist to catch a regression *in kind*
(a LINQ chain or a closure appearing in a render loop), not to police a few bytes
across machines and runtimes. Raising one is a legitimate outcome — re-run the
benchmark, confirm the new cost is intended, and update the budget and the table above
in the same commit.
