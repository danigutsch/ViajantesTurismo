#!/usr/bin/env bash

set -euo pipefail

shopt -s globstar nullglob

bash scripts/lint-markdown.sh

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
