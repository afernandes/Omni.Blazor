from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from prepare_github_pages import normalize_base_path, prepare  # noqa: E402


class PrepareGitHubPagesTests(unittest.TestCase):
    def test_normalize_base_path_adds_required_slashes(self) -> None:
        self.assertEqual("/", normalize_base_path(""))
        self.assertEqual("/Omni.Blazor/", normalize_base_path("Omni.Blazor"))
        self.assertEqual("/Omni.Blazor/", normalize_base_path("/Omni.Blazor/"))

    def test_prepare_rewrites_base_and_creates_static_host_files(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            publish_directory = Path(directory)
            index_path = publish_directory / "index.html"
            index_path.write_text(
                '<!doctype html><html><head><base href="/" /></head></html>',
                encoding="utf-8",
            )

            prepare(publish_directory, "/Omni.Blazor")

            expected = (
                '<!doctype html><html><head><base href="/Omni.Blazor/" /></head></html>'
            )
            self.assertEqual(expected, index_path.read_text(encoding="utf-8"))
            self.assertEqual(
                expected, (publish_directory / "404.html").read_text(encoding="utf-8")
            )
            self.assertTrue((publish_directory / ".nojekyll").is_file())

    def test_prepare_rejects_an_ambiguous_host_page(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            publish_directory = Path(directory)
            (publish_directory / "index.html").write_text(
                '<base href="/" /><base href="/other/" />', encoding="utf-8"
            )

            with self.assertRaisesRegex(ValueError, "exactly one"):
                prepare(publish_directory, "/Omni.Blazor")


if __name__ == "__main__":
    unittest.main()
