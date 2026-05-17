#!/usr/bin/env python3

from __future__ import annotations

import json
import pathlib
import re
import sys


IGNORED_PARTS = {"node_modules", "bin", "obj", "TestResults", ".vs", ".vscode"}


def is_included_json_path(path: pathlib.Path) -> bool:
    return path.suffix == ".json" and not any(part in IGNORED_PARTS for part in path.parts)


def append_escaped_character(content: str, index: int, result: list[str]) -> int:
    result.append(content[index])
    result.append(content[index + 1])
    return index + 2


def try_consume_string_character(
    content: str,
    index: int,
    result: list[str],
    string_quote: str,
) -> tuple[int, bool]:
    current = content[index]

    if current == "\\" and index + 1 < len(content):
        return append_escaped_character(content, index, result), False

    result.append(current)

    return index + 1, current == string_quote


def consume_line_comment(content: str, index: int) -> int:
    index += 2
    while index < len(content) and content[index] not in {"\n", "\r"}:
        index += 1
    return index


def consume_block_comment(content: str, index: int) -> int:
    index += 2
    while index + 1 < len(content) and not (content[index] == "*" and content[index + 1] == "/"):
        index += 1
    return index + 2


def try_consume_comment(content: str, index: int) -> int | None:
    if index + 1 >= len(content) or content[index] != "/":
        return None

    next_char = content[index + 1]
    if next_char == "/":
        return consume_line_comment(content, index)
    if next_char == "*":
        return consume_block_comment(content, index)

    return None


def iter_target_files(args: list[str]) -> list[pathlib.Path]:
    if args:
        return [pathlib.Path(arg) for arg in args if arg != "--" and is_included_json_path(pathlib.Path(arg))]

    return [path for path in pathlib.Path(".").rglob("*.json") if is_included_json_path(path)]


def strip_jsonc(content: str) -> str:
    result: list[str] = []
    index = 0
    in_string = False
    string_quote = ""

    while index < len(content):
        current = content[index]

        if in_string:
            index, string_closed = try_consume_string_character(content, index, result, string_quote)
            if string_closed:
                in_string = False
            continue

        if current in {'"', "'"}:
            in_string = True
            string_quote = current
            result.append(current)
            index += 1
            continue

        comment_end = try_consume_comment(content, index)
        if comment_end is not None:
            index = comment_end
            continue

        result.append(current)
        index += 1

    stripped = "".join(result)
    return re.sub(r",\s*([}\]])", r"\1", stripped)


def validate_file(path: pathlib.Path) -> None:
    content = path.read_text(encoding="utf-8-sig")
    sanitized = strip_jsonc(content)
    json.loads(sanitized)


def main() -> int:
    failures: list[str] = []

    for path in iter_target_files(sys.argv[1:]):
        try:
            validate_file(path)
        except Exception as exc:  # noqa: BLE001
            failures.append(f"{path}: {exc}")

    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
