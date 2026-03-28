#!/usr/bin/env bash

set -euo pipefail

main() {
    local coverage_reports_file="${1:-}"

    if [[ -z "${coverage_reports_file}" ]]; then
        echo "Usage: bash scripts/collect-test-coverage.sh <coverage-reports-file>" >&2
        return 1
    fi

    mkdir -p "$(dirname "${coverage_reports_file}")"

    dotnet test --solution ViajantesTurismo.slnx --no-build -- \
        --coverage \
        --coverage-output-format cobertura \
        --coverage-output coverage.cobertura.xml \
        --coverage-settings coverage.settings.xml

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
