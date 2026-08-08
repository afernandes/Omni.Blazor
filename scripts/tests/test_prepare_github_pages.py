from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path


sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from prepare_github_pages import (  # noqa: E402
    discover_routes,
    normalize_base_path,
    prepare,
)


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

    def test_discover_routes_reads_page_directives(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            source = Path(directory)
            (source / "nested").mkdir()
            (source / "Landing.razor").write_text('@page "/"\n<h1>hi</h1>', encoding="utf-8")
            (source / "nested" / "Deep.razor").write_text(
                '@page "/showcase/get-started"\n@page "/showcase/start"\n', encoding="utf-8"
            )
            (source / "Ignored.txt").write_text('@page "/nope"', encoding="utf-8")

            self.assertEqual(
                {"/", "/showcase/get-started", "/showcase/start"}, discover_routes([source])
            )

    def test_prepare_pre_renders_static_routes_so_deep_links_answer_200(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            publish_directory = root / "wwwroot"
            publish_directory.mkdir()
            (publish_directory / "index.html").write_text(
                '<base href="/" />', encoding="utf-8"
            )
            source = root / "src"
            source.mkdir()
            (source / "Pages.razor").write_text(
                '@page "/"\n@page "/showcase"\n@page "/showcase/get-started"\n'
                '@page "/orders/{id}"\n',
                encoding="utf-8",
            )

            prepare(publish_directory, "/Omni.Blazor", [source])

            expected = '<base href="/Omni.Blazor/" />'
            # a route gets its own page, so GitHub Pages serves it with 200
            self.assertEqual(
                expected, (publish_directory / "showcase.html").read_text(encoding="utf-8")
            )
            self.assertEqual(
                expected,
                (publish_directory / "showcase" / "get-started.html").read_text(encoding="utf-8"),
            )
            # "/" is index.html, and a parameterised route cannot be enumerated
            self.assertFalse((publish_directory / "orders").exists())
            # the fallback still exists for everything not pre-rendered
            self.assertTrue((publish_directory / "404.html").is_file())

    def test_prepare_never_shadows_a_published_asset(self) -> None:
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            publish_directory = root / "wwwroot"
            publish_directory.mkdir()
            (publish_directory / "index.html").write_text('<base href="/" />', encoding="utf-8")
            (publish_directory / "docs.html").write_text("real asset", encoding="utf-8")
            source = root / "src"
            source.mkdir()
            (source / "Pages.razor").write_text('@page "/docs"', encoding="utf-8")

            prepare(publish_directory, "/Omni.Blazor", [source])

            self.assertEqual(
                "real asset", (publish_directory / "docs.html").read_text(encoding="utf-8")
            )

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
