from __future__ import annotations

import sys
import tempfile
import threading
import unittest
import urllib.error
import urllib.request
from functools import partial
from http.server import ThreadingHTTPServer
from pathlib import Path


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from serve_github_pages import GitHubPagesRequestHandler  # noqa: E402


class ServeGitHubPagesTests(unittest.TestCase):
    """The local host must resolve requests exactly like GitHub Pages.

    Verified against the deployed site: an extensionless URL is served from the matching
    `.html` with 200 and no redirect, and only unmatched paths get `404.html` with a 404.
    If this drifts, the browser smoke tests stop proving anything about production.
    """

    def setUp(self) -> None:
        self._directory = tempfile.TemporaryDirectory()
        root = Path(self._directory.name)
        (root / "index.html").write_text("root", encoding="utf-8")
        (root / "404.html").write_text("fallback", encoding="utf-8")
        (root / "showcase.html").write_text("showcase", encoding="utf-8")
        (root / "showcase").mkdir()
        (root / "showcase" / "datagrid.html").write_text("datagrid", encoding="utf-8")
        (root / "docs").mkdir()
        (root / "docs" / "index.html").write_text("docs index", encoding="utf-8")

        handler = partial(
            GitHubPagesRequestHandler, directory=str(root), path_base="/Omni.Blazor"
        )
        self._server = ThreadingHTTPServer(("127.0.0.1", 0), handler)
        self._thread = threading.Thread(target=self._server.serve_forever, daemon=True)
        self._thread.start()
        self._base = f"http://127.0.0.1:{self._server.server_address[1]}/Omni.Blazor"

    def tearDown(self) -> None:
        self._server.shutdown()
        self._server.server_close()
        self._thread.join(timeout=5)
        self._directory.cleanup()

    def _get(self, path: str) -> tuple[int, str]:
        try:
            with urllib.request.urlopen(f"{self._base}{path}") as response:
                return response.status, response.read().decode("utf-8")
        except urllib.error.HTTPError as error:
            return error.code, error.read().decode("utf-8")

    def test_root_is_served(self) -> None:
        self.assertEqual((200, "root"), self._get("/"))

    def test_extensionless_route_resolves_to_the_html_file(self) -> None:
        # the whole point: a pre-rendered route answers 200, not the 404 fallback
        self.assertEqual((200, "showcase"), self._get("/showcase"))
        self.assertEqual((200, "datagrid"), self._get("/showcase/datagrid"))

    def test_directory_index_is_served(self) -> None:
        self.assertEqual((200, "docs index"), self._get("/docs/"))

    def test_unknown_route_falls_back_to_the_app_shell(self) -> None:
        self.assertEqual((404, "fallback"), self._get("/showcase/does-not-exist"))

    def test_paths_outside_the_base_path_are_rejected(self) -> None:
        outside = f"http://127.0.0.1:{self._server.server_address[1]}/index.html"
        with self.assertRaises(urllib.error.HTTPError) as caught:
            urllib.request.urlopen(outside)
        self.assertEqual(404, caught.exception.code)

    def test_explicit_html_file_still_works(self) -> None:
        self.assertEqual((200, "datagrid"), self._get("/showcase/datagrid.html"))


if __name__ == "__main__":
    unittest.main()
