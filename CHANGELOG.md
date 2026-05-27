# Changelog

All notable changes to **Omni.Blazor** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The version is derived from the latest `vX.Y.Z` git tag by [MinVer](https://github.com/adamralph/minver).

## [Unreleased]

### Added
- Initial public release scaffolding (`Directory.Build.props`, `Directory.Packages.props`, MinVer, Source Link, central package management).
- GitHub Actions CI + release pipeline targeting NuGet.org.
- Governance docs: `LICENSE`, `CHANGELOG`, `CONTRIBUTING`, `CODE_OF_CONDUCT`, `SECURITY`, issue & PR templates, Dependabot.
- Package icon (`assets/icon.png`).

### Changed
- Renamed library and namespace `Totvs.Blazor → Omni.Blazor` (project, components, CSS classes, JS namespace, design tokens).
- Replaced Bootstrap (full bundle, ~150 KB pre-gzip) with a minimal forked reset in `Themes/_reset.scss`. Compiled CSS now **~295 KB** (down from 438 KB, **-33 %**).
- Solution file `ClaudeBlazor.slnx → Omni.Blazor.slnx`.

### Removed
- Bootstrap SCSS source tree (`src/Omni.Blazor/BootstrapSrc/`) and `Themes/_bootstrap-override.scss`.

## Release format

Each release section follows this shape:

```
## [1.2.0] - 2026-MM-DD

### Added
- New `OmniXxx` component …

### Changed
- Tightened `OmniDataGrid` keyboard navigation …

### Deprecated
- `OmniLegacyThing` will be removed in 2.0; use `OmniNewThing`.

### Removed
- Dropped support for `oldParam` on `OmniFoo`.

### Fixed
- `OmniDatePicker` no longer crashes on …

### Security
- Bumped transitive `Xyz` to patch CVE-XXXX-YYYY.
```

[Unreleased]: https://github.com/afernandes/Omni.Blazor/compare/HEAD
