#!/usr/bin/env bash

set -euo pipefail

if command -v python3 >/dev/null 2>&1; then
    python3 scripts/lint-gherkin.py "$@"
    exit 0
fi

if command -v docker >/dev/null 2>&1; then
    docker run --rm \
        --volume "${PWD}:/workspace" \
        --workdir /workspace \
        python:3.13-alpine \
        python3 scripts/lint-gherkin.py "$@"
    exit 0
fi

echo "lint-gherkin requires either local python3 or docker." >&2
exit 1
