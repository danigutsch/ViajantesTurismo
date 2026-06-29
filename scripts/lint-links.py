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

REFERENCE_DEFINITION = re.compile(r"^\s{0,3}\[[^\]]+\]:\s+(\S+)")
AUTOLINK = re.compile(r'<([^<>"]+)>')
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
        errors.extend(validate_file(path))

    if errors:
        print_errors(errors)
        return 1

    print(f"Link validation passed for {len(files)} documentation files.")
    return 0


def validate_file(path: Path) -> list[str]:
    relative = path.relative_to(ROOT).as_posix()
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    anchors = markdown_anchors(text)
    errors = validate_policy_lines(relative, lines)

    for line_number, target in extract_link_targets(lines):
        error = validate_link_target(path, relative, anchors, line_number, target)
        if error:
            errors.append(error)

    return errors


def validate_policy_lines(relative: str, lines: list[str]) -> list[str]:
    errors: list[str] = []
    for line_number, line in iter_non_fenced_lines(lines):
        if has_github_issue_or_pr_url(line):
            errors.append(
                f"{relative}:{line_number}: direct GitHub issue/PR link is not allowed; "
                "summarize durable context or link a maintained doc instead"
            )
        if REPO_RELATIVE_ISSUE_OR_PR.search(line):
            errors.append(
                f"{relative}:{line_number}: repo-relative issue/PR link is not allowed; "
                "summarize durable context or link a maintained doc instead"
            )
    return errors


def validate_link_target(
    path: Path,
    relative: str,
    anchors: set[str],
    line_number: int,
    target: str,
) -> str:
    parsed_target = normalize_target(target)
    if not parsed_target or is_external_or_special(parsed_target):
        return ""

    target_path, fragment = split_fragment(parsed_target)
    resolved = resolve_target(path, target_path)
    if not is_relative_to(resolved, ROOT):
        return f"{relative}:{line_number}: link escapes repository: {target}"
    if not resolved.exists():
        return f"{relative}:{line_number}: missing local link target: {target}"
    if not fragment or resolved.suffix.lower() != ".md":
        return ""

    target_anchors = (
        anchors
        if resolved == path.resolve()
        else markdown_anchors(resolved.read_text(encoding="utf-8"))
    )
    decoded_fragment = urllib.parse.unquote(fragment).lower()
    if decoded_fragment in target_anchors:
        return ""
    return f"{relative}:{line_number}: missing Markdown anchor '{fragment}' in {target_path or relative}"


def resolve_target(path: Path, target_path: str) -> Path:
    if target_path:
        return (path.parent / urllib.parse.unquote(target_path)).resolve()
    return path.resolve()


def print_errors(errors: list[str]) -> None:
    print("Link validation failed:", file=sys.stderr)
    for error in errors:
        print(f"- {error}", file=sys.stderr)


def iter_markdown_files(paths: list[str]):
    candidates = collect_candidate_files(paths)
    for path in sorted({candidate.resolve() for candidate in candidates}):
        if should_scan(path):
            yield path


def collect_candidate_files(paths: list[str]) -> list[Path]:
    if paths:
        return collect_requested_files(paths)
    return collect_default_files()


def collect_requested_files(paths: list[str]) -> list[Path]:
    candidates: list[Path] = []
    for value in paths:
        path = (ROOT / value).resolve()
        if path.is_dir():
            candidates.extend(path.rglob("*.md"))
        elif path.suffix.lower() in {".md", ".txt"}:
            candidates.append(path)
    return candidates


def collect_default_files() -> list[Path]:
    candidates: list[Path] = []
    candidates.extend(existing_root_files(ROOT_MARKDOWN_FILES))
    candidates.extend(existing_root_files(ROOT_TEXT_FILES))
    for dirname in DEFAULT_DIRS:
        path = ROOT / dirname
        if path.exists():
            candidates.extend(path.rglob("*.md"))
    return candidates


def existing_root_files(names: tuple[str, ...]) -> list[Path]:
    return [path for name in names if (path := ROOT / name).exists()]


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
    for line_number, line in iter_non_fenced_lines(lines):
        for target in markdown_inline_link_targets(line):
            yield line_number, target
        reference_match = REFERENCE_DEFINITION.match(line)
        if reference_match:
            yield line_number, reference_match.group(1)
        for match in AUTOLINK.finditer(line):
            target = match.group(1)
            if "://" in target or target.startswith("#"):
                yield line_number, target


def has_github_issue_or_pr_url(line: str) -> bool:
    return any(is_github_issue_or_pr_url(token) for token in line_tokens(line))


def line_tokens(line: str):
    for token in line.split():
        yield token.strip("<>()[]{}\"'.,;")


def is_github_issue_or_pr_url(value: str) -> bool:
    parsed = urllib.parse.urlparse(value)
    if parsed.scheme not in {"http", "https"} or parsed.netloc != "github.com":
        return False
    parts = [part for part in parsed.path.split("/") if part]
    return len(parts) >= 4 and parts[2] in {"issues", "pull"} and parts[3].isdigit()


def markdown_inline_link_targets(line: str):
    search_start = 0
    while True:
        start = line.find("](", search_start)
        if start == -1:
            return
        target_start = start + 2
        target_end = line.find(")", target_start)
        if target_end == -1:
            return
        target = line[target_start:target_end].split(maxsplit=1)[0]
        if target:
            yield target
        search_start = target_end + 1


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
