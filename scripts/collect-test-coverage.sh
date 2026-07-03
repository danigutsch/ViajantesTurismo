#!/usr/bin/env bash

set -euo pipefail

coverage_args=(
    --coverage
    --coverage-output-format cobertura
    --coverage-output coverage.cobertura.xml
    --coverage-settings coverage.settings.xml
)

get_test_project_parallelism() {
    if [[ -n "${CI_TEST_PROJECT_PARALLELISM:-}" ]]; then
        if [[ "${CI_TEST_PROJECT_PARALLELISM}" =~ ^[1-9][0-9]*$ ]]; then
            printf '%s\n' "${CI_TEST_PROJECT_PARALLELISM}"
            return 0
        fi

        echo "CI_TEST_PROJECT_PARALLELISM must be a positive integer." >&2
        return 1
    fi

    if command -v nproc > /dev/null 2>&1; then
        nproc
        return 0
    fi

    getconf _NPROCESSORS_ONLN 2> /dev/null || printf '2\n'
}

run_project_tests() {
    local max_parallel
    max_parallel="$(get_test_project_parallelism)"

    local active=0
    local failed=0
    local project_path

    echo "Running $# test projects with up to ${max_parallel} project(s) in parallel."

    for project_path in "$@"; do
        (
            echo "==> Testing ${project_path}"
            dotnet test --project "${project_path}" --no-restore --no-build -- "${coverage_args[@]}"
        ) &

        active=$((active + 1))

        if [[ ${active} -ge ${max_parallel} ]]; then
            if ! wait -n; then
                failed=1
            fi

            active=$((active - 1))
        fi
    done

    while [[ ${active} -gt 0 ]]; do
        if ! wait -n; then
            failed=1
        fi

        active=$((active - 1))
    done

    return "${failed}"
}

main() {
    local coverage_reports_file="${1:-}"

    if [[ -z "${coverage_reports_file}" ]]; then
        echo "Usage: bash scripts/collect-test-coverage.sh <coverage-reports-file>" >&2
        return 1
    fi

    mkdir -p "$(dirname "${coverage_reports_file}")"

    shift

    if [[ $# -eq 0 ]]; then
        dotnet test --solution ViajantesTurismo.slnx --no-restore --no-build -- "${coverage_args[@]}"
    else
        run_project_tests "$@"
    fi

    shopt -s globstar nullglob
    local -a coverage_files=(tests/**/TestResults/**/coverage.cobertura.xml)
    shopt -u globstar nullglob

    if [[ ${#coverage_files[@]} -eq 0 ]]; then
        echo "Coverage collection completed without producing any coverage.cobertura.xml files." >&2
        echo "Expected files under tests/*/TestResults/*/coverage.cobertura.xml before generating a coverage report." >&2
        echo "This may indicate that the test run did not complete successfully or that coverage output was not written." >&2
        echo "To reproduce locally, run restore/build first, then run bash scripts/run-tests-with-coverage.sh from the repository root." >&2
        return 1
    fi

    local coverage_reports
    coverage_reports=$(printf '%s;' "${coverage_files[@]}")
    coverage_reports=${coverage_reports%;}

    printf '%s\n' "${coverage_reports}" > "${coverage_reports_file}"

    return 0
}

main "$@"
