#!/usr/bin/env bash

set -euo pipefail

main() {
    local slice_name="${1:?slice name is required}"

    if [[ -z "${GITHUB_STEP_SUMMARY:-}" ]]; then
        return 0
    fi

    {
        echo "### ${slice_name} artifacts"
        echo
        echo "Use these artifacts for early diagnosis when a completed job fails before the full workflow finishes."
        echo
        echo "| Artifact | Link |"
        echo "| --- | --- |"
        write_artifact_row "Test results" "${RESULTS_ARTIFACT_URL:-}"
        write_artifact_row "Failure diagnostics" "${DIAGNOSTICS_ARTIFACT_URL:-}"
        echo
    } >> "${GITHUB_STEP_SUMMARY}"

    return 0
}

write_artifact_row() {
    local label="$1"
    local url="$2"

    if [[ -n "${url}" ]]; then
        printf '| %s | [Download](%s) |\n' "${label}" "${url}"
    else
        printf '| %s | Not created for this job outcome |\n' "${label}"
    fi
}

main "$@"
