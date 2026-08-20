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

| Benchmark | Mean | Allocated | Before `[GeneratedRegex]` |
|---|---|---|---|
| `ShortReply` | 5.9 µs | **6.9 KB** | 39.6 µs / 84 KB |
| `FullDocument` (12 sections) | 176 µs | **217 KB** | 2.34 ms / 3.35 MB |
| `StreamingChunk` (half a document) | 87 µs | **109 KB** | 987 µs / 1.67 MB |
| `SanitizeHostileHtml` | 4.5 µs | **6.3 KB** | 7.9 µs / 6.3 KB |

**How this suite paid for itself** — the "before" column above was the state when these
benchmarks were first written, and it is why roadmap item 19 got done.

Rendering one 75-byte sentence used to allocate 84 KB: ~1100× the input. That was never
parsing cost — it was the .NET static regex cache thrashing. `MarkdownRenderer` ran ~29
distinct patterns through the *static* `Regex.Match` / `IsMatch` / `Replace` overloads,
which share a process-wide cache whose default size is **15**. With more patterns than
slots, most calls missed and **recompiled the pattern from scratch**, every render and
every line. Two more patterns were interpolated per call, so they built a new pattern
string each time and could never hit the cache at any size.

Diagnosed by raising `Regex.CacheSize` to 64 and re-measuring: allocation fell 91–94%,
which identified the cache rather than the parser as the cost.

The fix was `[GeneratedRegex]`, which gets there without touching `Regex.CacheSize` — a
process-global setting that belongs to the host application, not to a library. Each
pattern is now a compiled static instance: no cache lookup, nothing to recompile. The
one genuinely dynamic pattern (the closing fence, which depends on the opening fence's
character and length) became a hand-written scan, since it was also the worst case for
the old cache.

Time improved more than predicted — the estimate was about allocation, but removing
per-call recompilation made the document path **13× faster** as well.

Two things worth keeping in mind, both visible above:
- `SanitizeHostileHtml` allocation did not move (6.3 KB either way). It uses few enough
  patterns that they fit the old cache; its gain is time only. A useful reminder that
  the win came from cache misses, not from regex being "slow".
- 217 KB per document render is still not free. `OmniMarkdown` memoises, so static
  content pays it once — but streaming pays per chunk. That is now proportionate rather
  than pathological.

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
