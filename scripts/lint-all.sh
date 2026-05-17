#!/usr/bin/env bash

set -euo pipefail

shopt -s globstar nullglob

bash scripts/lint-markdown.sh
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
