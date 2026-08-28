---
name: building-omni-blazor-apps
description: Selects and composes Omni.Blazor components for Blazor application UI, including forms, CRUD screens, dashboards, navigation, overlays, and migrations. Use when generating or reviewing an app that consumes AndersonN.Omni.Blazor. Do not use for contributing new components to the Omni.Blazor library itself.
license: MIT
metadata:
  author: afernandes
---

# Build applications with Omni.Blazor

Use Omni.Blazor as the application's UI vocabulary while preserving the consumer
project's architecture, language, render mode, and product conventions.

## Required workflow

1. Resolve the consumer's actual `AndersonN.Omni.Blazor` version. Check direct and
   centrally managed package references; when necessary, inspect restored assets to
   resolve variables, ranges, or floating versions.
2. Select a catalog that matches that version. Follow
   [catalog and versioning](references/catalog-and-versioning.md). Do not use an old
   global MCP catalog for a newer package or `main` documentation for an older package.
3. Search by user intent, then inspect each chosen component's exact API. If a
   component uses a typed schema, builder, or provider, inspect the corresponding
   configuration API as well. Never infer a parameter, event, slot, enum member,
   service method, or CSS class from its name.
4. Read [component selection](references/component-selection.md) when alternatives
   overlap. Read [application patterns](references/application-patterns.md) when
   composing a complete screen or flow.
5. Implement the smallest coherent UI that satisfies the request. Preserve existing
   application structure and keep business rules, authorization, persistence, and
   orchestration outside Razor presentation code.
6. Validate at the level affected by the change. Build the consuming project. For
   overlays, focus, keyboard behavior, JavaScript interop, responsive layout, or
   render-mode-sensitive behavior, exercise the rendered application in a browser.

## Catalog rules

- Prefer the `omni-blazor` MCP tools when their catalog version matches the resolved
  package: call `get_catalog_info`, then `search_components` and `get_component`.
- Use `search_configuration_apis` and `get_configuration_api` for schema-driven forms,
  grids, filters, charts, schedulers, Kanban, Gantt, diagrams, or imports.
- In a source checkout, `llms.txt` is the discovery index and
  `docs/components.json` / `llms-full.txt` are the public contract. Do not browse
  component source merely to discover its API.
- Treat source inspection as implementation diagnosis, not as permission to use
  internal members.
- `OmniAiConversation` belongs to the optional `AndersonN.Omni.Blazor.Ai` package;
  confirm that dependency before using it.

## Composition rules

- Add `builder.Services.AddOmniComponents()` and one `OmniTheme` only when the app has
  not already configured them. No consumer JavaScript `<script>` tag is required.
- Prefer Omni components, layout primitives, design tokens, and shipped template
  classes before adding replacement controls or parallel styling systems.
- Keep server searches, refreshes, and persistence cancellable where the selected
  component contract supports cancellation; guard stale results in interactive UIs.
- Preserve accessible names, labels, validation messages, keyboard operation, focus
  restoration, empty/loading/error states, and responsive behavior.
- Do not silently add another component library for a gap. Explain the gap and use a
  small native Blazor/HTML composition when the catalog has no appropriate primitive.

## Completion evidence

Report which package/catalog version was used, which components were selected, and
what was actually validated. Distinguish a successful build or component test from a
real browser, device, accessibility, or production validation.

For changes inside the Omni.Blazor library repository, stop using this skill's
consumer workflow and follow the repository `AGENTS.md` contribution checklist.
