#!/usr/bin/env python3
"""Build clean Blazor Server/WASM consumers and execute the packed MCP tool."""

from __future__ import annotations

import os
import pathlib
import re
import subprocess
import sys
import tempfile
import textwrap


BASE_PATTERN = re.compile(
    r"^AndersonN\.Omni\.Blazor\.(?!Ai\.|Mcp\.|Localization\.Po\.)(?P<version>.+)\.nupkg$"
)


def fail(message: str) -> "NoReturn":
    raise RuntimeError(message)


def write(path: pathlib.Path, content: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(textwrap.dedent(content).lstrip(), encoding="utf-8")


def run(command: list[str], cwd: pathlib.Path, env: dict[str, str]) -> str:
    print(f"+ {' '.join(command)}", flush=True)
    completed = subprocess.run(
        command,
        cwd=cwd,
        env=env,
        text=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        timeout=300,
        check=False,
    )
    if completed.returncode != 0:
        print(completed.stdout, file=sys.stderr)
        fail(f"Command failed with exit code {completed.returncode}")
    return completed.stdout


def package_version(artifacts: pathlib.Path, expected: str | None) -> str:
    matches: list[tuple[pathlib.Path, str]] = []
    for package in artifacts.glob("AndersonN.Omni.Blazor.*.nupkg"):
        if package.name.endswith(".snupkg"):
            continue
        match = BASE_PATTERN.match(package.name)
        if match:
            matches.append((package, match.group("version")))
    if len(matches) != 1:
        fail(f"Expected one base package in {artifacts}, found {[p.name for p, _ in matches]}")

    version = matches[0][1]
    if expected is not None and version != expected:
        fail(f"Packed version {version!r} differs from expected version {expected!r}")

    for package_id in (
        "AndersonN.Omni.Localization",
        "AndersonN.Omni.Localization.Json",
        "AndersonN.Omni.Localization.Po",
        "AndersonN.Omni.Blazor.Ai",
        "AndersonN.Omni.Blazor.Mcp",
    ):
        package = artifacts / f"{package_id}.{version}.nupkg"
        if not package.is_file():
            fail(f"Missing exact package {package.name}")
    return version


def create_server(root: pathlib.Path, version: str) -> pathlib.Path:
    project = root / "server"
    write(
        project / "SmokeServer.csproj",
        f"""
        <Project Sdk="Microsoft.NET.Sdk.Web">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="AndersonN.Omni.Blazor" Version="{version}" />
            <PackageReference Include="AndersonN.Omni.Blazor.Ai" Version="{version}" />
            <PackageReference Include="AndersonN.Omni.Localization.Po" Version="{version}" />
          </ItemGroup>
        </Project>
        """,
    )
    write(
        project / "Program.cs",
        """
        using Omni.Blazor;
        using Omni.Blazor.Localization;
        using Omni.Localization.Po;
        using PackageSmoke.Server;

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        builder.Services.AddOmniComponents();
        builder.Services.AddOmniPortableObjectLocalization<OmniBlazorResource, Program>("Localization");
        _ = typeof(Omni.Blazor.Ai.OmniChatClient);

        var app = builder.Build();
        app.UseStaticFiles();
        app.UseAntiforgery();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
        app.Run();
        """,
    )
    write(
        project / "App.razor",
        """
        @namespace PackageSmoke.Server
        <!DOCTYPE html>
        <html lang="pt-BR">
        <head><HeadOutlet /></head>
        <body>
            <OmniTheme />
            <OmniButton Text="Package smoke" />
            <script src="_framework/blazor.web.js"></script>
        </body>
        </html>
        """,
    )
    write(
        project / "_Imports.razor",
        """
        @using Microsoft.AspNetCore.Components.Web
        @using Omni.Blazor.Components
        """,
    )
    return project


def create_localization(root: pathlib.Path, version: str) -> pathlib.Path:
    project = root / "localization"
    write(
        project / "SmokeLocalization.csproj",
        f"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <OutputType>Exe</OutputType>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.10" />
            <PackageReference Include="AndersonN.Omni.Localization" Version="{version}" />
            <PackageReference Include="AndersonN.Omni.Localization.Json" Version="{version}" />
          </ItemGroup>
        </Project>
        """,
    )
    write(
        project / "Program.cs",
        """
        using System.Globalization;
        using System.Text;
        using Microsoft.Extensions.DependencyInjection;
        using Omni.Localization;
        using Omni.Localization.Json;

        var services = new ServiceCollection();
        services.AddOmniLocalizationResource<AppResource>("pt-BR", "en",
            new Dictionary<string, string> { ["Greeting"] = "Hello" });
        services.AddOmniJsonTranslations<AppResource>(
            Encoding.UTF8.GetBytes("{\\\"culture\\\":\\\"pt-BR\\\",\\\"texts\\\":{\\\"Greeting\\\":\\\"Olá\\\"}}"));
        using var provider = services.BuildServiceProvider();
        var localizer = provider.GetRequiredService<IOmniLocalizer<AppResource>>();
        return localizer.Localize("Greeting", CultureInfo.GetCultureInfo("pt-BR")).Value == "Olá" ? 0 : 1;

        internal sealed class AppResource;
        """,
    )
    return project


def create_wasm(root: pathlib.Path, version: str) -> pathlib.Path:
    project = root / "wasm"
    write(
        project / "SmokeWasm.csproj",
        f"""
        <Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <Nullable>enable</Nullable>
            <ImplicitUsings>enable</ImplicitUsings>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="10.0.10" />
            <PackageReference Include="AndersonN.Omni.Blazor" Version="{version}" />
          </ItemGroup>
        </Project>
        """,
    )
    write(
        project / "Program.cs",
        """
        using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
        using Omni.Blazor;
        using PackageSmoke.Wasm;

        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.Services.AddOmniComponents();
        await builder.Build().RunAsync();
        """,
    )
    write(
        project / "App.razor",
        """
        @namespace PackageSmoke.Wasm
        <OmniTheme />
        <OmniAutoComplete TItem="string" Items="_items" TextSelector="item => item" />

        @code { private readonly string[] _items = ["Pacote", "WASM"]; }
        """,
    )
    write(
        project / "_Imports.razor",
        """
        @using Omni.Blazor.Components
        """,
    )
    write(project / "wwwroot" / "index.html", '<div id="app">Loading...</div>')
    return project


def main() -> int:
    if len(sys.argv) not in (2, 3):
        print("usage: smoke_packages.py <artifacts-dir> [expected-version]", file=sys.stderr)
        return 2

    artifacts = pathlib.Path(sys.argv[1]).resolve()
    if not artifacts.is_dir():
        fail(f"Artifact directory does not exist: {artifacts}")
    version = package_version(artifacts, sys.argv[2] if len(sys.argv) == 3 else None)

    with tempfile.TemporaryDirectory(prefix="omni-package-smoke-") as temporary:
        root = pathlib.Path(temporary)
        repository_global_json = pathlib.Path(__file__).resolve().parents[1] / "global.json"
        write(root / "global.json", repository_global_json.read_text(encoding="utf-8"))
        packages = root / "packages"
        config = root / "NuGet.Config"
        write(
            config,
            f"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="omni-artifacts" value="{artifacts.as_posix()}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """,
        )
        env = os.environ.copy()
        env["NUGET_PACKAGES"] = str(packages)
        env["DOTNET_NOLOGO"] = "true"
        env["DOTNET_CLI_TELEMETRY_OPTOUT"] = "true"

        for project in (
            create_localization(root, version),
            create_server(root, version),
            create_wasm(root, version),
        ):
            run(["dotnet", "restore", "--configfile", str(config), "--no-cache"], project, env)
            run(["dotnet", "build", "--configuration", "Release", "--no-restore"], project, env)

        run(["dotnet", "run", "--configuration", "Release", "--no-build"], root / "localization", env)

        tool_dir = root / "tools"
        run(
            [
                "dotnet", "tool", "install", "AndersonN.Omni.Blazor.Mcp",
                "--tool-path", str(tool_dir),
                "--version", version,
                "--configfile", str(config),
            ],
            root,
            env,
        )
        executable = tool_dir / ("omni-blazor-mcp.exe" if os.name == "nt" else "omni-blazor-mcp")
        reported = run([str(executable), "--version"], root, env).strip()
        if reported != version:
            fail(f"MCP tool reported {reported!r}; expected {version!r}")

    print(f"Package smoke passed for Omni.Blazor {version} (standalone localization, Server, WASM, AI, JSON, PO, MCP).")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
