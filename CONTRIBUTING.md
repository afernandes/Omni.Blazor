# Contributing to Omni.Blazor

Thank you for thinking about contributing! This document explains the workflow, conventions, and the bar we hold pull requests to.

By participating you agree to abide by the [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Ways to help

- **Report bugs** with a minimal reproduction — see `.github/ISSUE_TEMPLATE/bug_report.yml`.
- **Propose features** — open a discussion or issue first if the change is non-trivial.
- **Improve docs** — typos, missing parameters, new examples are always welcome.
- **Submit fixes** — small, focused PRs are easier to land than sprawling ones.

For **security issues, do not open a public issue** — follow [SECURITY.md](SECURITY.md).

## Prerequisites

- **.NET 10 SDK** (`global.json` pins the exact version). Install from <https://dotnet.microsoft.com/>.
- A recent IDE: Visual Studio 2026+, Rider, or VS Code with the C# Dev Kit.
- Optional: Node.js — only if you intend to add tooling under `tools/` (not currently used).

Clone & build:

```bash
git clone https://github.com/afernandes/Omni.Blazor.git
cd Omni.Blazor
dotnet build
```

Run the showcase:

```bash
dotnet run --project src/Forneria.Demo/Forneria.Demo
```

The library auto-compiles SCSS via `AspNetCore.SassCompiler` on build — no `npm install` required.

## Repository layout

```
Omni.Blazor/
├─ src/
│  ├─ Omni.Blazor/           ← The packable Razor Class Library
│  ├─ Forneria.Demo/         ← Showcase + reference app
│  └─ FoodService/           ← Real-world POS-style consumer
├─ assets/                   ← Brand / package assets
├─ .github/                  ← CI, templates, dependabot
├─ Directory.Build.props     ← Common MSBuild props
├─ Directory.Packages.props  ← Central Package Management
└─ Omni.Blazor.slnx          ← Solution
```

## Conventions

### Code style

- The repo enforces formatting via `.editorconfig`. Run `dotnet format` before committing.
- C# uses **file-scoped namespaces**, `var` when the type is apparent, expression-bodied members when they fit on one line.
- Razor components are single-file: `src/Omni.Blazor/Components/<Group>/Omni<Name>.razor` with inline `@code` (there are **no** `.razor.cs` code-behinds).
- CSS classes use the `omni-` prefix (`.omni-btn`, `.omni-card`); custom properties use `--omni-` (`var(--omni-accent)`); JS namespace is `window.omniBlazor`.

### Components

- Public parameters need XML doc comments — they show up in IntelliSense and on NuGet.
- Prefer parameters over implicit conventions. Defaults should match the most common case.
- Accessibility is non-negotiable: focus rings, `aria-*` attributes, keyboard support.
- Don't `@inject IJSRuntime` directly when a service exists — use `OmniInteropService`, `ScrollManager`, `BreakpointService`, etc.

### Commits

- Use **imperative present tense**: "Add scroll-to-top button", not "Added scroll-to-top button" or "Adds…".
- Keep the subject under 72 chars; explain the *why* in the body if non-obvious.
- One logical change per commit when reasonable.

### Pull requests

- Fork → branch → PR against `main`.
- Fill in the PR template (`.github/PULL_REQUEST_TEMPLATE.md`).
- New components / public API: include showcase page under `src/Forneria.Demo/Forneria.Demo.Pages/Pages/Showcase/`.
- Bumping the version is **not your responsibility** — MinVer derives it from the next git tag.
- The CI workflow must be green before merge.

### Changelog

Update [CHANGELOG.md](CHANGELOG.md) under the `## [Unreleased]` section. Maintainers move entries into a numbered release at tag time.

## Releasing (maintainers)

1. Update `CHANGELOG.md`: rename `[Unreleased]` to the new version + date.
2. Commit on `main`.
3. Tag: `git tag v1.2.3 -m "v1.2.3"` and `git push --tags`.
4. The `release.yml` workflow builds, packs, pushes to NuGet, and creates a GitHub Release with generated notes.

## License

By submitting a contribution you agree it will be published under the [MIT License](LICENSE) that covers the rest of the project.
