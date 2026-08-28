# Catalog and versioning

The catalog is the API contract. Match it to the package restored by the consumer;
component names alone are not enough because parameters and enum values can change.

## Resolve the consumer version

Check, in order:

1. The application's `PackageReference` for `AndersonN.Omni.Blazor` and the optional
   `AndersonN.Omni.Blazor.Ai` package.
2. `Directory.Packages.props`, imported props/targets, or other central package
   management when the project does not contain a literal version.
3. The restored `obj/project.assets.json` when a property, range, or floating version
   makes the resolved version ambiguous.

Do not change a package version merely to make the available catalog match.

## Choose the contract source

Use the first source that is both available and version-matched:

1. MCP: call `get_catalog_info`. Its `version` must match the resolved package before
   using the other catalog tools. `source: embedded-manifest` means the manifest ships
   with that MCP package. An external unversioned manifest may return `version: null`;
   establish its tag or commit provenance separately.
2. A checkout of the same release tag: read `llms.txt` for discovery and
   `docs/components.json` or `llms-full.txt` for exact API details.
3. A tagged raw artifact, replacing `<version>` with the resolved SemVer:
   `https://raw.githubusercontent.com/afernandes/Omni.Blazor/v<version>/docs/components.json`.
4. The public showcase artifacts that track `main`, only when the application consumes
   that same unreleased source: `https://afernandes.github.io/Omni.Blazor/components.json`.

If no matching contract is available, state the mismatch and avoid inventing API.
Ask before installing/updating a global tool or changing the consumer's agent config.

## MCP usage

After versions match:

1. `search_components` with the semantic need, such as `editable grid`, `entity`, or
   `empty state`.
2. `get_component` for every component emitted in Razor.
3. For a schema/builder/provider named in that response, use
   `search_configuration_apis` and `get_configuration_api` before writing C# setup.
4. Use `list_components` only for broad catalog exploration; do not load the full
   catalog into context for a focused screen.

The global tool embeds its catalog. If the user authorizes an update, use
`dotnet tool update -g AndersonN.Omni.Blazor.Mcp`; clients holding the executable open
may need to be closed before updating on Windows.
