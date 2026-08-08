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
    """Map the repository base path and use 404.html as the SPA fallback."""

    def __init__(self, *args: object, path_base: str, **kwargs: object) -> None:
        self.path_base = normalize_base_path(path_base).rstrip("/")
        super().__init__(*args, **kwargs)

    def do_GET(self) -> None:  # noqa: N802 - BaseHTTPRequestHandler API
        request_path = self._artifact_path()
        if request_path is None:
            self.send_error(HTTPStatus.NOT_FOUND)
            return

        self.path = request_path
        if Path(self.translate_path(self.path)).exists():
            super().do_GET()
            return

        self._serve_spa_fallback()

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
