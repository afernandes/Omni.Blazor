# Component selection

Use this guide to narrow candidates, then query the version-matched catalog for the
exact contract. These are semantic boundaries, not substitutes for API documentation.

## Forms and editing

- Use `OmniForm` when the screen owns explicit Razor fields and needs form submission
  and validation orchestration.
- Use `OmniDataForm` when a reusable strongly typed schema should generate fields.
- Use `OmniDataFormWizard` when one model and validation context span ordered steps.
- Use `OmniPropertyGrid` for a compact metadata-driven property inspector.
- Use `OmniDataGridForm` for coordinated CRUD with generated grid, detached edit
  drafts, validation, and a cancellable provider. Use a plain `OmniDataGrid` when the
  application owns editing and persistence orchestration itself.

For generated forms and CRUD, inspect both the component and its schema builder and
provider contracts. Keep authorization and persistence in the application layer.

## Collections and data-heavy views

- `OmniDataGrid`: tabular sorting, filtering, grouping, selection, editing,
  virtualization, or optional hierarchy.
- `OmniTreeGrid`: accessibility-focused hierarchical tables with bounded lazy load.
- `OmniTree`: navigation or selection hierarchy without tabular columns.
- `OmniKanban`, `OmniScheduler`, `OmniGantt`, `OmniPivotGrid`: choose only when the
  domain is truly board-, calendar-, timeline-, or pivot-shaped.
- `OmniVirtualize` plus `OmniPagination`: custom list/card surfaces when a grid would
  impose the wrong semantics.
- `OmniFileManager` and `OmniDataImport`: provider-backed file operations and bounded,
  typed delimited imports; do not re-create these as generic grids plus ad hoc logic.

## Choice inputs

- `OmniSelect`: bounded single-choice options.
- `OmniAutoComplete`: searchable single selection with local or cancellable remote
  suggestions.
- `OmniEntityPicker`: richer entity lookup through a grid while binding a stable key.
- `OmniMultiSelect`: multiple known values; `OmniTagInput`: user-authored tag values.
- `OmniCheckBoxList`, `OmniRadioGroup`, and `OmniSegmentedControl`: visible small sets
  where immediate comparison is more important than compactness.

Always verify value/item generic types, equality/key semantics, and local-versus-remote
loading APIs in the catalog.

## Shell, navigation, and actions

- Compose the application frame with `OmniLayout`, `OmniAppBar`, `OmniDrawer`,
  `OmniMain`, and the panel-menu family.
- Use `OmniGlobalSearch` as an embedded search surface and `OmniCommandPalette` for a
  keyboard-driven global action menu.
- Use `OmniToolBar` for persistent contextual actions, menus for grouped choices, and
  FAB components only for a small set of prominent contextual actions.

## Overlays and feedback

- Place `OmniOverlayHosts` once in the root layout when service-driven dialogs,
  notifications, tooltips, or context menus are used.
- Use `OmniPopover` for interactive anchored content and `OmniBottomSheet` for a
  bottom-edge task surface. Use `OmniOverlay` only when the standalone backdrop/surface
  primitive is the correct contract.
- Use `OmniConfirmPrompt` for destructive actions that intentionally require typed
  confirmation and `OmniExitPrompt` for guarded navigation with unsaved work.
- `OmniAlert` is persistent inline feedback; notification services are transient.
- `OmniSkeleton` represents expected content shape during load; `OmniSpinner` represents
  indeterminate activity. `OmniEmptyState` covers zero-data/no-results/first-run, while
  `OmniResult` covers whole-panel or whole-page outcomes such as 403/404/500/success.

## Visual data

Use `OmniStat`/`OmniStatGroup` for headline measures, `OmniSparkline` for compact trend,
and `OmniChart` for analytical comparison. Inspect chart schema APIs before writing
series configuration, and preserve text equivalents for essential information.
