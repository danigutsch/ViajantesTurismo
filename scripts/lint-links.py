#!/usr/bin/env python3

from __future__ import annotations

import argparse
import os
import re
import sys
import urllib.parse
from collections import Counter
from pathlib import Path


ROOT = Path.cwd()

DEFAULT_DIRS = (
    ".devcontainer",
    ".github",
    "benchmarks",
    "docs",
    "samples",
    "src",
    "tests",
)

ROOT_MARKDOWN_FILES = (
    "AGENTS.md",
    "CONTRIBUTING.md",
    "README.md",
    "SECURITY.md",
)

ROOT_TEXT_FILES = ("LICENSE.txt",)

EXCLUDED_PARTS = {
    ".git",
    ".worktrees",
    "bin",
    "obj",
    "TestResults",
}

EXCLUDED_RELATIVE_PREFIXES = (".github/ISSUE_TEMPLATE/",)

EXCLUDED_RELATIVE_NAMES = {
    "docs/pull_request_template.md",
}

EXCLUDED_RELATIVE_PATTERNS = (
    re.compile(r"^docs/GENERATED[^/]*\.md$"),
    re.compile(r"^tests/ISSUE-[^/]*\.md$"),
)

INLINE_LINK = re.compile(
    r"!??\[[^\]\n]*(?:\][^\]\n]*)*\]\(([^)\s]+)(?:\s+\"[^\"]*\")?\)"
)
REFERENCE_DEFINITION = re.compile(r"^\s{0,3}\[[^\]]+\]:\s+(\S+)")
AUTOLINK = re.compile(r'<([^<>"]+)>')
GITHUB_ISSUE_OR_PR_URL = re.compile(
    r'https?://github\.com/[^\s)>"]+/[^\s)>"]+/(?:issues|pull)/\d+\b'
)
REPO_RELATIVE_ISSUE_OR_PR = re.compile(r"(?:^|[\s(])(?:\.{0,2}/)*(?:issues|pull)/\d+\b")


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate maintained documentation links without transient package execution."
    )
    parser.add_argument(
        "paths", nargs="*", help="Optional files or directories to scan."
    )
    args = parser.parse_args()

    files = list(iter_markdown_files(args.paths))
    errors: list[str] = []

    for path in files:
        relative = path.relative_to(ROOT).as_posix()
        text = path.read_text(encoding="utf-8")
        lines = text.splitlines()
        anchors = markdown_anchors(text)

        for line_number, line in iter_non_fenced_lines(lines):
            if GITHUB_ISSUE_OR_PR_URL.search(line):
                errors.append(
                    f"{relative}:{line_number}: direct GitHub issue/PR link is not allowed; "
                    "summarize durable context or link a maintained doc instead"
                )
            if REPO_RELATIVE_ISSUE_OR_PR.search(line):
                errors.append(
                    f"{relative}:{line_number}: repo-relative issue/PR link is not allowed; "
                    "summarize durable context or link a maintained doc instead"
                )

        for line_number, target in extract_link_targets(lines):
            parsed_target = normalize_target(target)
            if not parsed_target or is_external_or_special(parsed_target):
                continue

            target_path, fragment = split_fragment(parsed_target)
            base = path.parent
            resolved = (
                (base / urllib.parse.unquote(target_path)).resolve()
                if target_path
                else path.resolve()
            )

            if not is_relative_to(resolved, ROOT):
                errors.append(
                    f"{relative}:{line_number}: link escapes repository: {target}"
                )
                continue

            if not resolved.exists():
                errors.append(
                    f"{relative}:{line_number}: missing local link target: {target}"
                )
                continue

            if fragment and resolved.suffix.lower() == ".md":
                target_anchors = (
                    anchors
                    if resolved == path.resolve()
                    else markdown_anchors(resolved.read_text(encoding="utf-8"))
                )
                decoded_fragment = urllib.parse.unquote(fragment).lower()
                if decoded_fragment not in target_anchors:
                    errors.append(
                        f"{relative}:{line_number}: missing Markdown anchor '{fragment}' in {target_path or relative}"
                    )

    if errors:
        print("Link validation failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print(f"Link validation passed for {len(files)} documentation files.")
    return 0


def iter_markdown_files(paths: list[str]):
    candidates: list[Path] = []
    if paths:
        for value in paths:
            path = (ROOT / value).resolve()
            if path.is_dir():
                candidates.extend(path.rglob("*.md"))
            elif path.suffix.lower() in {".md", ".txt"}:
                candidates.append(path)
    else:
        for name in ROOT_MARKDOWN_FILES:
            path = ROOT / name
            if path.exists():
                candidates.append(path)
        for name in ROOT_TEXT_FILES:
            path = ROOT / name
            if path.exists():
                candidates.append(path)
        for dirname in DEFAULT_DIRS:
            path = ROOT / dirname
            if path.exists():
                candidates.extend(path.rglob("*.md"))

    for path in sorted({candidate.resolve() for candidate in candidates}):
        if should_scan(path):
            yield path


def should_scan(path: Path) -> bool:
    try:
        relative = path.relative_to(ROOT).as_posix()
    except ValueError:
        return False

    if any(part in EXCLUDED_PARTS for part in Path(relative).parts):
        return False
    if any(relative.startswith(prefix) for prefix in EXCLUDED_RELATIVE_PREFIXES):
        return False
    if relative in EXCLUDED_RELATIVE_NAMES:
        return False
    return not any(pattern.match(relative) for pattern in EXCLUDED_RELATIVE_PATTERNS)


def extract_link_targets(lines: list[str]):
    in_fence = False
    fence_marker = ""
    for line_number, line in enumerate(lines, start=1):
        stripped = line.lstrip()
        if stripped.startswith(("```", "~~~")):
            marker = stripped[:3]
            if not in_fence:
                in_fence = True
                fence_marker = marker
            elif marker == fence_marker:
                in_fence = False
            continue
        if in_fence:
            continue

        for match in INLINE_LINK.finditer(line):
            yield line_number, match.group(1)
        reference_match = REFERENCE_DEFINITION.match(line)
        if reference_match:
            yield line_number, reference_match.group(1)
        for match in AUTOLINK.finditer(line):
            target = match.group(1)
            if "://" in target or target.startswith("#"):
                yield line_number, target


def iter_non_fenced_lines(lines: list[str]):
    in_fence = False
    fence_marker = ""
    for line_number, line in enumerate(lines, start=1):
        stripped = line.lstrip()
        if stripped.startswith(("```", "~~~")):
            marker = stripped[:3]
            if not in_fence:
                in_fence = True
                fence_marker = marker
            elif marker == fence_marker:
                in_fence = False
            continue
        if not in_fence:
            yield line_number, line


def normalize_target(target: str) -> str:
    return target.strip().strip("<>").strip()


def is_external_or_special(target: str) -> bool:
    lower = target.lower()
    return (
        "://" in lower
        or lower.startswith("mailto:")
        or lower.startswith("tel:")
        or lower.startswith("urn:")
    )


def split_fragment(target: str) -> tuple[str, str]:
    path, separator, fragment = target.partition("#")
    return path, fragment if separator else ""


def markdown_anchors(text: str) -> set[str]:
    anchors: set[str] = set()
    counts: Counter[str] = Counter()
    in_fence = False
    fence_marker = ""

    for line in text.splitlines():
        stripped = line.lstrip()
        if stripped.startswith(("```", "~~~")):
            marker = stripped[:3]
            if not in_fence:
                in_fence = True
                fence_marker = marker
            elif marker == fence_marker:
                in_fence = False
            continue
        if in_fence or not stripped.startswith("#"):
            continue
        heading = stripped.lstrip("#").strip()
        if not heading:
            continue
        slug = slugify(heading)
        counts[slug] += 1
        anchors.add(slug if counts[slug] == 1 else f"{slug}-{counts[slug] - 1}")

    return anchors


def slugify(value: str) -> str:
    value = re.sub(r"<[^>]+>", "", value)
    value = re.sub(r"[`*_~\[\]()]", "", value)
    value = value.lower().strip()
    value = re.sub(r"[^a-z0-9 _.-]", "", value)
    value = re.sub(r"\s", "-", value)
    return value.strip("-")


def is_relative_to(path: Path, parent: Path) -> bool:
    try:
        path.relative_to(parent)
        return True
    except ValueError:
        return False


if __name__ == "__main__":
    sys.exit(main())
