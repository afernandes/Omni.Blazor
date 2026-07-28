#!/usr/bin/env python3
"""Guard the Omni.Templates gallery against copy-paste drift.

The gallery's promise is "copy the code, paste it into your project". The copyable
block (the `_code` string) is a hand-maintained duplicate of the live markup, so it
drifts: it has shipped classes that exist in no stylesheet, leaving consumers with
silently broken layout.

Two rules are enforced:

1. COPYABLE CODE (`_code` strings) may only reference CSS classes that ship in the
   library bundle (omni.css). A consumer has the library — they do NOT have the
   gallery's own templates.css. Anything else must be an inline style.
2. LIVE MARKUP may additionally use the gallery's own `tpl-*` classes (templates.css),
   but nothing undefined.

Usage: python scripts/check_template_code.py
Exit code 0 when clean, 1 on the first violation found.
"""
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parent.parent
OMNI_CSS = REPO / "src/Omni.Blazor/wwwroot/css/omni.css"
TPL_CSS = REPO / "src/Omni.Templates/Omni.Templates/wwwroot/css/templates.css"
TPL_SCSS = REPO / "src/Omni.Templates/Omni.Templates/Themes/templates.scss"
TEMPLATES = REPO / "src/Omni.Templates/Omni.Templates/Pages"

# Blazor/utility classes applied by components at runtime rather than authored in CSS.
IGNORE = {"active", "disabled", "selected", "open", "show", "hide"}


def classes_in(path: pathlib.Path) -> set[str]:
    if not path.exists():
        return set()
    return set(re.findall(r"\.([a-zA-Z][\w-]*)", path.read_text(encoding="utf-8")))


def main() -> int:
    if not OMNI_CSS.exists():
        print(f"::error::{OMNI_CSS} not found — build the library first "
              "(dotnet build src/Omni.Blazor/Omni.Blazor.csproj)")
        return 1

    shipped = classes_in(OMNI_CSS)
    gallery = classes_in(TPL_CSS) | classes_in(TPL_SCSS)

    violations: list[str] = []
    for f in sorted(TEMPLATES.rglob("*.razor")):
        src = f.read_text(encoding="utf-8")
        rel = f.relative_to(REPO).as_posix()

        # Rule 1 — copyable code: only library-bundle classes.
        for block in re.finditer(r'_code\s*=\s*@"(.*?)";\s*$', src, re.S | re.M):
            for m in re.finditer(r'class=""([^"]*)""', block.group(1)):
                for cls in m.group(1).split():
                    if cls not in shipped and cls not in IGNORE:
                        violations.append(
                            f"{rel}: copyable code uses '{cls}', which does not ship in omni.css. "
                            "Consumers do not have the gallery stylesheet — use an inline style "
                            "or a class from the library bundle.")

        # Rule 2 — live markup: library bundle or the gallery's own stylesheet.
        live = re.sub(r'_code\s*=\s*@".*?";\s*$', "", src, flags=re.S | re.M)
        for m in re.finditer(r'class="([^"@]*)"', live):
            for cls in m.group(1).split():
                if cls not in shipped and cls not in gallery and cls not in IGNORE:
                    violations.append(
                        f"{rel}: live markup uses '{cls}', which is defined nowhere "
                        "(neither omni.css nor templates.scss).")

    if violations:
        for v in sorted(set(violations)):
            print(f"::error::{v}")
        print(f"\nFAIL {len(set(violations))} template drift violation(s)")
        return 1

    print("OK  templates: copyable code and live markup only use shipped CSS classes")
    return 0


if __name__ == "__main__":
    sys.exit(main())
