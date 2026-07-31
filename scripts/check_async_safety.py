#!/usr/bin/env python3
"""Reject runtime patterns that commonly cause async deadlocks or lost errors."""

from __future__ import annotations

import pathlib
import re
import sys


ROOTS = (pathlib.Path("src/Omni.Blazor"), pathlib.Path("src/Omni.Blazor.Ai"))
PATTERNS = {
    "async void": re.compile(r"\basync\s+void\b"),
    "Task.Result": re.compile(r"\.Result\b"),
    "Task.Wait": re.compile(r"\.Wait\s*\("),
    "GetAwaiter().GetResult()": re.compile(
        r"\.GetAwaiter\s*\(\s*\)\s*\.GetResult\s*\(\s*\)"
    ),
    "async Timer callback": re.compile(
        r"new\s+(?:System\.Threading\.)?Timer\s*\(\s*async\b"
    ),
    "discarded async work": re.compile(
        r"_\s*=(?!>)\s*.*(?:Async|Invoke\w*Async)\s*\("
    ),
    "unobserved InvokeAsync statement": re.compile(
        r"^\s*(?:[A-Za-z_]\w*\.)?Invoke\w*Async\s*\(.*\);\s*(?://.*)?$"
    ),
}

ALLOWED = {
    (pathlib.Path("src/Omni.Blazor/Utilities/TaskObserver.cs"), "discarded async work"),
}


def main() -> int:
    findings: list[str] = []
    for root in ROOTS:
        for path in root.rglob("*"):
            if path.suffix not in {".cs", ".razor"}:
                continue
            if any(part in {"bin", "obj", "wwwroot"} for part in path.parts):
                continue

            for line_number, line in enumerate(
                path.read_text(encoding="utf-8-sig").splitlines(), start=1
            ):
                for label, pattern in PATTERNS.items():
                    if pattern.search(line) and (path, label) not in ALLOWED:
                        findings.append(f"{path}:{line_number}: {label}")

    if not findings:
        print("Async safety gate passed.")
        return 0

    print("Async safety gate failed:", file=sys.stderr)
    for finding in findings:
        print(f"  - {finding}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
