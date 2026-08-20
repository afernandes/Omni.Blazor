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

**The finding worth acting on — and its root cause.** Rendering one 75-byte sentence
allocates 84 KB, which is ~1100× the input. That is not parsing cost; it is the .NET
static regex cache thrashing.

`MarkdownRenderer` uses **31 distinct patterns** through the *static* `Regex.Match` /
`IsMatch` / `Replace` overloads. Those consult a process-wide cache whose default size
is **15**. With 31 patterns rotating through 15 slots, most calls miss and **recompile
the pattern from scratch** — every render, every line.

Proven, not inferred. Raising `Regex.CacheSize` to 64 and re-measuring the same paths:

| Path | Default cache (15) | Cache 64 | Reduction |
|---|---|---|---|
| Short reply | 86 KB | 8 KB | **91%** |
| Full document | 3 412 KB | 213 KB | **94%** |
| Streaming chunk | 1 782 KB | 107 KB | **94%** |

Two patterns are also interpolated per call
(`MarkdownRenderer.cs:52` and `:339`), so they build a new pattern string every time and
can *never* hit the cache regardless of its size.

**Why this matters most for streaming.** Memoisation hides the cost for static content,
but every chunk of an assistant reply carries a new source, so a 100-chunk answer
allocates on the order of 170 MB — on the server, per connected user, under Blazor
Server.

**Fix:** roadmap item 19 (`[GeneratedRegex]`) is exactly the right remedy, and for a
sharper reason than originally recorded: each pattern becomes a compiled static
instance, so there is no cache lookup and no recompilation — the 94% without touching
process-global state. Bumping `Regex.CacheSize` gets the same numbers but is a poor fit
for a library, since it silently changes a setting the host application owns. The two
interpolated patterns need handling on their own either way.

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
