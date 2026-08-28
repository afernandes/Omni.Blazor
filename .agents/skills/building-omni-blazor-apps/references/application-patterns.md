# Application patterns

These patterns guide composition. Query every component's exact versioned API before
writing parameters or schema setup.

## Minimal application setup

Register services once in `Program.cs`:

```csharp
builder.Services.AddOmniComponents();
```

Render one theme component in the document head or the host location appropriate to the
application:

```razor
<OmniTheme Accent="amber" />
```

No JavaScript `<script>` tag is required. Runtime theme, accent, and density are exposed
through the library's documented `data-*` attributes and design tokens. Preserve an
existing setup instead of adding a second registration or theme instance.

## Application shell

Start from `OmniLayout` with `OmniAppBar`, `OmniDrawer`, `OmniMain`, and panel navigation.
Keep route definitions and authorization policies in the consuming application. Check
small and wide viewports, keyboard focus, drawer dismissal, active navigation state,
and the layout's actual Blazor render mode.

The repository template gallery includes a copy-paste app shell. It is not a NuGet
package or `dotnet new` template. When the source repository is available, inspect
`src/Omni.Templates/README.md` and copy only the relevant starter markup.

## CRUD screen

Choose one ownership model:

- Application-owned: `OmniDataGrid` plus explicit dialog/form composition. Prefer this
  when the workflow has domain-specific commands, unusual editing, or multiple
  persistence boundaries.
- Schema/provider-owned UI: `OmniDataGridForm` with its typed schema and
  `IDataGridFormProvider`. Prefer this for conventional create/read/update/delete flows
  with reusable metadata and expected validation/concurrency outcomes.

In either model, bind view state deliberately, handle loading/empty/error states, make
destructive actions explicit, and keep authorization on the server/application layer.

## Dashboard

Use layout primitives for reading order, then stat components for headline measures and
charts only where a graphical comparison adds value. Provide loading placeholders that
match the final geometry, meaningful empty states, and a text/table alternative for
information that cannot be lost to color or shape.

## Search and lookup

Use bounded local data only for genuinely small stable sets. For remote search, use the
component's cancellable contract, debounce where exposed, and ensure latest-results-win.
Preserve stable keys for entity selection; do not bind a display label as identity.

## Authentication, account, and status pages

The copy-paste gallery contains authentication, account, admin, dashboard, empty-state,
and HTTP/error starters. Treat their data and actions as illustrative: connect real
navigation, validation, authentication, localization, and backend behavior in the
consumer. Reuse the markup and shipped Omni CSS classes without importing gallery-only
styles.

## Validation checklist

- Build the consuming project with the restored package version used for catalog lookup.
- Exercise validation, cancellation, empty/loading/error paths, and re-entry where
  applicable.
- For Server and WebAssembly, validate the hosting mode that the user ships. Test both
  when shared code relies on timing-sensitive browser events or hydration.
- Use real browser keyboard/pointer events for overlay, focus, hotkey, drag/drop,
  responsive, and JavaScript-backed behavior.
- Check accessible names, labels, validation association, focus order, contrast, and
  reduced-motion behavior appropriate to the change.
- State any unperformed browser, device, accessibility, backend, or production checks.
