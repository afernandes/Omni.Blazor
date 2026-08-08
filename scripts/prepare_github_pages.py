#!/usr/bin/env python3
"""Prepare a published standalone Blazor WebAssembly app for GitHub Pages.

GitHub Pages is a plain static host: it has no SPA rewrite, so a deep link like
``/showcase/get-started`` has no file behind it. The usual `404.html` fallback makes the
app *render*, but every deep link and every refresh still answers **HTTP 404** — which
browsers and CDNs may cache, crawlers honour, and monitoring flags.

So besides the fallback we emit one real page per client route: route
``/showcase/get-started`` becomes ``showcase/get-started.html``. GitHub Pages resolves
extensionless URLs to the matching ``.html`` file (verified: it serves ``404.html`` for
``/404`` with **200** and no redirect), so those links now answer 200 with the URL intact
and Blazor's router takes it from there. ``404.html`` stays as the fallback for anything
not pre-rendered (typos, and parameterised routes, which cannot be enumerated).
"""

from __future__ import annotations

import argparse
import re
import shutil
from pathlib import Path


BASE_TAG = re.compile(r'<base\s+href=(["\'])[^"\']*\1\s*/?>', re.IGNORECASE)
PAGE_DIRECTIVE = re.compile(r'^\s*@page\s+"(?P<route>[^"]+)"', re.MULTILINE)


def discover_routes(source_directories: list[Path]) -> set[str]:
    """Collect every ``@page`` route declared in the given Razor source trees."""
    routes: set[str] = set()
    for directory in source_directories:
        directory = directory.resolve(strict=True)
        for razor in directory.rglob("*.razor"):
            content = razor.read_text(encoding="utf-8")
            routes.update(match.group("route") for match in PAGE_DIRECTIVE.finditer(content))
    return routes


def write_route_pages(publish_directory: Path, host_page: str, routes: set[str]) -> list[str]:
    """Write ``<route>.html`` for each static route. Returns the routes written."""
    written: list[str] = []
    for route in sorted(routes):
        relative = route.strip("/")
        if not relative:
            continue  # "/" is already index.html
        if "{" in route:
            continue  # parameterised route — falls back to 404.html
        target = publish_directory / f"{relative}.html"
        if target.exists():
            continue  # never shadow a real published asset
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(host_page, encoding="utf-8", newline="\n")
        written.append(route)
    return written


def normalize_base_path(value: str) -> str:
    """Return an absolute site path with the trailing slash required by Blazor."""
    base_path = value.strip()
    if not base_path:
        return "/"
    if "?" in base_path or "#" in base_path:
        raise ValueError("The GitHub Pages base path cannot contain a query or fragment.")
    return f"/{base_path.strip('/')}/"


def prepare(
    publish_directory: Path,
    base_path: str,
    route_sources: list[Path] | None = None,
) -> None:
    """Rewrite the host page and add the files required by project-site hosting."""
    publish_directory = publish_directory.resolve(strict=True)
    index_path = publish_directory / "index.html"
    if not index_path.is_file():
        raise FileNotFoundError(f"Published index.html not found: {index_path}")

    normalized_base_path = normalize_base_path(base_path)
    index_html = index_path.read_text(encoding="utf-8")
    rewritten_html, replacements = BASE_TAG.subn(
        f'<base href="{normalized_base_path}" />', index_html
    )
    if replacements != 1:
        raise ValueError(
            f"Expected exactly one <base href> in {index_path}, found {replacements}."
        )

    index_path.write_text(rewritten_html, encoding="utf-8", newline="\n")
    shutil.copyfile(index_path, publish_directory / "404.html")
    (publish_directory / ".nojekyll").touch()

    routes = discover_routes(route_sources) if route_sources else set()
    written = write_route_pages(publish_directory, rewritten_html, routes)

    print(
        f"Prepared GitHub Pages artifact at {publish_directory} "
        f"with base path {normalized_base_path}"
    )
    if route_sources:
        print(f"Pre-rendered {len(written)} client routes (of {len(routes)} discovered).")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("publish_directory", type=Path)
    parser.add_argument("base_path", help="GitHub Pages base path, for example /Omni.Blazor")
    parser.add_argument(
        "--routes-from",
        type=Path,
        action="append",
        default=None,
        dest="route_sources",
        metavar="DIR",
        help="Razor source tree to scan for @page routes; repeatable. "
        "Each static route gets its own .html so deep links answer 200 instead of 404.",
    )
    args = parser.parse_args()
    prepare(args.publish_directory, args.base_path, args.route_sources)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
