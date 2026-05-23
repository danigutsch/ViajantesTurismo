#!/usr/bin/env python3

from __future__ import annotations

import json
import pathlib
import re
import sys
from collections import Counter
from dataclasses import dataclass


IGNORED_PARTS = {"bin", "obj", "TestResults", "node_modules", ".sonarqube", ".vs", ".vscode"}


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


def iter_target_files(args: list[str]) -> list[pathlib.Path]:
    if args:
        files: list[pathlib.Path] = []
        for arg in args:
            if arg == "--":
                continue
            if arg == "tests/**/*.feature":
                files.extend(path for path in pathlib.Path("tests").rglob("*.feature") if is_feature_path(path))
                continue

            candidate = pathlib.Path(arg)
            if candidate.is_dir():
                files.extend(path for path in candidate.rglob("*.feature") if is_feature_path(path))
                continue

            if candidate.exists() and is_feature_path(candidate):
                files.append(candidate)

        return sorted({path.resolve(): path for path in files}.values(), key=lambda path: str(path))

    return sorted(
        (path for path in pathlib.Path("tests").rglob("*.feature") if is_feature_path(path)),
        key=lambda path: str(path),
    )


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
            for tag in collect_tag_tokens(no_comment):
                if not is_tag_allowed(tag, rules):
                    errors.append(f"{path}:{index}: tag '{tag}' is not allowed")
                if tag in rules.restricted_tags:
                    errors.append(f"{path}:{index}: tag '{tag}' is forbidden; use @wip")
            continue

        if starts_with_keyword(no_comment, "Feature"):
            name = no_comment.partition(":")[2].strip()
            if not name:
                errors.append(f"{path}:{index}: Feature name is required")
            else:
                if len(name) > rules.feature_name_max:
                    errors.append(f"{path}:{index}: Feature name exceeds {rules.feature_name_max} characters")
                feature_name = name
                feature_line = index
                seen_feature_names[name] += 1
            continue

        if starts_with_keyword(no_comment, "Scenario Outline"):
            if rules.require_examples_for_outlines and inside_outline and not outline_has_examples:
                errors.append(f"{path}:{index}: Scenario Outline must include an Examples section")

            name = no_comment.partition(":")[2].strip()
            if not name:
                errors.append(f"{path}:{index}: Scenario name is required")
            else:
                if len(name) > rules.scenario_name_max:
                    errors.append(f"{path}:{index}: Scenario name exceeds {rules.scenario_name_max} characters")
                scenario_names[name] += 1
                if scenario_names[name] > 1:
                    errors.append(f"{path}:{index}: duplicate scenario name '{name}'")

            inside_outline = True
            outline_has_examples = False
            has_any_scenario = True
            continue

        if starts_with_keyword(no_comment, "Scenario"):
            if rules.require_examples_for_outlines and inside_outline and not outline_has_examples:
                errors.append(f"{path}:{index}: Scenario Outline must include an Examples section")

            name = no_comment.partition(":")[2].strip()
            if not name:
                errors.append(f"{path}:{index}: Scenario name is required")
            else:
                if len(name) > rules.scenario_name_max:
                    errors.append(f"{path}:{index}: Scenario name exceeds {rules.scenario_name_max} characters")
                scenario_names[name] += 1
                if scenario_names[name] > 1:
                    errors.append(f"{path}:{index}: duplicate scenario name '{name}'")

            inside_outline = False
            outline_has_examples = False
            has_any_scenario = True
            continue

        if starts_with_keyword(no_comment, "Examples"):
            if inside_outline:
                outline_has_examples = True
            continue

    if rules.require_examples_for_outlines and inside_outline and not outline_has_examples:
        errors.append(f"{path}:{len(lines)}: Scenario Outline must include an Examples section")

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
