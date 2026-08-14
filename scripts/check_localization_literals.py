#!/usr/bin/env python3
"""Fail when library Razor markup introduces obvious hard-coded UI text."""

from __future__ import annotations

import pathlib
import re
import sys


ROOT = pathlib.Path(__file__).resolve().parents[1]
COMPONENTS = ROOT / "src" / "Omni.Blazor" / "Components"
RAZOR_COMMENT = re.compile(r"@\*.*?\*@", re.DOTALL)
HTML_COMMENT = re.compile(r"<!--.*?-->", re.DOTALL)
VISIBLE_TEXT = re.compile(r">([^<>]+)<")
LITERAL_ATTRIBUTE = re.compile(
    r"\b(aria-label|placeholder|title)\s*=\s*\"([^\"@]+)\"", re.IGNORECASE
)
WORDS = re.compile(r"[^\W\d_]", re.UNICODE)


def line_number(source: str, offset: int) -> int:
    return source.count("\n", 0, offset) + 1


def main() -> int:
    findings: list[str] = []
    for path in sorted(COMPONENTS.rglob("*.razor")):
        source = path.read_text(encoding="utf-8")
        clean = HTML_COMMENT.sub("", RAZOR_COMMENT.sub("", source))
        for code_marker in ("@code", "@functions"):
            clean = clean.split(code_marker, 1)[0]

        for match in VISIBLE_TEXT.finditer(clean):
            value = " ".join(match.group(1).split())
            if (
                not value
                or "@" in value
                or not WORDS.search(value)
                or any(token in value for token in ("{", "}", ";", "="))
                or value.casefold() in {"esc"}
                or re.search(r"\b(if|else|foreach|case|break|var|switch)\b", value)
            ):
                continue
            findings.append(
                f"{path.relative_to(ROOT)}:{line_number(clean, match.start(1))}: visible text {value!r}"
            )

        for match in LITERAL_ATTRIBUTE.finditer(clean):
            value = " ".join(match.group(2).split())
            if not value or not WORDS.search(value):
                continue
            findings.append(
                f"{path.relative_to(ROOT)}:{line_number(clean, match.start(2))}: "
                f"literal {match.group(1)}={value!r}"
            )

    if findings:
        print("Hard-coded user-facing strings found in Omni.Blazor Razor markup:", file=sys.stderr)
        print("\n".join(findings), file=sys.stderr)
        print("Use OmniTexts or an explicit localizable component parameter.", file=sys.stderr)
        return 1

    print("Localization literal gate passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
