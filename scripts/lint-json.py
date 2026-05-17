#!/usr/bin/env python3

from __future__ import annotations

import json
import pathlib
import re
import sys


IGNORED_PARTS = {"node_modules", "bin", "obj", "TestResults", ".vs", ".vscode"}


def iter_target_files(args: list[str]) -> list[pathlib.Path]:
    if args:
        return [pathlib.Path(arg) for arg in args if arg != "--" and arg.endswith(".json")]

    return [
        path for path in pathlib.Path(".").rglob("*.json")
        if not any(part in IGNORED_PARTS for part in path.parts)
    ]


def strip_jsonc(content: str) -> str:
    result: list[str] = []
    index = 0
    in_string = False
    string_quote = ""
    length = len(content)

    while index < length:
        current = content[index]
        next_char = content[index + 1] if index + 1 < length else ""

        if in_string:
            result.append(current)
            if current == "\\" and index + 1 < length:
                result.append(content[index + 1])
                index += 2
                continue
            if current == string_quote:
                in_string = False
            index += 1
            continue

        if current in {'"', "'"}:
            in_string = True
            string_quote = current
            result.append(current)
            index += 1
            continue

        if current == "/" and next_char == "/":
            index += 2
            while index < length and content[index] not in {"\n", "\r"}:
                index += 1
            continue

        if current == "/" and next_char == "*":
            index += 2
            while index + 1 < length and not (content[index] == "*" and content[index + 1] == "/"):
                index += 1
            index += 2
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
