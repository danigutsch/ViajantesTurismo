#!/usr/bin/env bash

set -euo pipefail

if output="$(dotnet run --project tools/SharedKernel.Documentation.Tool -- generate --config docs/architecture/generated-diagrams.json "$@" 2>&1)"; then
    printf '%s\n' "${output}"
else
    status=$?
    printf '%s\n' "${output}" >&2
    if [[ "${output}" == *"Generated documentation is stale:"* ]]; then
        printf '%s\n' "Run: bash scripts/update-architecture-diagrams.sh" >&2
    fi

    exit "${status}"
fi
