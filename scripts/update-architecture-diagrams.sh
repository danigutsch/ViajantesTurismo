#!/usr/bin/env bash

set -euo pipefail

diagram_command=generate
diagram_arguments=()
for argument in "$@"; do
    case "${argument}" in
        --check)
            diagram_command=verify
            ;;
        *)
            diagram_arguments+=("${argument}")
            ;;
    esac
done

if output="$(dotnet tool run sharedkernel-docs -- "${diagram_command}" --config docs/architecture/generated-diagrams.json "${diagram_arguments[@]}" 2>&1)"; then
    printf '%s\n' "${output}"
else
    status=$?
    printf '%s\n' "${output}" >&2
    if [[ "${output}" == *"Generated documentation is stale:"* ]]; then
        printf '%s\n' "Run: bash scripts/update-architecture-diagrams.sh" >&2
    fi

    exit "${status}"
fi
