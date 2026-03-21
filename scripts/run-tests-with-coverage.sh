#!/usr/bin/env bash

set -euo pipefail

# This helper expects the solution to have been restored and built already.
# CI calls it after the explicit restore/build steps in .github/workflows/ci.yml.

dotnet test --solution ViajantesTurismo.slnx --no-build -- \
    --coverage \
    --coverage-output-format cobertura \
    --coverage-output coverage.cobertura.xml \
    --coverage-settings coverage.settings.xml

shopt -s globstar nullglob
coverage_files=(tests/**/TestResults/**/coverage.cobertura.xml)
shopt -u globstar nullglob

if [[ ${#coverage_files[@]} -eq 0 ]]; then
    echo "Coverage collection completed without producing any coverage.cobertura.xml files." >&2
    echo "Expected files under tests/*/TestResults/*/coverage.cobertura.xml before generating the HTML report." >&2
    echo "This may indicate that the test run did not complete successfully or that coverage output was not written." >&2
    echo "To reproduce locally, run restore/build first, then run bash scripts/run-tests-with-coverage.sh from the repository root." >&2
    exit 1
fi

coverage_reports=$(printf '%s;' "${coverage_files[@]}")
coverage_reports=${coverage_reports%;}

dotnet tool run reportgenerator \
    "-reports:${coverage_reports}" \
    "-targetdir:TestResults/CoverageReport" \
    "-reporttypes:Html"
