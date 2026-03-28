#!/usr/bin/env bash

set -euo pipefail

set_build_required() {
    local build_required_value="$1"

    echo "build_required=${build_required_value}" >> "${GITHUB_OUTPUT}"

    return 0
}

require_build() {
    local message="$1"

    echo "${message}"
    set_build_required true
    exit 0
}

if [[ -z "${GITHUB_OUTPUT:-}" ]]; then
    echo "GITHUB_OUTPUT is required" >&2
    exit 1
fi

if [[ "${GITHUB_EVENT_NAME:-}" == "pull_request" ]]; then
    if [[ -z "${GITHUB_BASE_SHA:-}" || -z "${GITHUB_HEAD_SHA:-}" ]]; then
        require_build "Build and test will run because pull request SHAs are unavailable."
    fi

    diff_range="${GITHUB_BASE_SHA}...${GITHUB_HEAD_SHA}"
elif [[ "${GITHUB_EVENT_NAME:-}" == "push" ]]; then
    if [[ "${GITHUB_BEFORE_SHA:-}" == "0000000000000000000000000000000000000000" ]]; then
        require_build "Build and test will run for the first push on a branch."
    fi

    if [[ -z "${GITHUB_BEFORE_SHA:-}" || -z "${GITHUB_AFTER_SHA:-}" ]]; then
        require_build "Build and test will run because push SHAs are unavailable."
    fi

    diff_range="${GITHUB_BEFORE_SHA}..${GITHUB_AFTER_SHA}"
else
    require_build "Build and test will run for event '${GITHUB_EVENT_NAME:-unknown}'."
fi

build_required=false
changed_files=""

if ! changed_files="$(git diff --name-only "${diff_range}")"; then
    require_build "Build and test will run because the change range '${diff_range}' could not be evaluated."
fi

while IFS= read -r file; do
    if [[ -z "${file}" ]]; then
        continue
    fi

    case "${file}" in
        docs/** | README.md | CONTRIBUTING.md) ;;
        *)
            build_required=true
            break
            ;;
    esac
done <<< "${changed_files}"

if [[ "${build_required}" == "true" ]]; then
    echo "Build and test will run because non-documentation changes were detected."
else
    echo "Build and test will be skipped because only documentation changes were detected."
fi

set_build_required "${build_required}"
