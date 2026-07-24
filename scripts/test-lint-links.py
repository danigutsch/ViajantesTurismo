#!/usr/bin/env python3

from __future__ import annotations

import importlib.util
import sys
import unittest
from pathlib import Path
from types import ModuleType


sys.dont_write_bytecode = True


def load_linter() -> ModuleType:
    script_path = Path(__file__).with_name("lint-links.py")
    spec = importlib.util.spec_from_file_location("lint_links", script_path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load {script_path}.")

    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


LINTER = load_linter()


class IssueReferencePolicyTests(unittest.TestCase):
    def test_rejects_tracker_references(self) -> None:
        cases = {
            "direct issue URL": "See https://github.com/example/project/issues/945.",
            "direct pull request URL": "See https://github.com/example/project/pull/945.",
            "inline code URL": "See `https://github.com/example/project/issues/945`.",
            "repository-relative path": "See `issues/945`.",
            "bare shorthand": "Tracked separately in #945.",
            "standalone shorthand": "- #945",
            "GitHub shorthand": "Tracked as GH-945.",
            "qualified shorthand": "Tracked as example/project#945.",
            "issue-specific filename": "Output: `TestResults/issue-945-benchmark.tsv`.",
            "six-digit shorthand": "Tracked separately in #123456.",
        }

        for name, line in cases.items():
            with self.subTest(name=name):
                self.assertTrue(LINTER.validate_policy_lines("docs/example.md", [line]))

    def test_allows_non_tracker_hashes(self) -> None:
        cases = {
            "Unicode annex": "Unicode Standard Annex #15: Unicode Normalization Forms",
            "UAX abbreviation": "UAX #15 defines normalization forms.",
            "PKCS identifier": "Use a PKCS#12 certificate.",
            "Mermaid colors": "style node fill:#e7f5ff,stroke:#123456",
            "external fragment": "See https://example.com/reference/#123.",
            "ordinary number": "Revision 945 remains supported.",
        }

        for name, line in cases.items():
            with self.subTest(name=name):
                self.assertEqual(
                    [], LINTER.validate_policy_lines("docs/example.md", [line])
                )


if __name__ == "__main__":
    unittest.main()
