#!/usr/bin/env bash

set -euo pipefail

shopt -s globstar nullglob

if ! command -v shellcheck >/dev/null 2>&1; then
    echo "shellcheck is required for bash scripts/lint-all.sh. Install shellcheck locally or rely on CI." >&2
    exit 1
fi

shellcheck_targets=(
    ./.devcontainer/*.sh
    ./scripts/*.sh
    ./setup-dev.sh
)

shellcheck \
    --exclude=SC1091 \
    --source-path=SCRIPTDIR \
    -- "${shellcheck_targets[@]}"
bash scripts/lint-json.sh
bash scripts/lint-gherkin.sh tests/**/*.feature

# Markdown lint
if ! command -v npx >/dev/null 2>&1; then
    echo "npx is required for markdownlint. Please install Node.js/npx." >&2
    exit 1
fi
npx markdownlint-cli2@0.22.1 "**/*.md"

# Optional: markdown spelling (warn only, does not fail build)
if command -v mdspell >/dev/null 2>&1; then
    mdspell --en-us --report "docs/**/*.md" || true
else
    echo "Optional: Install markdown-spellcheck (mdspell) to enable spell checking. Skipping..." >&2
fi
