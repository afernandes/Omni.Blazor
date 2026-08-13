# Changelog

All notable changes to **Omni.Blazor** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The version is derived from the latest `vX.Y.Z` git tag by [MinVer](https://github.com/adamralph/minver).

## [Unreleased]

### Fixed
- `ThemeService.InitializeAsync` now reads `prefers-color-scheme` itself when the `<html>` element carries no `data-accent`/`data-theme` — the signal that the `OmniTheme` bootstrap never ran. Hosts that cannot use the component in `<head>` (a static `index.html` under MAUI or Photino) always started in light mode on a dark OS, because the empty-storage branch only mirrored back what the bootstrap had applied. Keeping `OmniTheme` in `<head>` is still the way to avoid the flash: the fallback lands one paint late.

## [0.7.0] - 2026-08-10

### Added
- `OmniVirtualKeyboard`: on-screen keyboard for touch terminals (self-checkout, kiosk, PDV). Binds its own value like any `FormComponent<string>`, so it needs no reference to another input. Standard/Numeric/Email layouts plus fully custom ones through `VirtualKeyboardLayout`, one-shot Shift, a Symbols mode, `MaxLength`, `OnKeyPress`/`OnEnter`, and keys that report their pressed state to assistive tech.
- Native AOT and trimming compatibility contract (`IsAotCompatible`), analyzer-clean builds and a published native smoke consumer exercised by CI.
- Scoped lazy ECMAScript-module interop split by feature domain, with per-module caching, deterministic cancellation, in-flight call draining, contract validation and disposal coverage.
- Immutable composition through `Include`, `Extend`, targeted overrides and selective clearing across the main Fluent Schemas.
- Headless `OmniEntityEditor<TItem,TKey>` CRUD coordinator and reusable Scheduler, Kanban and Gantt adapters.
- `OmniPropertyGrid<TModel>` and `OmniWorkflowDesigner<TNode>` with typed inspection, validation and bounded history.
- `StackedColumn`, `StackedBar`, `Scatter`, `Bubble`, `Radar` and `Gauge` charts, including typed `ChartSchema` support.
- Dedicated showcases and coverage for concurrency, cancellation, disposal, composition and P2 rendering behavior.
- Immutable Fluent schemas for `OmniDataGrid`, `OmniGantt`, `OmniScheduler`, `OmniKanban`, `OmniChart` and `OmniDiagramCanvas`, with expression-based model projection, one-shot builders, validation and showcase coverage. `DataGridSchema<TItem>` is shared by standalone grids, `OmniDataGridForm` and `OmniEntityPicker`.
- `OmniDataFilter<TItem>` typed query architecture: immutable `DataFilterSchema<TItem>` fields declared with expressions, versioned `DataFilterQuery<TItem>` snapshots, nested AND/OR and BETWEEN builders, bounded source-generated JSON serialization, custom value codecs, allow-listed deserialization, local predicates and `IQueryable` expression translation.
- `OmniDataGridForm<TItem,TKey>`: typed CRUD composition of `OmniDataGrid` and `OmniDataForm` with immutable fluent schema, copy-safe drafts, local or cancellable provider persistence, confirmed delete, custom actions, operation events and dialog/drawer/inline editors. Row and bulk actions now support priority-based automatic overflow, anchored accessible menus, grouping/shortcut/description metadata, named authorization policies and a resizable/frozen actions column.
- `DataGridViewState`: controlled or local-storage-backed DataGrid preferences for column order, width, visibility, frozen edges, sorting, text filters, grouping and search; business data, selection and expanded rows are intentionally excluded.
- `OmniEntityPicker<TItem,TKey>`: form-bound local or server-side entity lookup backed by `OmniDataGrid`, stable-key binding, cancellable key resolution and dialog/drawer presentations.
- `OmniDataFormWizard<TModel>`: immutable multi-step DataForm composition over one shared `EditContext`, conditional steps, per-step validation and cancellable async rules.
- `OmniDataImport<TItem>`: bounded incremental CSV/TSV parsing, typed fluent mappings, header aliases, remapping UI, validated preview, partial-import policy and cancellable persistence snapshots.
- `Collection(...).Grid(schema)`: renders bounded DataForm collections through the same DataGridForm schema while preserving the parent `EditContext`, indexed validation and reorder/min/max rules.
- DataGridForm row and operation policies (`VisibleWhen` / `DisabledWhen`), typed mutation results for validation/conflict/not-found/forbidden outcomes, guarded unsaved changes and bounded typed bulk actions.
- Generated manifest and MCP discovery for fluent DataForm, DataGridForm, wizard and DataImport schemas/builders/providers.
- `OmniDataGrid.RefreshAsync()`: reapplies filter/sort/grouping on demand. The in-memory shaping pipeline is now memoized by a state stamp (data reference + count, search, sorts, filters, groups, page); a parent re-render with nothing changed no longer re-sorts the whole set — measured at 318 ms per click with 1M sorted items before. `RefreshAsync` is the valve for items mutated in place (same collection, same count), which the stamp cannot see.
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
- Package API compatibility validation against version `0.5.0`.
- Cancellation-aware, latest-wins async validation overloads for forms and form components.
- `OmniSignaturePad` form input with undo, clear and PNG/JPEG/SVG export.
- `OmniGlobalSearch` with local/remote sources, keyboard navigation and latest-wins cancellation.
- `OmniFileManager` with a storage-provider contract, bounded listings and guarded file operations.
- `OmniTreeGrid` with declarative columns, accessible hierarchy, cancellable lazy loading and bounded LRU cache.
- Shared `OmniItemsProvider<TItem>` contract with bounded pagination and cancellation for AutoComplete, Select and MultiSelect.
- `OmniDataForm<TModel>` schema composition, typed conventions/profiles, dependent lookups, bounded editable collections, responsive groups, diagnostics and observable validation state.
- Chromium browser coverage for DataForm focus, keyboard/ARIA behavior, container queries, accessibility and rapid model replacement.
- Chromium browser coverage for advanced data entry now runs against both Interactive Server and WebAssembly hosts.
- Clean-package smoke tests that compile Blazor Server/WASM consumers and execute the installed MCP tool.

### Changed
- Migrated the library-owned JavaScript surface from `window.omniBlazor` and manually included scripts to private ECMAScript modules loaded independently for core, scrolling, responsive behavior, overlays, inputs, navigation, speech, data components and display features.
- Replaced the central JavaScript identifier-to-module resolver with feature-scoped DI contracts. Each module now owns one import and lifetime, while components declare only the core, scroll, responsive, overlay, input, navigation, speech, data, display or diagram capability they consume.
- Made typed schemas mandatory for Scheduler and Gantt projection, explicit factories mandatory for DataImport, and typed property expressions mandatory for field-level DataAnnotations validation.
- Replaced reflection-based form snapshot cloning with explicit typed snapshot/restoration hooks and an allocation-light shallow property snapshot by default.
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
- Rebuilt the `OmniDataGrid` drag-to-group gesture on pointer events. The grip used HTML5 Drag and Drop (`draggable` + `dragstart`/`dragover`/`drop`), and inside WebView2 hosts (.NET MAUI and Photino on Windows) the native drag is routed through the host's OLE drag loop, so the page never received the `drop` — dragging a header onto the group panel silently did nothing. The gesture is now browser-owned JS (`gridStartGroupDrag`), the same shape as the column-resize gesture: pointer events on the grip, document-level listeners, a ghost chip following the cursor, panel highlight while armed/over, and a single `.NET` callback when the pointer is released over the panel — no per-move Blazor traffic, which matters on Server, and touch works too (the grip opts out of scrolling via `touch-action: none`). Verified end to end in Chrome and in WebView2 (Photino on Windows, the same runtime the MAUI `BlazorWebView` uses).
- Emitted `aria-expanded` and `aria-selected` as explicit `"true"`/`"false"` strings across every component that bound them to a `bool`. Blazor omits an attribute whose value is a `false` bool, and for these two states absence is not the same as `false`: a missing `aria-expanded` reads as "this control opens nothing" and a missing `aria-selected` as "this row is not selectable", so the widget role was lost for exactly as long as the control stayed collapsed or unselected. Covers `OmniDatePicker` (input + trigger), `OmniDateRangePicker`, `OmniEntityPicker`, `OmniGlobalSearch` (combobox + options), `OmniDataGrid` (master-detail expanders in both flat and hierarchy mode, hierarchy chevron), `OmniTreeGrid` (rows + toggle) and `OmniFileManager` rows. `aria-busy`, `aria-disabled` and `aria-invalid` are left as bools on purpose — their implicit default *is* `false`.
- Gave the `OmniPassword` visibility toggle an accessible name. The eye button rendered an icon with no `aria-label`, no `title` and no text, so it was the one control in the library that screen readers announced as an unlabelled button — its siblings (`OmniTextBox`, `OmniSelect`, `OmniDatePicker`, `OmniAutoComplete`) already labelled their clear buttons. It now carries `aria-label`/`title` from the new `OmniTexts.ShowPassword` / `OmniTexts.HidePassword` keys, plus `aria-pressed` for the revealed state.
- Promoted `OmniDatePicker` and `OmniDateRangePicker` panels to the browser top layer with viewport-aware positioning, so calendars are no longer clipped by scrollable dialog, drawer or form containers.
- Resolved initial typed DataForm lookup values to human-readable items before opening paged lists, without temporarily exposing persisted identifiers.
- Styled the paged load-more action shared by `OmniSelect`, `OmniMultiSelect` and `OmniAutoComplete`, including hover, keyboard focus and disabled states.
- Preserved the standard input height of an empty `OmniSelect` trigger when neither a value nor placeholder is present.
- Kept conventional and annotated identifiers out of metadata-generated DataForms unless explicitly declared, and replaced the Gantt Form parent-id editor with a typed, paged task-title selector that prevents hierarchy cycles.
- Reprojected Scheduler and Gantt data after in-place CRUD list mutations, and deferred shared click-outside notifications until the triggering click completes so WebAssembly cannot remove a Save button before its handler runs.
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
- Removed pre-v1 string/reflection projection APIs from Scheduler, Gantt, Pivot, Tree and DataFilter, the open `Type`-based DataForm editor registration overload, public `IJSRuntime` service constructors and `OmniForm.SnapshotOptions`.
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

[Unreleased]: https://github.com/afernandes/Omni.Blazor/compare/v0.7.0...HEAD
[0.7.0]: https://github.com/afernandes/Omni.Blazor/compare/v0.6.1...v0.7.0
