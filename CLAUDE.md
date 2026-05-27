# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

Three first-class projects under `src/`, plus a single test project:

- **`src/Omni.Blazor/`** — packable Razor Class Library (`Microsoft.NET.Sdk.Razor`). ~140 components across `Components/{Buttons,Display,Inputs,Layout,Navigation,Overlay,Data,Forms,Marketing,Base}/`. Ships its own SCSS bundle (`Themes/omni.scss` → auto-compiled by `AspNetCore.SassCompiler` on build → served via `_content/Omni.Blazor/css/omni.css`). The only project where `IsPackable=true`.
- **`src/Forneria.Demo/`** — three-project showcase: `.Pages` (RCL with all demo pages), `Forneria.Demo` (Blazor Server host), `Forneria.Demo.Wasm` (Blazor WebAssembly host). Dual-render — both hosts reference the same `.Pages` RCL.
- **`src/FoodService/`** — POS-style consumer app: `FoodService.Pages` (RCL) + `FoodService` (Server host only). Two domain features: `PdvFeature/` (operator point-of-sale) and `CardapioFeature/` (customer-facing digital menu with 7-screen state machine).
- **`test/Omni.Blazor.Tests/`** — bUnit + xUnit.v3, one `Components/{Folder}/Omni{Name}Tests.cs` per library component (~1000 tests).

Solution file: `Omni.Blazor.slnx` (new SDK-style solution format). Central Package Management is on — `Directory.Packages.props` is the single source of truth for versions; csproj files reference packages by ID only.

## Build / run / test

The .NET SDK is pinned in `global.json` (`10.0.100-rc.1` or later via `latestMinor`).

```bash
# Build the whole solution
dotnet build

# Library only (fast, no exe-bin lock conflicts)
dotnet build src/Omni.Blazor/Omni.Blazor.csproj

# Run the showcase
dotnet run --project src/Forneria.Demo/Forneria.Demo        # http://localhost:5253

# Run the POS / cardapio app
dotnet run --project src/FoodService/FoodService            # https://localhost:7301

# All tests
dotnet test test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj

# Filter to one component
dotnet test test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj --filter "FullyQualifiedName~OmniButtonTests"

# Filter to a folder
dotnet test test/Omni.Blazor.Tests/Omni.Blazor.Tests.csproj --filter "FullyQualifiedName~Components.Inputs"

# Format
dotnet format
```

**Lock pitfall**: when FoodService or Forneria.Demo is running, building the executable project fails because `Omni.Blazor.dll` is locked in the host's bin. Either stop the host first, or build only `src/Omni.Blazor/Omni.Blazor.csproj` — its output goes to its own `bin/` and isn't affected by the running consumer.

SCSS recompiles automatically on every build via `AspNetCore.SassCompiler`. No `npm install` required.

## Component authoring conventions (non-negotiable)

Every razor component in `src/Omni.Blazor/` MUST follow this shape:

```razor
@namespace Omni.Blazor.Components
@inherits OmniComponent                  @* or OmniComponentWithChildren *@
@using Omni.Blazor.Utilities             @* required for CssBuilder *@

<element class="@RootCss" style="@Style" @attributes="Attributes">
    @* content *@
</element>

@code {
    [Parameter] public ... { get; set; }

    private string RootCss => CssBuilder.Default("omni-mycomponent")
        .AddClass("omni-mycomponent-on", IsOn)
        .AddClass(SomeEnum switch { Variant.A => "omni-mycomponent-a", _ => null })
        .AddClass(Class)                  // ALWAYS append consumer's Class last
        .Build();
}
```

Hard rules enforced by the test suite:

1. **`OmniComponent` is the base for everything.** It exposes `Class`, `Style`, `Attributes` (`CaptureUnmatchedValues=true`), an auto-generated `Id`, and `ParameterScope`. The root element of every component must splat all three (`class="@RootCss" style="@Style" @attributes="Attributes"`).
2. **`CssBuilder` is the only way to compose classes.** The legacy `Cls(params string?[])` helper was deleted from `OmniComponent`. Don't reintroduce it. `StyleBuilder` is the twin for inline styles when you have conditional CSS declarations.
3. **`@key` in every foreach.** Lists of items render with `@key="item.Id"` to keep Blazor's diff stable.
4. **Form inputs inherit `FormComponent<TValue>`** (in `Components/Base/`). It wires `Value`/`ValueChanged`/`ValueExpression`, builds `FieldIdentifier`, attaches to cascading `EditContext`, exposes `SetValueAsync` (the only sanctioned write path), and runs per-input validators. Never re-implement two-way binding manually in an input.
5. **Reactive recomputation goes through `ParameterState<T>`**, not raw `OnParametersSet`. Register in `OnInitialized`:
   ```csharp
   private ParameterState<TValue> _state = null!;
   protected override void OnInitialized() {
       _state = RegisterParameter<TValue>(nameof(Value))
           .WithParameter(() => Value)
           .WithChangeHandler(Recompute)
           .Attach();
   }
   ```
   `OmniComponent.SetParametersAsync` runs `ParameterScope.DetectAllAsync()` after base assigns parameters — handlers fire only on real changes. This avoids re-encoding/re-computing when consumers change unrelated parameters (`Class`, `Style`). Examples in `OmniBarcode`, `OmniQRCode`, `OmniChart`, `OmniSlider`, `OmniNumeric`, `OmniMaskedTextBox`, `OmniPopover`, `OmniValidationMessage`.
6. **DI services over `IJSRuntime`.** When something needs JS, use the existing service (`ScrollManager`, `BreakpointService`, `HotkeyService`, `KeyInterceptorService`, `OverlayLifecycle`, etc.) instead of injecting `IJSRuntime` directly. Services are registered by `AddOmniComponents()` in `Extensions/ServiceCollectionExtensions.cs`.
7. **Naming.** CSS class prefix `omni-`, CSS custom property prefix `--omni-`, JS namespace `window.omniBlazor`. App-level CSS in consumers uses its own prefix (`fs-pdv-*`, `fs-cd-*` in FoodService).

## Theming

All visual tokens are CSS custom properties in `src/Omni.Blazor/Themes/_tokens.scss`:

- Color palette: `--omni-bg`, `--omni-fg`, `--omni-accent` (+ Hover/Soft), `--omni-good/warn/danger/info`, plus elevations (`--omni-bg-elev`, `--omni-bg-sunken`) and line strengths (`--omni-line`, `--omni-line-strong`).
- Runtime swappable: 12 accent presets via `<html data-accent="amber|crimson|emerald|blue|violet|teal|cyan|indigo|fuchsia|lime|orange|rose">`.
- Dark/light: `<html data-theme="light|dark">` (or `system` for prefers-color-scheme).
- Density: `<html data-density="compact|comfortable|spacious">` rescales topbar height, radii, padding, gaps.

`<OmniTheme />` in `<head>` injects the stylesheet link. `<OmniAppearanceToggle />` exposes a user-facing toggle (persists to localStorage). `<OmniBreakpointProvider>` cascades the current breakpoint (Xs/Sm/Md/Lg/Xl/Xxl) — query `BreakpointService` for the live value.

## Testing

`test/Omni.Blazor.Tests/TestContextBase` is the base for every test. It inherits `Bunit.TestContext` (aliased as `BunitTestContext` to disambiguate from xUnit.v3's `TestContext`), pre-registers every Omni service (BreakpointService, ScrollManager, DialogService, HotkeyService, NotificationService, TooltipService, ContextMenuService), and sets `JSInterop.Mode = Loose` so JS calls don't throw.

Standard coverage per component (template = `OmniButtonTests.cs`):

1. Default render produces the expected root tag + base class.
2. Consumer `Class` parameter is appended to the root element.
3. Consumer `Style` is forwarded to the root `style` attribute.
4. Unmatched attributes (`data-testid`, `aria-label`) splat onto root via `.AddUnmatched(...)`.
5. Key behavioural parameters (enum variants via `[Theory]`, event callbacks, two-way binding round-trip, boundary clamping, disabled handling).

For components with `ParameterState`, tests assert that the change handler does NOT fire when only `Class`/`Style` change (use `Assert.Same` on a cached reference, or an `internal int RecomputeCount` counter — `InternalsVisibleTo("Omni.Blazor.Tests")` is set on the library project to expose internals).

## App architecture patterns (FoodService)

- **State containers are Scoped DI services** raising an `event Action? OnChange`. Consumers subscribe in `OnInitialized` (`State.OnChange += StateHasChanged`) and unsubscribe in `Dispose` — always pair with `@implements IDisposable`. Examples: `PdvOrderService`, `CardapioDigitalState`.
- **State machines drive screen routing.** `CardapioDigitalState.Screen` is an enum (`Home → Sizes → Flavors → Preview → Cart → Checkout → Tracking`); the page renders one `Cardapio{X}Screen.razor` per state via `@switch`.
- **Feature folders own their models + mock data + components + screens.** See `src/FoodService/FoodService.Pages/Pages/CardapioFeature/`: `CardapioModels.cs` (records), `CardapioMockData.cs` (static catalog), `CardapioDigitalState.cs` (Scoped service), `Components/` (atomic), `Screens/` (one per state).
- **Cardapio records are prefixed `Cd`** (CdFlavor, CdBorda, CdCartItem, CdPayMethod) to avoid name collision with `PdvFeature.*`. Mechanical renames that touched string literals or CSS class names have historically introduced "CdBorda" / "Pizza Pizza" UI leaks — grep for the type prefix in `.razor` strings before declaring such a refactor done.

## Solution / packaging notes

- `Directory.Build.props` defaults every project to `<IsPackable>false</IsPackable>`. Only `src/Omni.Blazor/Omni.Blazor.csproj` flips it to `true`, which also conditionally pulls in MinVer + SourceLink.
- **Versioning is via git tags** (MinVer). Tag `v1.2.3` on `main` → `release.yml` packs and pushes to NuGet. Never hand-edit `<Version>` in a csproj.
- `Forneria.Demo` and `FoodService` are explicitly non-packable consumers — they showcase the library but never ship as packages.
- New components MUST include a showcase page under `src/Forneria.Demo/Forneria.Demo.Pages/Pages/Showcase/` (per CONTRIBUTING.md).

## Reference design assets

`docs/design-reference/` holds wireframe / mock bundles that have been used to drive past implementations (PDV, CardapioDigital, SAAS Wireframes). When the user references a "design bundle" URL of the form `https://api.anthropic.com/v1/design/h/...`, the response is a gzipped tar — fetch with `curl`, expand with `gunzip | tar -x`, then read the `README.md` inside before any HTML/JSX so you understand intent before structure.
