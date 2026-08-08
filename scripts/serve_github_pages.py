#!/usr/bin/env python3
"""Serve a prepared GitHub Pages artifact locally with project-site semantics."""

from __future__ import annotations

import argparse
from functools import partial
from http import HTTPStatus
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from urllib.parse import urlsplit, urlunsplit

from prepare_github_pages import normalize_base_path


class GitHubPagesRequestHandler(SimpleHTTPRequestHandler):
    """Map the repository base path and resolve requests the way GitHub Pages does.

    Resolution order mirrors the real host so the browser tests are faithful: the literal
    file, then ``<path>.html`` (GitHub Pages serves extensionless URLs from the matching
    ``.html`` with 200 and no redirect — that is what makes the pre-rendered route pages
    work), then a directory index, and only then the ``404.html`` fallback with a 404.
    """

    def __init__(self, *args: object, path_base: str, **kwargs: object) -> None:
        self.path_base = normalize_base_path(path_base).rstrip("/")
        super().__init__(*args, **kwargs)

    def do_GET(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler API
        request_path = self._artifact_path()
        if request_path is None:
            self.send_error(HTTPStatus.NOT_FOUND)
            return

        resolved = self._resolve(request_path)
        if resolved is None:
            self._serve_spa_fallback()
            return

        self.path = resolved
        super().do_GET()

    def _resolve(self, request_path: str) -> str | None:
        """Return the artifact path to serve, or None when nothing matches."""
        parsed = urlsplit(request_path)
        candidates = [parsed.path]
        if not parsed.path.endswith("/"):
            candidates.append(f"{parsed.path}.html")

        for candidate in candidates:
            target = Path(self.translate_path(candidate))
            if target.is_file():
                return urlunsplit(("", "", candidate, parsed.query, ""))
            if target.is_dir() and (target / "index.html").is_file():
                return urlunsplit(("", "", f"{candidate.rstrip('/')}/", parsed.query, ""))
        return None

    def _artifact_path(self) -> str | None:
        parsed = urlsplit(self.path)
        path = parsed.path
        if self.path_base:
            if path == self.path_base:
                path += "/"
            prefix = f"{self.path_base}/"
            if not path.startswith(prefix):
                return None
            path = path[len(self.path_base) :]

        return urlunsplit(("", "", path or "/", parsed.query, ""))

    def _serve_spa_fallback(self) -> None:
        fallback = Path(self.directory) / "404.html"
        if not fallback.is_file():
            self.send_error(HTTPStatus.NOT_FOUND)
            return

        content = fallback.read_bytes()
        self.send_response(HTTPStatus.NOT_FOUND)
        self.send_header("Content-Type", "text/html; charset=utf-8")
        self.send_header("Content-Length", str(len(content)))
        self.end_headers()
        self.wfile.write(content)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--directory", required=True, type=Path)
    parser.add_argument("--path-base", default="/")
    parser.add_argument("--bind", default="127.0.0.1")
    parser.add_argument("--port", required=True, type=int)
    args = parser.parse_args()

    directory = args.directory.resolve(strict=True)
    handler = partial(
        GitHubPagesRequestHandler,
        directory=str(directory),
        path_base=args.path_base,
    )
    server = ThreadingHTTPServer((args.bind, args.port), handler)
    print(
        f"Serving {directory} at http://{args.bind}:{args.port}"
        f"{normalize_base_path(args.path_base)}",
        flush=True,
    )
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
