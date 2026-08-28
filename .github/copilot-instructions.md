# Copilot instructions — Omni.Blazor

Full guidance lives in [AGENTS.md](../AGENTS.md); architecture in [CLAUDE.md](../CLAUDE.md).

When generating or reviewing an application that consumes the package, use the
[building-omni-blazor-apps skill](../.agents/skills/building-omni-blazor-apps/SKILL.md).
Load only the references it routes to, compare the MCP/catalog version with the
project's resolved package, and never invent public API.

When contributing to this library, use `AGENTS.md` as the single source of truth for
component structure, typed JS modules, styles, tests, showcase coverage, generated
artifacts, build commands, and release/versioning. Do not maintain a second copy of
those rules in this file.
