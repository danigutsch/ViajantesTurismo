#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat >&2 <<'EOF'
Usage: bash scripts/run-ci-test-slice.sh --slice-name <name> [--install-playwright] <project> [<project> ...]
EOF

    return 1
}

run_with_log() {
    local description="$1"
    local log_file="$2"

    shift 2

    echo "==> ${description}"

    if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
        if "$@" 2>&1 | tee "${log_file}"; then
            return 0
        fi
    else
        if "$@" >"${log_file}" 2>&1; then
            return 0
        fi
    fi

    echo "${description} failed. See ${log_file}." >&2
    return 1
}

find_playwright_script() {
    local playwright_script

    playwright_script="$(find tests -name playwright.ps1 -path '*/bin/Debug/*' | head -1)"

    if [[ -z "${playwright_script}" ]]; then
        echo "Playwright install script was not found under tests/*/bin/Debug after build." >&2
        return 1
    fi

    printf '%s\n' "${playwright_script}"

    return 0
}

append_timing() {
    local timings_file="$1"
    local phase_name="$2"
    local duration_seconds="$3"

    printf '%s\t%s\n' "${phase_name}" "${duration_seconds}" >> "${timings_file}"

    return 0
}

write_summary() {
    local slice_name="$1"
    local timings_file="$2"

    if [[ -z "${GITHUB_STEP_SUMMARY:-}" ]]; then
        return 0
    fi

    {
        echo "## ${slice_name}"
        echo
        echo "| Phase | Duration (s) |"
        echo "| --- | ---: |"

        while IFS=$'\t' read -r phase_name duration_seconds; do
            if [[ "${phase_name}" == "phase" ]]; then
                continue
            fi

            echo "| ${phase_name} | ${duration_seconds} |"
        done < "${timings_file}"

        echo
    } >> "${GITHUB_STEP_SUMMARY}"

    return 0
}

main() {
    local slice_name=""
    local install_playwright=false

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --slice-name)
                slice_name="$2"
                shift 2
                ;;
            --install-playwright)
                install_playwright=true
                shift
                ;;
            --)
                shift
                break
                ;;
            *)
                break
                ;;
        esac
    done

    if [[ -z "${slice_name}" || $# -eq 0 ]]; then
        usage
    fi

    local -a projects=("$@")
    local slice_slug
    slice_slug="$(printf '%s' "${slice_name}" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')"

    local coverage_reports_file="TestResults/${slice_slug}-coverage-reports.txt"
    local coverage_collection_log="TestResults/${slice_slug}-coverage-collection.log"
    local playwright_install_log="TestResults/${slice_slug}-playwright-install.log"
    local reportgenerator_log="TestResults/${slice_slug}-reportgenerator.log"
    local coverage_output_dir="TestResults/CoverageReport/${slice_slug}"
    local timings_file="TestResults/${slice_slug}-phase-timings.tsv"

    mkdir -p TestResults
    printf 'phase\tduration_seconds\n' > "${timings_file}"

    if [[ "${install_playwright}" == "true" ]]; then
        local playwright_script
        playwright_script="$(find_playwright_script)"

        local start_seconds
        start_seconds="$(date +%s)"

        run_with_log "Installing Playwright browsers for ${slice_name}" "${playwright_install_log}" \
            bash scripts/install-playwright.sh "${playwright_script}" chromium

        local end_seconds
        end_seconds="$(date +%s)"
        local install_playwright_seconds
        install_playwright_seconds="$(( end_seconds - start_seconds ))"
        append_timing "${timings_file}" "install_playwright" "${install_playwright_seconds}"
    fi

    local start_seconds
    start_seconds="$(date +%s)"

    run_with_log "Running ${slice_name} tests with coverage" "${coverage_collection_log}" \
        bash scripts/collect-test-coverage.sh "${coverage_reports_file}" "${projects[@]}"

    local end_seconds
    end_seconds="$(date +%s)"
    local collect_test_coverage_seconds
    collect_test_coverage_seconds="$(( end_seconds - start_seconds ))"
    append_timing "${timings_file}" "collect_test_coverage" "${collect_test_coverage_seconds}"

    local coverage_reports
    coverage_reports="$(< "${coverage_reports_file}")"

    rm -rf "${coverage_output_dir}"

    start_seconds="$(date +%s)"

    run_with_log "Generating coverage report for ${slice_name}" "${reportgenerator_log}" \
        dotnet tool run reportgenerator \
            "-reports:${coverage_reports}" \
            "-targetdir:${coverage_output_dir}" \
            "-reporttypes:Html"

    end_seconds="$(date +%s)"
    local generate_coverage_report_seconds
    generate_coverage_report_seconds="$(( end_seconds - start_seconds ))"
    append_timing "${timings_file}" "generate_coverage_report" "${generate_coverage_report_seconds}"

    write_summary "${slice_name}" "${timings_file}"

    return 0
}

main "$@"
