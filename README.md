# Omni.Blazor

[![NuGet](https://img.shields.io/nuget/v/AndersonN.Omni.Blazor.svg?logo=nuget&color=004880)](https://www.nuget.org/packages/AndersonN.Omni.Blazor)
[![Downloads](https://img.shields.io/nuget/dt/AndersonN.Omni.Blazor.svg?logo=nuget&color=004880)](https://www.nuget.org/packages/AndersonN.Omni.Blazor)
[![License: MIT](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![CI](https://github.com/afernandes/Omni.Blazor/actions/workflows/ci.yml/badge.svg)](https://github.com/afernandes/Omni.Blazor/actions/workflows/ci.yml)

> Modern Blazor component library for .NET 10 — **198 components**, warm cream/amber design system, dark mode, runtime accent swap, no Bootstrap dependency.

```xml
<PackageReference Include="AndersonN.Omni.Blazor" Version="*" />
```

---

## Highlights

- **198 components** across data, inputs, layout, navigation, overlays, marketing, and AI/chat
- **Single CSS bundle** (~295 KB) — no Bootstrap, no Tailwind, no runtime CSS-in-JS
- **Design tokens** in CSS custom properties (`--omni-*`) — restyleable without recompiling
- **Light / Dark / System** appearance toggle out of the box
- **Runtime accent swap** — pick amber, crimson, emerald, blue, or violet via `[data-accent]`
- **Cream + amber** default palette (Forneria design language)
- **Source Link** + portable PDBs + `.snupkg` — step into the library while debugging
- **Static Web Assets** — CSS/JS shipped via `_content/Omni.Blazor/...` (no extra build step)
- **AI-ready** — `llms.txt` + `llms-full.txt` + a machine-readable [`docs/components.json`](docs/components.json) manifest and an [`AGENTS.md`](AGENTS.md) runbook, so coding agents (Claude Code, Cursor, Copilot) generate correct code
- **MIT licensed**

## Installation

```bash
dotnet add package AndersonN.Omni.Blazor
```

Or via NuGet UI in Visual Studio / Rider.

### Optional — AI layer

The AI orchestration (`OmniChatClient` + the drop-in `OmniAiConversation`, built on the
standard `Microsoft.Extensions.AI.IChatClient`) ships as a **separate package** so the base
library never forces the AI dependency on consumers who don't use it:

```bash
dotnet add package AndersonN.Omni.Blazor.Ai
```

It references the base package, so installing it pulls in `AndersonN.Omni.Blazor` too. The
streaming-UI primitives (`OmniStreamingText`, `OmniMessage`, `OmniPromptInput`, …) stay in the
base package — only the `IChatClient`-backed orchestration lives in `.Ai`.

> **Security — keep model credentials server-side.** In a Blazor **WebAssembly** app, never
> configure the `IChatClient` with a provider API key on the client (it ships to the browser).
> Point the client at a **server-side proxy / your own backend endpoint** that holds the key.
> In Blazor **Server** the key already lives on the server.

## Quick start

**1) Reference the stylesheet** (handled automatically — `<OmniTheme>` injects it):

```razor
@* App.razor / _Host.cshtml *@
<head>
    <OmniTheme />
</head>
```

**2) Wrap your shell** with the layout primitives (`MainLayout.razor`):

```razor
@inherits LayoutComponentBase

<OmniLayout>
    <OmniAppBar>
        <OmniDrawerToggle Target="_drawer" />
        <OmniBrand Name="Acme" />
        <div class="omni-header-spacer"></div>
        <OmniAppearanceToggle />
    </OmniAppBar>

    <OmniDrawer @ref="_drawer" Anchor="DrawerAnchor.Left">
        <OmniPanelMenuSection Label="Main">
            <OmniPanelMenuItem Text="Dashboard" Icon="layout-dashboard" Path="/" />
            <OmniPanelMenuItem Text="Orders"    Icon="package"          Path="/orders" />
        </OmniPanelMenuSection>
    </OmniDrawer>

    <OmniMain>
        @Body
    </OmniMain>
</OmniLayout>

@* Renders dialogs, notifications, tooltips and context menus opened via the services. *@
<OmniOverlayHosts />

@code {
    private OmniDrawer _drawer = null!;
}
```

**3) Use components** anywhere:

```razor
<OmniDataGrid TItem="Order" Data="@orders" AllowPaging PageSize="20" AllowSorting AllowColumnFilter>
    <Columns>
        <OmniDataGridColumn TItem="Order" Title="#" Property="@(o => (object)o.Id)" />
        <OmniDataGridColumn TItem="Order" Title="Total" Property="@(o => (object)o.Total)">
            <Template>
                <span class="omni-mono">@context.Total.ToString("C")</span>
            </Template>
        </OmniDataGridColumn>
        <OmniDataGridColumn TItem="Order" Title="Status" Property="@(o => o.Status)">
            <Template>
                <OmniBadge Text="@context.Status" Variant="BadgeVariant.Accent" />
            </Template>
        </OmniDataGridColumn>
    </Columns>
</OmniDataGrid>
```

## Component catalog

_198 components — generated from [`docs/components.json`](docs/components.json). Run `dotnet run --project tools/Omni.Blazor.ManifestGen` after changing the public surface._

<details>
<summary><strong>Layout</strong> (32)</summary>

OmniAppBar, OmniAppearanceToggle, OmniAuthLayout, OmniBanner, OmniBento, OmniBentoItem, OmniBrand, OmniBreakpointProvider, OmniCol, OmniContainer, OmniDrawer, OmniDrawerToggle, OmniFooter, OmniHidden, OmniLayout, OmniMain, OmniMasonry, OmniMediaQuery, OmniPaneHeader, OmniPaneToolbar, OmniParallax, OmniParallaxLayer, OmniRow, OmniSplitAsideLabel, OmniSplitView, OmniSplitter, OmniSplitterPane, OmniStack, OmniSwipeArea, OmniTheme, OmniThemePicker, OmniToolBar
</details>

<details>
<summary><strong>Navigation</strong> (15)</summary>

OmniBreadcrumb, OmniExitPrompt, OmniGlobalSearch, OmniHotkey, OmniMenuBar, OmniMenuBarItem, OmniPagination, OmniPanelMenu, OmniPanelMenuItem, OmniPanelMenuSection, OmniSegmentedControl, OmniStep, OmniStepper, OmniTabItem, OmniTabs
</details>

<details>
<summary><strong>Inputs</strong> (31)</summary>

OmniAutoComplete, OmniCalendar, OmniCheckBox, OmniCheckBoxList, OmniColorPicker, OmniDatePicker, OmniDateRangePicker, OmniFileUpload, OmniFormField, OmniListBox, OmniMaskedTextBox, OmniMultiSelect, OmniNumeric, OmniPassword, OmniPasswordStrength, OmniPickList, OmniQtyStepper, OmniRadio, OmniRadioGroup, OmniRating, OmniSecurityCode, OmniSelect, OmniSignaturePad, OmniSlider, OmniSpeechToText, OmniSpeechToTextButton, OmniSwitch, OmniTagInput, OmniTextArea, OmniTextBox, OmniTimePicker
</details>

<details>
<summary><strong>Forms</strong> (11)</summary>

OmniCompareValidator, OmniCustomValidator, OmniDataAnnotationValidator, OmniEmailValidator, OmniForm, OmniLengthValidator, OmniRangeValidator, OmniRegexValidator, OmniRequiredValidator, OmniValidationMessage, OmniValidationSummary
</details>

<details>
<summary><strong>Buttons</strong> (8)</summary>

OmniButton, OmniFab, OmniFabMenu, OmniFabMenuItem, OmniScrollToTopButton, OmniSocialButton, OmniSplitButton, OmniToggleButton
</details>

<details>
<summary><strong>Display</strong> (37)</summary>

OmniAccordion, OmniAccordionItem, OmniAlert, OmniAvatar, OmniAvatarGroup, OmniBadge, OmniBarcode, OmniCard, OmniCardBody, OmniCardGroup, OmniCardMedia, OmniCarousel, OmniCarouselItem, OmniChart, OmniChip, OmniDescriptionItem, OmniDescriptionList, OmniEmptyState, OmniIcon, OmniImage, OmniKbd, OmniLabel, OmniLink, OmniMarkdown, OmniOptionCard, OmniProgress, OmniProgressCircular, OmniQRCode, OmniResult, OmniSkeleton, OmniSparkline, OmniSpinner, OmniStat, OmniStatGroup, OmniStatusBadge, OmniTimeline, OmniTimelineItem
</details>

<details>
<summary><strong>Data</strong> (34)</summary>

OmniChat, OmniDataFilter, OmniDataFilterItem, OmniDataFilterProperty, OmniDataGrid, OmniDataGridColumn, OmniDayView, OmniDiagramCanvas, OmniDropZone, OmniDropZoneContainer, OmniDropZoneItem, OmniFileManager, OmniGantt, OmniGanttColumn, OmniHtmlEditor, OmniHtmlEditorButton, OmniKanban, OmniMonthView, OmniMultiDayView, OmniPivotColumn, OmniPivotGrid, OmniPivotRow, OmniPivotValue, OmniScheduler, OmniTree, OmniTreeGrid, OmniTreeGridColumn, OmniTreeItem, OmniTreeLevel, OmniVirtualize, OmniWeekView, OmniYearPlannerView, OmniYearTimelineView, OmniYearView
</details>

<details>
<summary><strong>Overlays &amp; feedback</strong> (18)</summary>

AlertDialog, ConfirmDialog, OmniBottomSheet, OmniCommandPalette, OmniConfirmPrompt, OmniContextMenuHost, OmniDialogHost, OmniMenu, OmniMenuItem, OmniMenuSeparator, OmniNotificationHost, OmniOverlay, OmniOverlayHosts, OmniPopover, OmniTooltipHost, OmniTour, OmniTourHost, OmniTourStep
</details>

<details>
<summary><strong>Marketing</strong> (4)</summary>

OmniEyebrow, OmniHero, OmniMosaic, OmniMosaicCard
</details>

<details>
<summary><strong>AI &amp; chat</strong> (7)</summary>

OmniAiConversation, OmniCitation, OmniMessage, OmniPromptInput, OmniStreamingText, OmniSuggestionChips, OmniThinkingBlock
</details>

## Page templates

Beyond the components, the repo ships **26 ready-made pages** — login, register, 2FA,
users grid, roles, permissions, dashboard, pricing, error states, and the **app shell**
(top bar + collapsible sidebar + content) that every app needs.

They are **copy-paste starters**, not a package: run the gallery, hit *"Ver código"*, paste
into your project. The copyable markup only uses components plus CSS classes that ship in
`omni.css`, so it works as-is in your app (enforced in CI).

```bash
dotnet run --project src/Omni.Templates/Omni.Templates.Host   # http://localhost:5305
```

→ **[Template index and usage](src/Omni.Templates/README.md)**

## For AI agents

Coding agents (Claude Code, Cursor, Copilot, v0) should read the generated,
machine-readable surface instead of browsing component source:

- **[`llms.txt`](llms.txt)** — curated index of every component (name, category, one-line description, source link).
- **[`llms-full.txt`](llms-full.txt)** — full dump: parameters, events, slots, enum values and theme tokens.
- **[`docs/components.json`](docs/components.json)** — the same data, structured (generated by `tools/Omni.Blazor.ManifestGen`).
- **[`AGENTS.md`](AGENTS.md)** — runbook for using the library and contributing components. Cursor (`.cursor/rules/`) and Copilot (`.github/copilot-instructions.md`) rules point here.
- **MCP server** — live `list_components` / `get_component` / `search_components` tools over the manifest. Install with `dotnet tool install -g AndersonN.Omni.Blazor.Mcp` and run `omni-blazor-mcp`; see [AGENTS.md](AGENTS.md#mcp-server-live-tools-for-agents).

If a parameter is not in `docs/components.json`, it does not exist — don't invent it.

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
