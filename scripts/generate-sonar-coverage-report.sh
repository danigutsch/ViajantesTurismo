#!/usr/bin/env bash

set -euo pipefail

main() {
    local coverage_report="TestResults/sonar-coverage.xml"
    local coverage_reports_file="TestResults/sonar-coverage-reports.txt"
    local coverage_html_dir="TestResults/CoverageReport"

    mkdir -p TestResults

    shopt -s globstar nullglob
    local -a coverage_files=(artifacts/**/coverage.cobertura.xml tests/**/TestResults/**/coverage.cobertura.xml TestResults/**/coverage.cobertura.xml)
    shopt -u globstar nullglob

    if [[ ${#coverage_files[@]} -eq 0 ]]; then
        echo "Coverage aggregation completed without finding any coverage.cobertura.xml files." >&2
        return 1
    fi

    local coverage_reports
    coverage_reports=$(printf '%s;' "${coverage_files[@]}")
    coverage_reports=${coverage_reports%;}

    printf '%s\n' "${coverage_reports}" > "${coverage_reports_file}"

    rm -rf "${coverage_html_dir}"

    dotnet tool run reportgenerator \
        "-reports:${coverage_reports}" \
        "-targetdir:${coverage_html_dir}" \
        "-reporttypes:Html;SonarQube"

    cp "${coverage_html_dir}/SonarQube.xml" "${coverage_report}"

    return 0
}

main "$@"
