# Installing the Omni.Blazor Agent Skill

The `building-omni-blazor-apps` skill teaches compatible AI coding agents how to
select and compose Omni.Blazor components without inventing public APIs or mixing
catalog versions.

The complete source directory is:

```text
.agents/skills/building-omni-blazor-apps/
```

Always copy the entire directory, including `SKILL.md`, `agents/`, and
`references/`. The examples below use:

- `<OMNI_BLAZOR_REPO>` for the local Omni.Blazor repository.
- `<CONSUMER_REPO>` for the application that consumes `AndersonN.Omni.Blazor`.

## Choose the installation scope

Use a **project installation** when the skill should be versioned with one
application and shared with its team. Commit the copied directory to that
repository.

Use a **personal installation** when you want the skill available in every
project on your computer. This is the simplest setup for newly created projects,
but teammates will not receive the skill from the application's repository.

| Client | Project location | Personal location |
|---|---|---|
| ChatGPT Desktop / Codex | `<CONSUMER_REPO>/.agents/skills/` | `%USERPROFILE%/.agents/skills/` |
| GitHub Copilot | `<CONSUMER_REPO>/.agents/skills/` | `%USERPROFILE%/.agents/skills/` |
| Claude Code | `<CONSUMER_REPO>/.claude/skills/` | `%USERPROFILE%/.claude/skills/` |

Installing once in `.agents/skills` therefore serves both ChatGPT Desktop/Codex
and GitHub Copilot. Claude Code needs the same skill directory under
`.claude/skills`.

## ChatGPT Desktop and Codex

Standalone local skills are supported by the ChatGPT desktop app, Codex CLI, and
the Codex IDE extension.

### Install for one project

Run this in PowerShell after replacing both placeholders:

```powershell
$source = "<OMNI_BLAZOR_REPO>\.agents\skills\building-omni-blazor-apps"
$destination = "<CONSUMER_REPO>\.agents\skills"

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Recurse -Force -Path $source -Destination $destination
```

The resulting file must exist at:

```text
<CONSUMER_REPO>/.agents/skills/building-omni-blazor-apps/SKILL.md
```

Commit `.agents/skills/building-omni-blazor-apps` when the whole team should use
the skill.

### Install personally for every project

```powershell
$source = "<OMNI_BLAZOR_REPO>\.agents\skills\building-omni-blazor-apps"
$destination = Join-Path $env:USERPROFILE ".agents\skills"

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Recurse -Force -Path $source -Destination $destination
```

After installing:

1. Open the consumer repository in the ChatGPT desktop app, Codex CLI, or the
   Codex IDE extension.
2. Restart the client if the skill does not appear immediately.
3. In ChatGPT, type `@` and select **Build with Omni.Blazor**.
4. In a Codex task, type `$building-omni-blazor-apps`, or use `/skills` and select
   it.
5. Ask it to build or review a screen that consumes Omni.Blazor.

> The manual folder installation above does not install a standalone skill into
> ChatGPT on the web or mobile. Those surfaces receive third-party skills through
> plugins. Plugin packaging is intentionally out of scope for the current manual
> distribution.

## Claude Code

### Install for one project

```powershell
$source = "<OMNI_BLAZOR_REPO>\.agents\skills\building-omni-blazor-apps"
$destination = "<CONSUMER_REPO>\.claude\skills"

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Recurse -Force -Path $source -Destination $destination
```

The resulting file must exist at:

```text
<CONSUMER_REPO>/.claude/skills/building-omni-blazor-apps/SKILL.md
```

Commit `.claude/skills/building-omni-blazor-apps` when the whole team should use
the skill.

### Install personally for every project

```powershell
$source = "<OMNI_BLAZOR_REPO>\.agents\skills\building-omni-blazor-apps"
$destination = Join-Path $env:USERPROFILE ".claude\skills"

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Recurse -Force -Path $source -Destination $destination
```

After installing:

1. Start Claude Code from the consumer repository.
2. If `.claude/skills` did not exist when the current session started, restart
   Claude Code once.
3. Run `/building-omni-blazor-apps` to invoke the skill explicitly, or describe an
   Omni.Blazor application task and let Claude select it automatically.

## GitHub Copilot

### Install for one project

Use the same `.agents/skills` project installation shown for ChatGPT Desktop and
Codex:

```powershell
$source = "<OMNI_BLAZOR_REPO>\.agents\skills\building-omni-blazor-apps"
$destination = "<CONSUMER_REPO>\.agents\skills"

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Recurse -Force -Path $source -Destination $destination
```

The resulting file must exist at:

```text
<CONSUMER_REPO>/.agents/skills/building-omni-blazor-apps/SKILL.md
```

Commit this directory to make it available to Copilot agent mode, Copilot CLI,
Copilot coding agent, and Copilot code review for that repository.

### Install personally for every project

Use the same personal installation as ChatGPT Desktop and Codex:

```powershell
$source = "<OMNI_BLAZOR_REPO>\.agents\skills\building-omni-blazor-apps"
$destination = Join-Path $env:USERPROFILE ".agents\skills"

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Copy-Item -Recurse -Force -Path $source -Destination $destination
```

After installing:

1. Open the consumer repository in an environment that supports Copilot agent
   skills, such as agent mode in Visual Studio Code or GitHub Copilot CLI.
2. Ask Copilot to build or review an application screen using Omni.Blazor. Copilot
   selects the skill from its description when the task matches.
3. In an already-running Copilot CLI session, run `/skills reload` after copying
   the skill.

## Install for all three clients

For a team repository, copy the skill to both project locations:

```text
<CONSUMER_REPO>/
├── .agents/skills/building-omni-blazor-apps/
└── .claude/skills/building-omni-blazor-apps/
```

For personal use across newly created projects, copy it once to each personal
location:

```text
%USERPROFILE%/
├── .agents/skills/building-omni-blazor-apps/
└── .claude/skills/building-omni-blazor-apps/
```

## Verify the installation

Regardless of the client, confirm that:

1. `SKILL.md`, `agents/openai.yaml`, and all three files under `references/` were
   copied.
2. The consuming application has an `AndersonN.Omni.Blazor` package reference.
3. A test prompt such as the following causes the agent to resolve the installed
   package version before selecting components:

```text
Use the building-omni-blazor-apps skill to propose a customer registration form.
Before writing Razor, report the installed Omni.Blazor version and the matching
catalog source you will use.
```

The MCP server is optional. When enabled, its catalog must match the consuming
application's resolved `AndersonN.Omni.Blazor` version. See the
[MCP setup](../AGENTS.md#mcp-server-live-tools-for-agents) for installation and
version checks.

## Update or remove the skill

To update, replace the installed `building-omni-blazor-apps` directory with the
complete directory from the desired Omni.Blazor revision. Do not merge individual
files from different revisions.

To remove it, delete only the corresponding
`building-omni-blazor-apps` directory from the selected project or personal skills
location, then restart or reload the client if necessary.

## Official client documentation

- [OpenAI: Build skills](https://learn.chatgpt.com/docs/build-skills)
- [Anthropic: Extend Claude with skills](https://code.claude.com/docs/en/slash-commands)
- [GitHub: Adding agent skills for GitHub Copilot](https://docs.github.com/en/copilot/how-tos/copilot-on-github/customize-copilot/customize-cloud-agent/add-skills)
