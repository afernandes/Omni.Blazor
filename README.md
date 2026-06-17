# Omni.Blazor

[![NuGet](https://img.shields.io/nuget/v/Omni.Blazor.svg?logo=nuget&color=004880)](https://www.nuget.org/packages/Omni.Blazor)
[![Downloads](https://img.shields.io/nuget/dt/Omni.Blazor.svg?logo=nuget&color=004880)](https://www.nuget.org/packages/Omni.Blazor)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![CI](https://github.com/afernandes/Omni.Blazor/actions/workflows/ci.yml/badge.svg)](https://github.com/afernandes/Omni.Blazor/actions/workflows/ci.yml)

> Modern Blazor component library for .NET 10 — **80+ components**, warm cream/amber design system, dark mode, runtime accent swap, no Bootstrap dependency.

```xml
<PackageReference Include="Omni.Blazor" Version="*" />
```

---

## Highlights

- **80+ components** across data, inputs, layout, navigation, overlays, marketing
- **Single CSS bundle** (~295 KB) — no Bootstrap, no Tailwind, no runtime CSS-in-JS
- **Design tokens** in CSS custom properties (`--omni-*`) — restyleable without recompiling
- **Light / Dark / System** appearance toggle out of the box
- **Runtime accent swap** — pick amber, crimson, emerald, blue, or violet via `[data-accent]`
- **Cream + amber** default palette (Forneria design language)
- **Source Link** + portable PDBs + `.snupkg` — step into the library while debugging
- **Static Web Assets** — CSS/JS shipped via `_content/Omni.Blazor/...` (no extra build step)
- **MIT licensed**

## Installation

```bash
dotnet add package Omni.Blazor
```

Or via NuGet UI in Visual Studio / Rider.

## Quick start

**1) Reference the stylesheet** (handled automatically — `<OmniTheme>` injects it):

```razor
@* App.razor / _Host.cshtml *@
<head>
    <OmniTheme />
</head>
```

**2) Wrap your shell** with the layout primitives:

```razor
<OmniLayout>
    <OmniHeader>
        <OmniBrand>Acme</OmniBrand>
        <OmniAppearanceToggle />
    </OmniHeader>
    <OmniSidebar>
        <OmniPanelMenu>
            <OmniPanelMenuItem Text="Dashboard" Icon="dashboard" Href="/" />
            <OmniPanelMenuItem Text="Orders"   Icon="receipt"   Href="/orders" />
        </OmniPanelMenu>
    </OmniSidebar>
    <OmniMain>
        @Body
    </OmniMain>
</OmniLayout>
```

**3) Use components** anywhere:

```razor
<OmniDataGrid Data="@orders" PageSize="20" AllowFiltering AllowSorting>
    <OmniDataGridColumn Property="Id"     Title="#" />
    <OmniDataGridColumn Property="Total"  Title="Total" Format="C" />
    <OmniDataGridColumn Property="Status" Title="Status">
        <Template Context="o">
            <OmniBadge Color="@StatusColor(o.Status)">@o.Status</OmniBadge>
        </Template>
    </OmniDataGridColumn>
</OmniDataGrid>
```

## Component catalog

<details>
<summary><strong>Buttons</strong></summary>

OmniButton, OmniIconButton, OmniSplitButton, OmniToggleButton, OmniFab, OmniFabMenu, OmniScrollToTopButton, OmniSpeechToTextButton
</details>

<details>
<summary><strong>Data</strong></summary>

OmniDataGrid, OmniDataGridColumn, OmniVirtualize, OmniDropZone, OmniChart, OmniSparkline
</details>

<details>
<summary><strong>Display</strong></summary>

OmniCard, OmniStat, OmniBadge, OmniChip, OmniAvatar, OmniIcon, OmniKbd, OmniProgress, OmniSkeleton, OmniQRCode, OmniBarcode, OmniAlert
</details>

<details>
<summary><strong>Inputs</strong></summary>

OmniTextBox, OmniPassword, OmniTextArea, OmniCheckBox, OmniSwitch, OmniSelect, OmniMultiSelect, OmniDatePicker, OmniDateRangePicker, OmniTimePicker, OmniCalendar, OmniFormField, OmniUpload
</details>

<details>
<summary><strong>Layout</strong></summary>

OmniLayout, OmniHeader, OmniSidebar, OmniMain, OmniContainer, OmniBrand, OmniAppBar, OmniDrawer, OmniDrawerToggle, OmniFooter, OmniPaneHeader, OmniBento, OmniBentoItem, OmniMasonry, OmniParallax, OmniParallaxLayer, OmniRow, OmniCol, OmniStack
</details>

<details>
<summary><strong>Navigation</strong></summary>

OmniPanelMenu, OmniPanelMenuItem, OmniPanelMenuSection, OmniBreadcrumb, OmniTabs, OmniTabItem, OmniSegmentedControl, OmniStepper, OmniMenuBar, OmniMenuBarItem
</details>

<details>
<summary><strong>Marketing</strong></summary>

OmniEyebrow, OmniHero, OmniMosaic, OmniMosaicCard
</details>

<details>
<summary><strong>Overlays & feedback</strong></summary>

OmniDialogHost, OmniNotificationHost, OmniTooltipHost, OmniContextMenuHost, OmniConfirmDialog, OmniAlertDialog, OmniOverlays, OmniPopover, OmniBottomSheet
</details>

<details>
<summary><strong>Foundations</strong></summary>

OmniTheme, OmniAppearanceToggle, OmniThemePicker, OmniSwipeArea, OmniHotkeys, OmniExitPrompt, OmniHidden, BreakpointService, ScrollManager
</details>

## Theming

All visual tokens are CSS custom properties:

```css
:root {
  --omni-bg:           oklch(0.985 0.004 60);
  --omni-fg:           oklch(0.18 0.01 60);
  --omni-accent:       #d97706;  /* amber by default */
  --omni-accent-hover: #b45309;
  --omni-radius:       10px;
  /* ...80+ tokens; see Themes/_tokens.scss */
}
```

Swap accents at runtime:

```html
<html data-accent="emerald">  <!-- amber | crimson | emerald | blue | violet -->
```

Or set dark mode:

```html
<html data-theme="dark">  <!-- light | dark | system -->
```

The `<OmniAppearanceToggle />` component does both, persists to `localStorage`, and respects `prefers-color-scheme`.

## Supported targets

| Framework      | Status |
|----------------|--------|
| .NET 10 (Blazor Server) | ✅ Primary target |
| .NET 10 (Blazor WebAssembly) | ✅ Supported |
| .NET 10 (Blazor United / hybrid) | ✅ Supported |

## Examples

The repository ships two reference apps:

- **`src/Forneria.Demo`** — full showcase + landing + auth + 50+ component pages. The default demo.
- **`src/FoodService`** — POS-style real-world layout consuming the library.

To run locally:

```bash
git clone https://github.com/afernandes/Omni.Blazor.git
cd Omni.Blazor
dotnet run --project src/Forneria.Demo/Forneria.Demo
```

Open <http://localhost:5253>.

## Contributing

Bug reports, feature requests and PRs are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md) and [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md). Vulnerability reports follow [SECURITY.md](SECURITY.md).

## Versioning

Releases follow [Semantic Versioning](https://semver.org). The changelog lives at [CHANGELOG.md](CHANGELOG.md) and is authored in the [Keep a Changelog](https://keepachangelog.com) format.

## License

Released under the [MIT License](LICENSE). The bundled CSS reset is forked from [Bootstrap 5](https://github.com/twbs/bootstrap) (MIT) — credit kept in `Themes/_reset.scss`.
