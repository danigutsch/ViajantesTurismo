#!/usr/bin/env bash

set -euo pipefail

npm exec --yes --package markdownlint-cli@0.48.0 -- \
    markdownlint "**/*.md" \
    --ignore node_modules \
    --ignore "**/bin/**" \
    --ignore "**/obj/**" \
    --ignore "**/TestResults/**" \
    --config .markdownlint.json "$@"
