#!/usr/bin/env python3
"""Verify that relative links in tracked markdown files point at real files.

External URLs are not fetched — this check is offline and deterministic so it
can never fail from network flakiness. Vendored packages are skipped.
"""
import re
import subprocess
import sys
from pathlib import Path

SKIP_PREFIXES = ("http://", "https://", "mailto:", "#")
SKIP_DIRS = (
    "Packages/",
    "Library/",
    "node_modules/",
    # Vendored third-party SDK folders under Assets — their docs ship with
    # broken relative links we don't control.
    "Assets/LevelPlay/",
    "Assets/MobileDependencyResolver/",
)

LINK_RE = re.compile(r"\[[^\]]*\]\(([^)\s]+)(?:\s+\"[^\"]*\")?\)")
FENCE_RE = re.compile(r"```.*?```", re.DOTALL)
INLINE_CODE_RE = re.compile(r"`[^`\n]*`")

files = subprocess.run(
    ["git", "ls-files", "*.md"], capture_output=True, text=True, check=True
).stdout.splitlines()

broken = []
for name in files:
    if name.startswith(SKIP_DIRS):
        continue
    md = Path(name)
    text = md.read_text(encoding="utf-8", errors="replace")
    text = FENCE_RE.sub("", text)
    text = INLINE_CODE_RE.sub("", text)
    for target in LINK_RE.findall(text):
        if target.startswith(SKIP_PREFIXES):
            continue
        path = target.split("#", 1)[0]
        if not path:
            continue
        if not (md.parent / path).exists():
            broken.append(f"{name}: broken relative link -> {target}")

if broken:
    print("\n".join(broken))
    sys.exit(1)
print(f"OK: checked {len(files)} markdown files, no broken relative links.")
