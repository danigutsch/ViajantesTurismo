#!/usr/bin/env bash

set -euo pipefail

# This helper expects the solution to have been restored and built already.
# CI calls it after the explicit restore/build steps in .github/workflows/ci.yml.

coverage_reports_file="TestResults/coverage-reports.txt"

bash scripts/collect-test-coverage.sh "${coverage_reports_file}"

coverage_reports="$(< "${coverage_reports_file}")"

dotnet tool run reportgenerator \
    "-reports:${coverage_reports}" \
    "-targetdir:TestResults/CoverageReport" \
    "-reporttypes:Html"
