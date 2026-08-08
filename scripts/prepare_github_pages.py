#!/usr/bin/env python3
"""Prepare a published standalone Blazor WebAssembly app for GitHub Pages."""

from __future__ import annotations

import argparse
import re
import shutil
from pathlib import Path


BASE_TAG = re.compile(r'<base\s+href=(["\'])[^"\']*\1\s*/?>', re.IGNORECASE)


def normalize_base_path(value: str) -> str:
    """Return an absolute site path with the trailing slash required by Blazor."""
    base_path = value.strip()
    if not base_path:
        return "/"
    if "?" in base_path or "#" in base_path:
        raise ValueError("The GitHub Pages base path cannot contain a query or fragment.")
    return f"/{base_path.strip('/')}/"


def prepare(publish_directory: Path, base_path: str) -> None:
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

    print(
        f"Prepared GitHub Pages artifact at {publish_directory} "
        f"with base path {normalized_base_path}"
    )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("publish_directory", type=Path)
    parser.add_argument("base_path", help="GitHub Pages base path, for example /Omni.Blazor")
    args = parser.parse_args()
    prepare(args.publish_directory, args.base_path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
