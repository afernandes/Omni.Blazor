# AGENTS.md — Omni.Blazor

Entry point for AI coding agents (Claude Code, Cursor, Copilot, v0, …) — whether
you are **generating UI with** this library or **contributing to** it. Humans:
see [CLAUDE.md](CLAUDE.md) (architecture deep-dive) and
[CONTRIBUTING.md](CONTRIBUTING.md).

## What this is

**Omni.Blazor** — a packable Razor Class Library for **.NET 10**. 206 components (authoritative count + full API: [`docs/components.json`](docs/components.json))
across Buttons, Data, Display, Forms, Inputs, Layout, Marketing, Navigation and
Overlay. One SCSS bundle, isolated feature ES modules, all theming via CSS custom properties.

| | |
|---|---|
| NuGet package id | `AndersonN.Omni.Blazor` |
| Namespace / assembly | `Omni.Blazor` |
| Static assets | `_content/Omni.Blazor/css/omni.css`, lazily imported JS modules |
| CSS class prefix | `omni-` |
| Design-token prefix | `--omni-` |
| JS interop | scoped typed services + isolated ES modules |

## Machine-readable surface — read these first

Do **not** browse component source files to learn the API. Read the generated artifacts:

- **[`llms.txt`](llms.txt)** — curated index: every component with a one-line description and a source link, grouped by category. Start here.
- **[`llms-full.txt`](llms-full.txt)** — full dump: every component's parameters, events, slots, enum values and the theme tokens.
- **[`docs/components.json`](docs/components.json)** — the same data, structured (per component: `parameters`/`events`/`slots` with `type`, `enumValues`, `default`, `required`, `summary`). **Generated — never hand-edit.**

These three are produced by one reflection-based generator. **Regenerate after any
change to the public component surface** (new component, new/renamed `[Parameter]`,
new enum, edited XML doc):

```bash
dotnet run --project tools/Omni.Blazor.ManifestGen
pwsh ./tools/generate-localization-resources.ps1  # after changing OmniTexts
```

## MCP server (live tools for agents)

A stdio **MCP server** (`tools/Omni.Blazor.Mcp`) exposes the catalog as live tools —
`list_components`, `get_component`, `search_components` — over the embedded manifest
(self-contained, no library reference).

**Use it (external projects)** — install the .NET tool:

```bash
dotnet tool install -g AndersonN.Omni.Blazor.Mcp
```

Then register it. In **Claude Code**, use the CLI and pick the scope deliberately —
`user` makes the server available in every project you open, `project` writes it to a
committed `.mcp.json` so the whole team gets it, and the default `local` is just you in
just this repository:

```bash
claude mcp add omni-blazor --scope user -- omni-blazor-mcp
```

```bash
claude mcp list
```

Other clients (Cursor, Claude Desktop, Copilot) take the same command as JSON:

```json
{
  "mcpServers": {
    "omni-blazor": { "command": "omni-blazor-mcp" }
  }
}
```

> **Command not found?** GUI MCP clients (Cursor, Claude Desktop) may not inherit your
> shell `PATH`, so `~/.dotnet/tools` isn't on it. Use the absolute path instead:
> `~/.dotnet/tools/omni-blazor-mcp` (macOS/Linux) or
> `%USERPROFILE%\.dotnet\tools\omni-blazor-mcp.exe` (Windows).

**Already installed? Update, don't install.** The manifest is baked into the package, so
an old tool serves an old catalog. Use:

```bash
dotnet tool update -g AndersonN.Omni.Blazor.Mcp
```

> **`Access to the path ... is denied` on Windows?** That is a lock, not a permission
> problem: updating uninstalls the current version first, and every MCP client you have
> open is running `omni-blazor-mcp` as a child process holding those files. Close those
> clients (or stop the processes — `Get-Process omni-blazor-mcp | Stop-Process`) and run
> the update again. Each client respawns the server on its next start.

**Contributing to this repo** — it is already wired in [`.mcp.json`](.mcp.json) to run
straight from source (no install): `dotnet run --project tools/Omni.Blazor.Mcp -c Release`.

## Using the library (generating UI)

```razor
@* App.razor / _Host — <head> *@
<OmniTheme Accent="amber" />        @* injects _content/Omni.Blazor/css/omni.css *@
```
```csharp
// Program.cs
builder.Services.AddOmniComponents();   // registers every Omni service
```
No JavaScript `<script>` tag is required; browser helpers are imported lazily.
```html
<!-- runtime theming -->
<html data-theme="light|dark" data-accent="amber|crimson|emerald|blue|violet|teal|cyan|indigo|fuchsia|lime|orange|rose" data-density="compact|comfortable|spacious">
```

Pick components and parameters from `llms.txt` / `components.json`. Do not invent
parameters — if it is not in the manifest, it does not exist.

## Contributing a component — non-negotiable checklist

Template to copy: `src/Omni.Blazor/Components/Buttons/OmniButton.razor` (+ its test).

1. **Files**: keep small components in
   `src/Omni.Blazor/Components/<Category>/Omni<Name>.razor`. Complex components
   may use a partial `Omni<Name>.razor.cs` code-behind, following the Radzen
   pattern: markup and Razor-only concerns stay in `.razor`; state machines,
   lifecycle, async work and testable logic go in `.razor.cs`. Use
   `Omni<Name>.razor.css` only for truly component-scoped styles; shared tokens
   and reusable variants stay in the theme sources.
2. **Inherit** `OmniComponent` (or `OmniComponentWithChildren` for `ChildContent`, or `FormComponent<TValue>` for inputs).
3. **Root element splats all three:** `class="@RootCss" style="@Style" @attributes="Attributes"`.
4. **Compose classes only with `CssBuilder`** — `CssBuilder.Default("omni-x").AddClass(...).AddClass(Class).Build()`. The consumer's `Class` is **always appended last**. (`StyleBuilder` is the twin for inline styles.)
5. **Stable identity for dynamic lists:** use `@key="item.Id"` for mutable,
   stateful, editable, selectable or reordered lists. Static, immutable
   decoration loops may omit it when the identity-preservation cost has no
   benefit; document non-obvious exceptions.
6. **Inputs** inherit `FormComponent<TValue>`; write through `SetValueAsync` only — never re-implement two-way binding.
7. **Reactive recompute** goes through `ParameterState<T>` registered in `OnInitialized`, not raw `OnParametersSet`.
8. **JS via narrow DI capabilities** (`ScrollManager`, `HotkeyService`, `IOmniOverlayJsModule`, …) — never inject `IJSRuntime` directly and never add a central identifier-to-module resolver. Each ES module owns its import/lifetime through a feature-scoped interface registered in `Extensions/ServiceCollectionExtensions.cs`; components declare only the capabilities they consume.
9. **Styles / JS**: reusable styles belong in the theme sources; isolated
   implementation details may use `.razor.css`. JavaScript is an implementation
   detail behind typed DI services and colocated/lazy ES modules; never add a
   browser global or require consumers to add a `<script>` tag.
10. **Document every public `[Parameter]`** with a `/// <summary>` and give the component a leading `@* one-sentence description *@` — both feed the AI manifest.
11. **Test** at `test/Omni.Blazor.Tests/Components/<Category>/Omni<Name>Tests.cs` (base render + `Class`/`Style`/`Attributes` splat + behaviour) **and** a **showcase page** under `src/Forneria.Demo/Forneria.Demo.Pages/Pages/Showcase/<Category>/`.

Then regenerate the manifest (above), `dotnet format`, and `dotnet test`.

## Performance, concurrency and lifetime

Follow [`docs/engineering-quality.md`](docs/engineering-quality.md). In
particular:

- measure before introducing `Span<T>`, pooling, caching or lock-free code;
- never use sync-over-async (`.Result`, `.Wait()`, `GetAwaiter().GetResult()`);
- async refresh/search/validation operations are cancellable and latest-wins;
- every event, timer, observer, `DotNetObjectReference`, JS handle,
  `CancellationTokenSource` and owned stream is released deterministically;
- fire-and-forget work must observe and route exceptions;
- shared mutable state needs an explicit synchronization and ownership model;
- caches are bounded or have an eviction/lifetime policy;
- add race, cancellation, reentrancy and disposal tests for affected code.

## Build / test / format

```bash
dotnet build src/Omni.Blazor/Omni.Blazor.csproj                 # library only (fast, no exe lock)
dotnet test  test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj    # 2,000+ bUnit tests
dotnet test  test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj --filter "FullyQualifiedName~Omni<Name>Tests"
dotnet format
```

**Lock pitfall:** while a host (Forneria.Demo / FoodService / Omni.Templates.Host)
is running, building the exe fails (`Omni.Blazor.dll` is locked). Build only
`src/Omni.Blazor/Omni.Blazor.csproj`, or stop the host first.

## Versioning / release

Version comes from git tags via **MinVer** — never hand-edit `<Version>`. Tag
`vX.Y.Z` on `main` → `release.yml` packs and publishes to NuGet.
