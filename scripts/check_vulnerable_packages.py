#!/usr/bin/env python3
"""Fail CI when `dotnet list package --vulnerable --format json` reports findings."""

from __future__ import annotations

import json
import pathlib
import sys
from typing import Any


def collect_findings(node: Any, context: tuple[str, ...] = ()) -> list[str]:
    findings: list[str] = []
    if isinstance(node, dict):
        package_id = str(node.get("id") or node.get("name") or "")
        next_context = context + ((package_id,) if package_id else ())
        vulnerabilities = node.get("vulnerabilities")
        if isinstance(vulnerabilities, list) and vulnerabilities:
            for vulnerability in vulnerabilities:
                if not isinstance(vulnerability, dict):
                    findings.append(" / ".join(next_context) or "unknown package")
                    continue
                severity = vulnerability.get("severity", "unknown severity")
                advisory = vulnerability.get("advisoryurl", "no advisory URL")
                package = " / ".join(next_context) or "unknown package"
                findings.append(f"{package}: {severity} ({advisory})")

        for key, value in node.items():
            if key != "vulnerabilities":
                findings.extend(collect_findings(value, next_context))
    elif isinstance(node, list):
        for item in node:
            findings.extend(collect_findings(item, context))
    return findings


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: check_vulnerable_packages.py <dotnet-list-json>", file=sys.stderr)
        return 2

    report_path = pathlib.Path(sys.argv[1])
    report = json.loads(report_path.read_text(encoding="utf-8-sig"))
    findings = collect_findings(report)
    if not findings:
        print("NuGet vulnerability gate passed: no vulnerable packages reported.")
        return 0

    print("NuGet vulnerability gate failed:", file=sys.stderr)
    for finding in findings:
        print(f"  - {finding}", file=sys.stderr)
    return 1


if __name__ == "__main__":
    raise SystemExit(main())
