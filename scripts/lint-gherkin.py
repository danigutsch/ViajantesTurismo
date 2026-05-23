#!/usr/bin/env python3

from __future__ import annotations

import json
import pathlib
import re
import sys
from collections import Counter
from dataclasses import dataclass


IGNORED_PARTS = {"bin", "obj", "TestResults", "node_modules", ".sonarqube", ".vs", ".vscode"}
FEATURE_GLOB = "*.feature"


@dataclass(frozen=True)
class RuleConfig:
    tags: set[str]
    patterns: tuple[re.Pattern[str], ...]
    restricted_tags: set[str]
    feature_name_max: int
    scenario_name_max: int
    require_examples_for_outlines: bool


def get_rule_payload(raw: dict[str, object], key: str) -> dict[str, object]:
    rule_value = raw.get(key, ["off", {}])
    if isinstance(rule_value, list) and len(rule_value) > 1 and isinstance(rule_value[1], dict):
        return rule_value[1]

    return {}


def is_rule_enabled(raw: dict[str, object], key: str) -> bool:
    rule_value = raw.get(key)
    if rule_value == "on":
        return True

    return bool(isinstance(rule_value, list) and len(rule_value) > 0 and rule_value[0] == "on")


def load_rules(config_path: pathlib.Path) -> RuleConfig:
    raw = json.loads(config_path.read_text(encoding="utf-8"))
    payload = get_rule_payload(raw, "allowed-tags")
    restricted_tags_payload = get_rule_payload(raw, "no-restricted-tags")
    name_length_payload = get_rule_payload(raw, "name-length")
    tags = set(payload.get("tags", []))
    patterns = tuple(re.compile(pattern) for pattern in payload.get("patterns", []))
    restricted_tags = set(restricted_tags_payload.get("tags", []))
    feature_name_max = int(name_length_payload.get("Feature", 250))
    scenario_name_max = int(name_length_payload.get("Scenario", 150))
    require_examples_for_outlines = is_rule_enabled(raw, "no-scenario-outlines-without-examples")

    return RuleConfig(
        tags=tags,
        patterns=patterns,
        restricted_tags=restricted_tags,
        feature_name_max=feature_name_max,
        scenario_name_max=scenario_name_max,
        require_examples_for_outlines=require_examples_for_outlines,
    )


def is_feature_path(path: pathlib.Path) -> bool:
    return path.suffix == ".feature" and not any(part in IGNORED_PARTS for part in path.parts)


def collect_feature_files(root: pathlib.Path) -> list[pathlib.Path]:
    return [path for path in root.rglob(FEATURE_GLOB) if is_feature_path(path)]


def unique_sorted_paths(paths: list[pathlib.Path]) -> list[pathlib.Path]:
    return sorted({path.resolve(): path for path in paths}.values(), key=lambda path: str(path))


def iter_target_files(args: list[str]) -> list[pathlib.Path]:
    if not args:
        return unique_sorted_paths(collect_feature_files(pathlib.Path("tests")))

    files: list[pathlib.Path] = []
    for arg in args:
        if arg == "--":
            continue
        if arg == "tests/**/*.feature":
            files.extend(collect_feature_files(pathlib.Path("tests")))
            continue

        candidate = pathlib.Path(arg)
        if candidate.is_dir():
            files.extend(collect_feature_files(candidate))
            continue

        if candidate.exists() and is_feature_path(candidate):
            files.append(candidate)

    return unique_sorted_paths(files)


def strip_inline_comment(value: str) -> str:
    if "#" not in value:
        return value.rstrip()
    hash_index = value.find("#")
    if hash_index == 0:
        return ""
    return value[:hash_index].rstrip()


def is_tag_allowed(tag: str, rules: RuleConfig) -> bool:
    if tag in rules.tags:
        return True
    return any(pattern.fullmatch(tag) for pattern in rules.patterns)


def starts_with_keyword(content: str, keyword: str) -> bool:
    return content == keyword or content.startswith(f"{keyword}:")


def collect_tag_tokens(tag_line: str) -> list[str]:
    return [token for token in tag_line.split() if token.startswith("@")]


def add_name_errors(
    errors: list[str],
    path: pathlib.Path,
    index: int,
    label: str,
    name: str,
    max_length: int,
) -> bool:
    if not name:
        errors.append(f"{path}:{index}: {label} name is required")
        return False

    if len(name) > max_length:
        errors.append(f"{path}:{index}: {label} name exceeds {max_length} characters")

    return True


def validate_tag_line(path: pathlib.Path, index: int, line: str, rules: RuleConfig, errors: list[str]) -> None:
    for tag in collect_tag_tokens(line):
        if not is_tag_allowed(tag, rules):
            errors.append(f"{path}:{index}: tag '{tag}' is not allowed")
        if tag in rules.restricted_tags:
            errors.append(f"{path}:{index}: tag '{tag}' is forbidden; use @wip")


def validate_feature_line(
    path: pathlib.Path,
    index: int,
    line: str,
    rules: RuleConfig,
    seen_feature_names: Counter[str],
    errors: list[str],
) -> tuple[str | None, int | None]:
    name = line.partition(":")[2].strip()
    if not add_name_errors(errors, path, index, "Feature", name, rules.feature_name_max):
        return None, None

    seen_feature_names[name] += 1
    return name, index


def validate_scenario_line(
    path: pathlib.Path,
    index: int,
    line: str,
    rules: RuleConfig,
    scenario_names: Counter[str],
    errors: list[str],
) -> None:
    name = line.partition(":")[2].strip()
    if not add_name_errors(errors, path, index, "Scenario", name, rules.scenario_name_max):
        return

    scenario_names[name] += 1
    if scenario_names[name] > 1:
        errors.append(f"{path}:{index}: duplicate scenario name '{name}'")


def add_outline_examples_error(errors: list[str], path: pathlib.Path, index: int, rules: RuleConfig, inside_outline: bool, outline_has_examples: bool) -> None:
    if rules.require_examples_for_outlines and inside_outline and not outline_has_examples:
        errors.append(f"{path}:{index}: Scenario Outline must include an Examples section")


def validate_feature(path: pathlib.Path, rules: RuleConfig, seen_feature_names: Counter[str]) -> list[str]:
    content = path.read_text(encoding="utf-8")
    lines = content.splitlines()
    errors: list[str] = []

    if not lines:
        return [f"{path}:1: empty feature file"]

    if path.read_bytes() and not content.endswith("\n"):
        errors.append(f"{path}:{len(lines)}: missing newline at EOF")

    feature_name: str | None = None
    feature_line: int | None = None
    scenario_names: Counter[str] = Counter()
    has_any_scenario = False
    inside_outline = False
    outline_has_examples = False

    for index, raw_line in enumerate(lines, start=1):
        if raw_line.rstrip() != raw_line:
            errors.append(f"{path}:{index}: trailing spaces are not allowed")

        stripped = raw_line.strip()
        if not stripped or stripped.startswith("#"):
            continue

        no_comment = strip_inline_comment(stripped)
        if not no_comment:
            continue

        if "\t" in raw_line:
            errors.append(f"{path}:{index}: tabs are not allowed")

        if no_comment.startswith("@"):
            validate_tag_line(path, index, no_comment, rules, errors)
            continue

        if starts_with_keyword(no_comment, "Feature"):
            feature_name, feature_line = validate_feature_line(path, index, no_comment, rules, seen_feature_names, errors)
            continue

        if starts_with_keyword(no_comment, "Scenario Outline"):
            add_outline_examples_error(errors, path, index, rules, inside_outline, outline_has_examples)
            validate_scenario_line(path, index, no_comment, rules, scenario_names, errors)

            inside_outline = True
            outline_has_examples = False
            has_any_scenario = True
            continue

        if starts_with_keyword(no_comment, "Scenario"):
            add_outline_examples_error(errors, path, index, rules, inside_outline, outline_has_examples)
            validate_scenario_line(path, index, no_comment, rules, scenario_names, errors)

            inside_outline = False
            outline_has_examples = False
            has_any_scenario = True
            continue

        if starts_with_keyword(no_comment, "Examples"):
            if inside_outline:
                outline_has_examples = True

    add_outline_examples_error(errors, path, len(lines), rules, inside_outline, outline_has_examples)

    if feature_name is None:
        errors.append(f"{path}:1: Feature declaration is required")
    elif seen_feature_names[feature_name] > 1 and feature_line is not None:
        errors.append(f"{path}:{feature_line}: duplicate feature name '{feature_name}'")

    if not has_any_scenario:
        errors.append(f"{path}:1: feature file must contain at least one scenario")

    return errors


def main() -> int:
    config_path = pathlib.Path(".gherkin-lintrc")
    if not config_path.exists():
        print(".gherkin-lintrc not found", file=sys.stderr)
        return 1

    rules = load_rules(config_path)
    files = iter_target_files(sys.argv[1:])

    if not files:
        return 0

    seen_feature_names: Counter[str] = Counter()
    failures: list[str] = []

    for feature_file in files:
        failures.extend(validate_feature(feature_file, rules, seen_feature_names))

    if failures:
        for failure in failures:
            print(failure, file=sys.stderr)
        return 1

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
