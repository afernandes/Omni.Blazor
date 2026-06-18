# Copilot instructions — Omni.Blazor

Full guidance lives in [AGENTS.md](../AGENTS.md); architecture in [CLAUDE.md](../CLAUDE.md).

**Omni.Blazor** is a .NET 10 Razor Class Library (~167 components). NuGet id
`AndersonN.Omni.Blazor`, namespace `Omni.Blazor`, assets at `_content/Omni.Blazor/…`.

## Generating UI with the library
Pick components and parameters from the generated manifest — **never invent a
parameter that isn't there**:
- `llms.txt` (index), `llms-full.txt` / `docs/components.json` (full params, events, slots, enums, defaults).

Setup: `builder.Services.AddOmniComponents();` + `<OmniTheme />` in `<head>`. Theme
via `<html data-theme>` / `data-accent` / `data-density`.

## Contributing a component (hard rules)
- Single-file `.razor` under `src/Omni.Blazor/Components/<Category>/` — **no code-behinds**.
- Inherit `OmniComponent` / `OmniComponentWithChildren` / `FormComponent<TValue>`.
- Root splats `class="@RootCss" style="@Style" @attributes="Attributes"`.
- Classes via `CssBuilder` only; consumer `Class` appended last. `@key` on every `foreach`.
- Inputs write via `SetValueAsync`; reactive recompute via `ParameterState<T>`.
- JS only through DI services — never inject `IJSRuntime`.
- Styles → `Themes/_components.scss`; JS → `wwwroot/js/Omni.js` (`window.omniBlazor`).
- Document every public `[Parameter]` with `/// <summary>`; lead the component with `@* one sentence *@`.
- Add a bUnit test and a showcase page. Then regenerate: `dotnet run --project tools/Omni.Blazor.ManifestGen`.

## Build / test
`dotnet build src/Omni.Blazor/Omni.Blazor.csproj` · `dotnet test test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj` · `dotnet format`. Don't hand-edit `<Version>` (MinVer from git tags).
