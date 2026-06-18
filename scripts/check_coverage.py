#!/usr/bin/env python3
"""Fail the build if any target assembly's line coverage is below a threshold.

Usage: check_coverage.py <min_percent> <coverage_dir> <assembly> [<assembly> ...]

Reads every coverage.cobertura.xml under <coverage_dir>. An assembly can be
instrumented by more than one test project (e.g. the library is loaded by the
manifest-gen tests too), so per assembly we keep the report with the MOST covered
lines — its dedicated test project. Prints a per-assembly summary and exits
non-zero if any is below the threshold.
"""
import glob
import sys
import xml.etree.ElementTree as ET


def main() -> int:
    threshold = float(sys.argv[1])
    cov_dir = sys.argv[2]
    targets = sys.argv[3:]

    files = glob.glob(f"{cov_dir}/**/coverage.cobertura.xml", recursive=True)
    if not files:
        print(f"::error::no coverage.cobertura.xml found under '{cov_dir}'")
        return 1

    best: dict[str, tuple[int, int]] = {}  # assembly -> (covered, total)
    for path in files:
        root = ET.parse(path).getroot()
        for pkg in root.iter("package"):
            name = pkg.get("name")
            if name not in targets:
                continue
            lines = [ln for cls in pkg.iter("class") for ln in cls.findall(".//line")]
            total = len(lines)
            covered = sum(1 for ln in lines if int(ln.get("hits", "0")) > 0)
            if name not in best or covered > best[name][0]:
                best[name] = (covered, total)

    ok = True
    for name in targets:
        if name not in best or best[name][1] == 0:
            print(f"::error::no coverage found for assembly '{name}'")
            ok = False
            continue
        covered, total = best[name]
        pct = 100.0 * covered / total
        if pct >= threshold:
            print(f"OK   {name}: {pct:.2f}% ({covered}/{total})  [min {threshold}%]")
        else:
            print(f"FAIL {name}: {pct:.2f}% ({covered}/{total})  [min {threshold}%]")
            print(f"::error::{name} line coverage {pct:.2f}% is below the {threshold}% minimum")
            ok = False

    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
