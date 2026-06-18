# AGENTS.md — Omni.Blazor

Entry point for AI coding agents (Claude Code, Cursor, Copilot, v0, …) — whether
you are **generating UI with** this library or **contributing to** it. Humans:
see [CLAUDE.md](CLAUDE.md) (architecture deep-dive) and
[CONTRIBUTING.md](CONTRIBUTING.md).

## What this is

**Omni.Blazor** — a packable Razor Class Library for **.NET 10**. ~167 components
across Buttons, Data, Display, Forms, Inputs, Layout, Marketing, Navigation and
Overlay. One SCSS bundle, one JS file, all theming via CSS custom properties.

| | |
|---|---|
| NuGet package id | `AndersonN.Omni.Blazor` |
| Namespace / assembly | `Omni.Blazor` |
| Static assets | `_content/Omni.Blazor/css/omni.css`, `_content/Omni.Blazor/js/Omni.js` |
| CSS class prefix | `omni-` |
| Design-token prefix | `--omni-` |
| JS namespace | `window.omniBlazor` |

## Machine-readable surface — read these first

Do **not** browse 167 `.razor` files to learn the API. Read the generated artifacts:

- **[`llms.txt`](llms.txt)** — curated index: every component with a one-line description and a source link, grouped by category. Start here.
- **[`llms-full.txt`](llms-full.txt)** — full dump: every component's parameters, events, slots, enum values and the theme tokens.
- **[`docs/components.json`](docs/components.json)** — the same data, structured (per component: `parameters`/`events`/`slots` with `type`, `enumValues`, `default`, `required`, `summary`). **Generated — never hand-edit.**

These three are produced by one reflection-based generator. **Regenerate after any
change to the public component surface** (new component, new/renamed `[Parameter]`,
new enum, edited XML doc):

```bash
dotnet run --project tools/Omni.Blazor.ManifestGen
```

## MCP server (live tools for agents)

A stdio **MCP server** (`tools/Omni.Blazor.Mcp`) exposes the catalog as live tools —
`list_components`, `get_component`, `search_components` — over the embedded manifest
(self-contained, no library reference). In **this repo** it is already wired in
[`.mcp.json`](.mcp.json). To use it from another MCP client (Cursor, Claude Code,
Copilot), add:

```json
{
  "mcpServers": {
    "omni-blazor": {
      "command": "dotnet",
      "args": ["run", "--project", "tools/Omni.Blazor.Mcp/Omni.Blazor.Mcp.csproj", "-c", "Release"]
    }
  }
}
```

## Using the library (generating UI)

```razor
@* App.razor / _Host — <head> *@
<OmniTheme Accent="amber" />        @* injects _content/Omni.Blazor/css/omni.css *@
```
```csharp
// Program.cs
builder.Services.AddOmniComponents();   // registers every Omni service
```
```html
<!-- runtime theming -->
<html data-theme="light|dark" data-accent="amber|crimson|emerald|blue|violet|teal|cyan|indigo|fuchsia|lime|orange|rose" data-density="compact|comfortable|spacious">
```

Pick components and parameters from `llms.txt` / `components.json`. Do not invent
parameters — if it is not in the manifest, it does not exist.

## Contributing a component — non-negotiable checklist

Template to copy: `src/Omni.Blazor/Components/Buttons/OmniButton.razor` (+ its test).

1. **File** `src/Omni.Blazor/Components/<Category>/Omni<Name>.razor`, single-file (inline `@code` — **no `.razor.cs` code-behinds**).
2. **Inherit** `OmniComponent` (or `OmniComponentWithChildren` for `ChildContent`, or `FormComponent<TValue>` for inputs).
3. **Root element splats all three:** `class="@RootCss" style="@Style" @attributes="Attributes"`.
4. **Compose classes only with `CssBuilder`** — `CssBuilder.Default("omni-x").AddClass(...).AddClass(Class).Build()`. The consumer's `Class` is **always appended last**. (`StyleBuilder` is the twin for inline styles.)
5. **`@key` on every `foreach`** (`@key="item.Id"`).
6. **Inputs** inherit `FormComponent<TValue>`; write through `SetValueAsync` only — never re-implement two-way binding.
7. **Reactive recompute** goes through `ParameterState<T>` registered in `OnInitialized`, not raw `OnParametersSet`.
8. **JS via DI services** (`ScrollManager`, `HotkeyService`, `OverlayLifecycle`, …) — never inject `IJSRuntime` directly. New services register in `Extensions/ServiceCollectionExtensions.cs`.
9. **Styles**: append a block to the single bundle `src/Omni.Blazor/Themes/_components.scss`. **JS**: append to the single file `src/Omni.Blazor/wwwroot/js/Omni.js` under `window.omniBlazor`. (No per-component files.)
10. **Document every public `[Parameter]`** with a `/// <summary>` and give the component a leading `@* one-sentence description *@` — both feed the AI manifest.
11. **Test** at `test/Omni.Blazor.Tests/Components/<Category>/Omni<Name>Tests.cs` (base render + `Class`/`Style`/`Attributes` splat + behaviour) **and** a **showcase page** under `src/Forneria.Demo/Forneria.Demo.Pages/Pages/Showcase/<Category>/`.

Then regenerate the manifest (above), `dotnet format`, and `dotnet test`.

## Build / test / format

```bash
dotnet build src/Omni.Blazor/Omni.Blazor.csproj                 # library only (fast, no exe lock)
dotnet test  test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj    # ~1,700 bUnit tests
dotnet test  test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj --filter "FullyQualifiedName~Omni<Name>Tests"
dotnet format
```

**Lock pitfall:** while a host (Forneria.Demo / FoodService / Omni.Templates.Host)
is running, building the exe fails (`Omni.Blazor.dll` is locked). Build only
`src/Omni.Blazor/Omni.Blazor.csproj`, or stop the host first.

## Versioning / release

Version comes from git tags via **MinVer** — never hand-edit `<Version>`. Tag
`vX.Y.Z` on `main` → `release.yml` packs and publishes to NuGet.
