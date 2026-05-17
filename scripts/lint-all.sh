#!/usr/bin/env bash

set -euo pipefail

shopt -s globstar nullglob

bash scripts/lint-markdown.sh
shellcheck \
    --exclude=SC1091 \
    --source-path=SCRIPTDIR \
    -- ./**/*.sh
bash scripts/lint-json.sh
bash scripts/lint-gherkin.sh tests/**/*.feature
