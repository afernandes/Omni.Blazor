# Engineering quality standard

This is the quality bar for Omni.Blazor runtime code. It applies to components,
services, JavaScript bridges and optional packages. Performance changes must
preserve correctness, accessibility and API clarity.

## Component structure

- Keep small components in a single `.razor` file.
- Split complex components into `.razor` and partial `.razor.cs` files. Markup,
  directives and Razor-specific fragments stay in `.razor`; lifecycle, state
  machines, async orchestration and independently testable logic belong in
  `.razor.cs`.
- Use `.razor.css` for styles that are private implementation details of one
  component. Shared variants, design tokens and styles used by overlays or
  dynamically created DOM stay in the theme bundle.
- Decompose large algorithms into internal classes before adding more state to
  a component. A component should coordinate work, not become the only place
  where that work can be tested.

## Async and concurrency

- Async stays async end-to-end. `.Result`, `.Wait()` and
  `GetAwaiter().GetResult()` are forbidden in runtime code.
- Public long-running operations accept `CancellationToken` when cancellation
  can stop meaningful work.
- Search, validation, navigation and data refresh use latest-wins semantics.
  A stale completion must never overwrite newer UI state.
- Avoid `async void` except where a framework event signature requires it.
  Event bridges must route work through an exception-observing `Task`.
- Never `await` while holding `lock`, `ReaderWriterLockSlim` or another
  synchronous critical section. Use short critical sections or an async-aware
  primitive and always release it in `finally`.
- Shared mutable state must have one documented owner or use `lock`,
  `Interlocked`, `Volatile`, channels or immutable snapshots as appropriate.
  Lock-free algorithms require dedicated tests and a written justification.
- Bound parallelism and queues. Avoid unbounded fan-out, thundering-herd
  wakeups, unfair global locks, convoy-prone critical sections and operations
  that can starve unrelated component work.
- Reentrant callbacks may dispose or mutate the component. Re-check
  cancellation, disposal and operation version after every awaited consumer
  callback before committing state.

These rules cover data races, torn reads/writes, memory ordering, ABA hazards,
deadlocks, livelocks, starvation, priority inversion, convoy problems,
reentrancy, dining-philosophers lock graphs and exception safety. Most Blazor
component state is serialized by the renderer synchronization context, but
services, timers, JS callbacks and consumer-provided tasks must not assume that
all continuations run on one thread.

## Lifetime and resources

- Pair every subscription with deterministic unsubscription.
- Dispose timers, cancellation sources, streams, pooled buffers, JS module
  references, `DotNetObjectReference` instances, observers and registration
  handles.
- Disposal is idempotent. Cancel in-flight work before releasing dependencies,
  and make late callbacks harmless.
- Fire-and-forget work must catch and route exceptions to
  `DispatchExceptionAsync`, logging or another explicit observer. Do not allow
  unobserved task exceptions. Component work uses `ObserveTask`; services and
  disposal paths use `TaskObserver`. The async-safety gate rejects direct task
  discards.
- Avoid closures that capture a component or large object graph in long-lived
  delegates. Prefer named handlers that can be unsubscribed.
- Caches must be bounded, scoped or evict entries. Document ownership of cached
  disposable values.
- Do not add finalizers to managed-only types. Prefer `IDisposable` /
  `IAsyncDisposable` so resources do not accumulate in the finalizer queue.

## Allocation and throughput

- Measure with a representative benchmark, allocation profiler or trace before
  and after optimization. Keep the evidence with the change.
- Avoid repeated LINQ materialization and delegate/closure creation inside hot
  render loops. Cache stable derived state through `ParameterState<T>`.
- Use `Span<T>` / `ReadOnlySpan<T>` only for synchronous, non-escaping parsing
  or formatting paths. They cannot cross `await`, iterator or component-field
  boundaries.
- Use `Memory<T>` when data must cross async boundaries without immediate
  copying. State who owns the underlying memory and for how long.
- Use `ArrayPool<T>` only when profiles show material buffer pressure. Return
  buffers in `finally`, clear reference-containing buffers, and never expose a
  returned buffer.
- Prefer streaming, pagination and bounded batches over collecting large
  payloads. Avoid large temporary strings and arrays that land on the LOH.
- Pooling is not automatically faster. Do not pool small objects or retain
  large buffers indefinitely, which can worsen memory retention and LOH
  fragmentation.
- Consider false sharing only in measured, genuinely parallel low-level code;
  ordinary renderer-bound component fields do not benefit from padding.

## Verification required by risk

- Async changes: tests for cancellation, stale completion, exception
  propagation, reentrancy and disposal during work.
- Subscriptions and JS handles: mount/unmount tests and idempotent disposal.
- Caches: capacity and eviction tests.
- Shared services: parallel stress tests with deterministic assertions.
- Allocation-sensitive algorithms: BenchmarkDotNet or equivalent allocation
  evidence.
- Browser lifecycle and focus: Playwright coverage in both interactive server
  and WebAssembly hosts where behavior differs.

No review can prove the global absence of every concurrency or memory failure.
The standard therefore requires explicit ownership, bounded work, deterministic
cleanup, focused tests and measurement for every affected path.
