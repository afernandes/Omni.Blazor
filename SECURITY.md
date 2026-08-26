# Security Policy

## Supported Versions

Omni.Blazor is currently a pre-1.0 project with a fast-moving public API.
Security fixes are provided for the latest stable version published on NuGet.
Users should update to the newest release before reporting an issue that may
already have been fixed.

| Version | Supported |
| --- | --- |
| Latest stable release | Yes |
| Older releases | No |
| Unreleased code on `main` | Best effort |

If a security fix cannot be safely applied to the latest release, the advisory
will state which versions are affected and which version contains the fix.

## Reporting a Vulnerability

Do not open a public issue, discussion, or pull request for a suspected security
vulnerability.

Email the maintainer at [andersonafn@gmail.com](mailto:andersonafn@gmail.com)
with the subject `[SECURITY] Omni.Blazor: <short summary>`. Include as much of the
following information as is available:

- The affected Omni.Blazor package, component, tool, and version.
- The .NET version, hosting model, browser, and operating system, when relevant.
- A clear description of the vulnerability and its potential impact.
- Minimal reproduction steps or a proof of concept.
- Relevant logs, stack traces, screenshots, or configuration, with secrets and
  personal data removed.
- Any known mitigations or suggested remediation.
- Your preferred disclosure timeline and credit information.

If the report is confirmed, the maintainer will open a private GitHub Security
Advisory and coordinate remediation and disclosure there. Please keep the report
confidential until a fix or an agreed disclosure is published.

## Response Process

The maintainer aims to:

1. Acknowledge a new report within three business days.
2. Provide an initial assessment within seven business days when enough
   information is available.
3. Keep the reporter informed of material progress and requests for additional
   evidence.
4. Coordinate a release, advisory, changelog entry, and CVE request when the
   confirmed vulnerability warrants them.

These time frames are goals rather than service-level guarantees. Complex
reports may require additional investigation or coordination with upstream
projects.

## Scope

Reports are in scope when they affect the confidentiality, integrity, or
availability of supported Omni.Blazor packages, the Omni.Blazor MCP tool, or the
repository's release and distribution process.

Vulnerabilities that exist only in an upstream dependency should normally be
reported to that dependency's maintainers. They remain relevant to Omni.Blazor
when the library's use or configuration of the dependency makes the issue
directly exploitable by consumers.

General bugs, accessibility problems, and feature requests that do not have a
security impact should use the repository's public issue tracker.
