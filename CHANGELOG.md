# Changelog

All notable changes to **Omni.Blazor** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The version is derived from the latest `vX.Y.Z` git tag by [MinVer](https://github.com/adamralph/minver).

## [Unreleased]

### Added
- `OmniDataGrid.MaxGroups` (default 10 000) caps how many group nodes one grouping pass builds, with an on-screen notice when the cap is hit — the twin of `MaxVisibleRows` for hierarchies. Grouping by a near-unique column (an id, a timestamp with seconds) used to build one group per row, so the group tree cost more than the data it described. `GroupLimitReachedText` customizes the notice and `GroupLimitReached` exposes the state.
- `OmniDataGrid.AutoCollapseGroupsThreshold` (default 100): grouping starts collapsed when the first-level group count goes above it. Changing the grouping cleared the collapsed set, so the first render after dragging a column was always the whole set expanded. Set to `0` to disable.
- `OmniDataGrid.VisibleGroupRowCount`: number of currently flattened group rows (headers + rows of open groups + footers).
- `OmniDataGridColumn.GroupHierarchy`: hierarchical date grouping. One drag on a date column expands into Year › Month › Day (or any sequence of `DateGroupInterval`), with ready-made hierarchies in `DateGroupHierarchy`. Each level gets its own chip in the group panel and can be removed on its own — dropping "Day" leaves Year › Month standing. Group labels shorten under their parent level ("julho", not "julho 2026") and go back to the full form when that parent is removed.
- Initial public release scaffolding (`Directory.Build.props`, `Directory.Packages.props`, MinVer, Source Link, central package management).
- GitHub Actions CI + release pipeline targeting NuGet.org.
- Governance docs: `LICENSE`, `CHANGELOG`, `CONTRIBUTING`, `CODE_OF_CONDUCT`, `SECURITY`, issue & PR templates, Dependabot.
- Package icon (`assets/icon.png`).
- Engineering quality standard for async, concurrency, lifetime and allocation-sensitive code.
- CI gates for vulnerable NuGet dependencies and unsafe async patterns.
- Package API compatibility validation against version `0.3.0`.
- Cancellation-aware, latest-wins async validation overloads for forms and form components.
- `OmniSignaturePad` form input with undo, clear and PNG/JPEG/SVG export.
- `OmniGlobalSearch` with local/remote sources, keyboard navigation and latest-wins cancellation.
- `OmniFileManager` with a storage-provider contract, bounded listings and guarded file operations.
- `OmniTreeGrid` with declarative columns, accessible hierarchy, cancellable lazy loading and bounded LRU cache.
- Shared `OmniItemsProvider<TItem>` contract with bounded pagination and cancellation for AutoComplete, Select and MultiSelect.
- `OmniDataForm<TModel>` schema composition, typed conventions/profiles, dependent lookups, bounded editable collections, responsive groups, diagnostics and observable validation state.
- Chromium browser coverage for DataForm focus, keyboard/ARIA behavior, container queries, accessibility and rapid model replacement.
- Clean-package smoke tests that compile Blazor Server/WASM consumers and execute the installed MCP tool.

### Changed
- `OmniDataGrid` in-memory grouping now runs over the whole filtered set instead of the current page, and paging slices **first-level groups** rather than rows. Previously a group could be split across pages, each page showing a partial count for the same key. The pager labels the unit ("… de 37 grupos") while grouped. Server-side (`DataProvider`) is unchanged: only the returned window is available, so grouping still applies to it.
- Renamed library and namespace `Totvs.Blazor → Omni.Blazor` (project, components, CSS classes, JS namespace, design tokens).
- Replaced Bootstrap (full bundle, ~150 KB pre-gzip) with a minimal forked reset in `Themes/_reset.scss`. Compiled CSS now **~295 KB** (down from 438 KB, **-33 %**).
- Solution file `ClaudeBlazor.slnx → Omni.Blazor.slnx`.
- Pinned the stable .NET 10 SDK and refreshed ASP.NET Core, AI, bUnit and build dependencies.
- Expanded the generated catalog and MCP manifest from 174 to 198 public components.
- Allowed complex components to split markup and orchestration into `.razor` + `.razor.cs`.
- Unified `OmniDataGrid` and `OmniTreeGrid` hierarchy handling behind one cancellable, bounded state engine with latest-wins publication, per-node request deduplication, cycle protection, deterministic disposal and accessible tree-grid semantics.
- Standardized the hierarchy API of both grids on `KeySelector`, `Children`, `HasChildren`, `ChildrenProvider`, controlled `ExpandedKeys` and explicit cache/concurrency/visibility limits.
- Replaced DataGrid and AutoComplete `LoadData` callbacks with cancellation-aware `DataProvider` / `ItemsProvider` contracts.
- Replaced DataForm `Fields` and raw `EditorParameters` configuration with immutable `DataFormSchema<TModel>` and strongly typed fluent builders.
- Changed DataGrid CSV export to stream through bounded provider batches with a configurable hard row cap.
- Routed deliberately detached tasks through a central fault observer and made the async-safety gate reject raw discards.
- Made the release workflow run vulnerability, test, manifest, async and real-package smoke gates before publishing.

### Fixed
- `OmniDataGrid` grouped mode now honours `Virtualize`. The grouped `<tbody>` was a recursive walk over the group tree that never consulted `Virtualize`, so every row of every expanded group was mounted in one batch while the flat mode next to it virtualized normally. The tree is now flattened into a linear row list (header / data / footer) before rendering, and that single list feeds both branches — virtualized and not. Measured with 20 000 rows in 4 groups: 20 004 `<tr>` before, 110 after, and the count no longer grows with the row count. Group headers and footers are pinned to `RowHeight` while virtualizing (via `--omni-grid-row-h`) so the scrollbar doesn't drift with the header-to-row ratio; paged mode keeps its natural heights.
- Prevented stale async validation, overlapping server loads and timer callbacks from overwriting newer state.
- Made notification, tour, carousel, chat and AI conversation cleanup idempotent and cancellation-aware.
- Prevented reflection caches from permanently rooting dynamic types or invalid property names.
- Preserved the consumer-provided DOM `id` as the component identity used by JS interop.
- Restored binary compatibility for `AddOmniComponents`, Gantt generic types and `OmniChatClient` constructors.
- Excluded nested local build artifacts from the base NuGet package.
- Serialized and versioned MenuBar listener registration so rapid state changes cannot reorder global JS handlers.
- Made DataForm field validation return explicit valid/invalid/cancelled/superseded outcomes, target real focusable editors and deterministically release validation, provider and nested-collection state.

### Removed
- Bootstrap SCSS source tree (`src/Omni.Blazor/BootstrapSrc/`) and `Themes/_bootstrap-override.scss`.
- Removed the former `OmniDataGrid.ChildrenSelector` and non-cancellable `OmniDataGrid.LoadChildren` parameters. Use `Children` and the cancellation-aware `ChildrenProvider` instead.
- Removed `OmniTreeGrid.LoadChildren` and `TreeGridChildrenProvider<TItem>`. Both grids now use `HierarchyChildrenProvider<TItem>` through `ChildrenProvider`.
- Removed non-cancellable `OmniDataGrid.LoadData`, non-cancellable `OmniAutoComplete.LoadData` and the unused `OmniSelect.ChildContent` slot.

## Release format

Each release section follows this shape:

```
## [1.2.0] - 2026-MM-DD

### Added
- New `OmniXxx` component …

### Changed
- Tightened `OmniDataGrid` keyboard navigation …

### Deprecated
- `OmniLegacyThing` will be removed in 2.0; use `OmniNewThing`.

### Removed
- Dropped support for `oldParam` on `OmniFoo`.

### Fixed
- `OmniDatePicker` no longer crashes on …

### Security
- Bumped transitive `Xyz` to patch CVE-XXXX-YYYY.
```

[Unreleased]: https://github.com/afernandes/Omni.Blazor/compare/HEAD
