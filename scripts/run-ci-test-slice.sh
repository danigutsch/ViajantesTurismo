#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat >&2 <<'EOF'
Usage: bash scripts/run-ci-test-slice.sh --slice-name <name> [--install-playwright] [--projects-file <path>] <project> [<project> ...]
EOF

    return 1
}

json_escape_into() {
    local target_var_name="$1"
    local value="$2"

    value=${value//\\/\\\\}
    value=${value//\"/\\\"}
    value=${value//$'\n'/\\n}
    value=${value//$'\r'/\\r}
    value=${value//$'\t'/\\t}

    printf -v "${target_var_name}" '%s' "${value}"
}

write_timing_row() {
    local phase="$1"
    local description="$2"
    local started_at_utc="$3"
    local completed_at_utc="$4"
    local duration_seconds="$5"
    local status="$6"
    local log_file="$7"

    printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
        "${phase}" \
        "${description}" \
        "${started_at_utc}" \
        "${completed_at_utc}" \
        "${duration_seconds}" \
        "${status}" \
        "${log_file}" >> "${timings_file}"

    return 0
}

run_with_log() {
    local phase="$1"
    local description="$2"
    local log_file="$3"

    shift 3

    local started_at_epoch
    started_at_epoch=$(date +%s)

    local started_at_utc
    started_at_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

    local completed_at_epoch
    local completed_at_utc
    local duration_seconds
    local exit_code
    local status

    echo "==> ${description}"

    if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
        echo "::group::${description}"
    fi

    set +e

    if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
        "$@" 2>&1 | tee "${log_file}"
        exit_code="${PIPESTATUS[0]}"
    else
        "$@" >"${log_file}" 2>&1
        exit_code="$?"
    fi

    set -e

    completed_at_epoch=$(date +%s)
    completed_at_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')
    duration_seconds=$((completed_at_epoch - started_at_epoch))
    status="success"

    if [[ ${exit_code} -ne 0 ]]; then
        status="failure"
    fi

    write_timing_row \
        "${phase}" \
        "${description}" \
        "${started_at_utc}" \
        "${completed_at_utc}" \
        "${duration_seconds}" \
        "${status}" \
        "${log_file}"

    if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
        echo "::endgroup::"
    fi

    if [[ ${exit_code} -eq 0 ]]; then
        return 0
    fi

    echo "${description} failed. See ${log_file}." >&2
    return "${exit_code}"
}

build_projects() {
    local project_path

    for project_path in "$@"; do
        if ! dotnet build --no-restore "${project_path}"; then
            return 1
        fi
    done

    return 0
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

write_summary() {
    local slice_name="$1"
    local timings_file="$2"

    if [[ -z "${GITHUB_STEP_SUMMARY:-}" ]]; then
        return 0
    fi

    {
        echo "## ${slice_name}"
        echo
        echo "| Phase | Duration | Status | Log |"
        echo "| --- | ---: | --- | --- |"

        local phase
        local duration_seconds
        local status
        local log_file
        local code_quote='`'

        while IFS=$'\t' read -r phase _description _started_at_utc _completed_at_utc duration_seconds status log_file; do
            if [[ "${phase}" == "phase" ]]; then
                continue
            fi

            printf '| %s%s%s | %ss | %s%s%s | %s%s%s |\n' \
                "${code_quote}" \
                "${phase}" \
                "${code_quote}" \
                "${duration_seconds}" \
                "${code_quote}" \
                "${status}" \
                "${code_quote}" \
                "${code_quote}" \
                "${log_file}" \
                "${code_quote}"
        done < "${timings_file}"

        echo
    } >> "${GITHUB_STEP_SUMMARY}"

    return 0
}

write_manifest() {
    local manifest_file="$1"
    local slice_name="$2"
    local slice_slug="$3"
    local install_playwright="$4"
    local status="$5"
    local workflow_started_utc="$6"
    local completed_at_utc="$7"
    local timings_file="$8"
    local build_log="$9"
    local coverage_collection_log="${10}"
    local playwright_install_log="${11}"
    local escaped_slice_name
    local escaped_slice_slug
    local escaped_status
    local escaped_workflow_started_utc
    local escaped_completed_at_utc
    local escaped_timings_file
    local escaped_build_log
    local escaped_coverage_collection_log
    local escaped_playwright_install_log

    json_escape_into escaped_slice_name "${slice_name}"
    json_escape_into escaped_slice_slug "${slice_slug}"
    json_escape_into escaped_status "${status}"
    json_escape_into escaped_workflow_started_utc "${workflow_started_utc}"
    json_escape_into escaped_completed_at_utc "${completed_at_utc}"
    json_escape_into escaped_timings_file "${timings_file}"
    json_escape_into escaped_build_log "${build_log}"
    json_escape_into escaped_coverage_collection_log "${coverage_collection_log}"
    json_escape_into escaped_playwright_install_log "${playwright_install_log}"

    {
        echo '{'
        printf '  "job_type": "test_slice",\n'
        printf '  "slice_name": "%s",\n' "${escaped_slice_name}"
        printf '  "slice_slug": "%s",\n' "${escaped_slice_slug}"
        printf '  "status": "%s",\n' "${escaped_status}"
        printf '  "workflow_started_at_utc": "%s",\n' "${escaped_workflow_started_utc}"
        printf '  "completed_at_utc": "%s",\n' "${escaped_completed_at_utc}"
        printf '  "install_playwright": %s,\n' "${install_playwright}"
        echo '  "files": {'
        printf '    "timing_file": "%s",\n' "${escaped_timings_file}"
        printf '    "build_log": "%s",\n' "${escaped_build_log}"
        printf '    "coverage_collection_log": "%s",\n' "${escaped_coverage_collection_log}"

        if [[ "${install_playwright}" == "true" ]]; then
            printf '    "playwright_install_log": "%s"\n' "${escaped_playwright_install_log}"
        else
            printf '    "playwright_install_log": null\n'
        fi

        echo '  },'
        echo '  "projects": ['

        local index
        local escaped_project
        for index in "${!projects[@]}"; do
            json_escape_into escaped_project "${projects[${index}]}"
            printf '    "%s"' "${escaped_project}"

            if (( index + 1 < ${#projects[@]} )); then
                printf ','
            fi

            printf '\n'
        done

        echo '  ],'
        echo '  "phases": ['

        local first_phase=true
        local phase
        local description
        local started_at_utc
        local phase_completed_at_utc
        local duration_seconds
        local phase_status
        local log_file
        local escaped_phase
        local escaped_description
        local escaped_started_at_utc
        local escaped_phase_completed_at_utc
        local escaped_duration_seconds
        local escaped_phase_status
        local escaped_log_file

        while IFS=$'\t' read -r phase description started_at_utc phase_completed_at_utc duration_seconds phase_status log_file; do
            if [[ "${phase}" == "phase" ]]; then
                continue
            fi

            json_escape_into escaped_phase "${phase}"
            json_escape_into escaped_description "${description}"
            json_escape_into escaped_started_at_utc "${started_at_utc}"
            json_escape_into escaped_phase_completed_at_utc "${phase_completed_at_utc}"
            json_escape_into escaped_duration_seconds "${duration_seconds}"
            json_escape_into escaped_phase_status "${phase_status}"
            json_escape_into escaped_log_file "${log_file}"

            if [[ "${first_phase}" == "false" ]]; then
                printf ',\n'
            fi

            printf '    {"name":"%s","description":"%s","started_at_utc":"%s","completed_at_utc":"%s","duration_seconds":%s,"status":"%s","log_file":"%s"}' \
                "${escaped_phase}" \
                "${escaped_description}" \
                "${escaped_started_at_utc}" \
                "${escaped_phase_completed_at_utc}" \
                "${escaped_duration_seconds}" \
                "${escaped_phase_status}" \
                "${escaped_log_file}"

            first_phase=false
        done < "${timings_file}"

        printf '\n'
        echo '  ]'
        echo '}'
    } > "${manifest_file}"

    return 0
}

cleanup() {
    local exit_code="$1"
    local completed_at_epoch
    local completed_at_utc
    local duration_seconds
    local total_status="success"

    trap - EXIT

    completed_at_epoch=$(date +%s)
    completed_at_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')
    duration_seconds=$((completed_at_epoch - workflow_started_epoch))

    if [[ ${exit_code} -ne 0 ]]; then
        total_status="failure"
    fi

    write_timing_row \
        "total_slice" \
        "${slice_name}" \
        "${workflow_started_utc}" \
        "${completed_at_utc}" \
        "${duration_seconds}" \
        "${total_status}" \
        "-"

    write_manifest \
        "${manifest_file}" \
        "${slice_name}" \
        "${slice_slug}" \
        "${install_playwright}" \
        "${total_status}" \
        "${workflow_started_utc}" \
        "${completed_at_utc}" \
        "${timings_file}" \
        "${build_log}" \
        "${coverage_collection_log}" \
        "${playwright_install_log}"

    return "${exit_code}"
}

main() {
    slice_name=""
    install_playwright=false
    projects_file=""

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
            --projects-file)
                projects_file="$2"
                shift 2
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

    if [[ -z "${slice_name}" ]]; then
        usage
    fi

    projects=()

    if [[ -n "${projects_file}" ]]; then
        while IFS= read -r project_path; do
            [[ -z "${project_path}" ]] && continue
            projects+=("${project_path}")
        done < "${projects_file}"
    else
        projects=("$@")
    fi

    if [[ ${#projects[@]} -eq 0 ]]; then
        usage
    fi

    slice_slug="$(printf '%s' "${slice_name}" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')"

    build_log="TestResults/${slice_slug}-build.log"
    coverage_reports_file="TestResults/${slice_slug}-coverage-reports.txt"
    coverage_collection_log="TestResults/${slice_slug}-coverage-collection.log"
    playwright_install_log="TestResults/${slice_slug}-playwright-install.log"
    timings_file="TestResults/${slice_slug}-phase-timings.tsv"
    manifest_file="TestResults/${slice_slug}-manifest.json"

    mkdir -p TestResults
    printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
        "phase" \
        "description" \
        "started_at_utc" \
        "completed_at_utc" \
        "duration_seconds" \
        "status" \
        "log_file" > "${timings_file}"

    workflow_started_epoch=$(date +%s)
    workflow_started_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

    trap 'cleanup "$?"; exit $?' EXIT

    run_with_log "build_projects" "Building ${slice_name} test projects" "${build_log}" \
        build_projects "${projects[@]}"

    if [[ "${install_playwright}" == "true" ]]; then
        local playwright_script
        playwright_script="$(find_playwright_script)"

        run_with_log "install_playwright" "Installing Playwright browsers for ${slice_name}" "${playwright_install_log}" \
            bash scripts/install-playwright.sh "${playwright_script}" chromium
    fi

    run_with_log "collect_test_coverage" "Running ${slice_name} tests with coverage" "${coverage_collection_log}" \
        bash scripts/collect-test-coverage.sh "${coverage_reports_file}" "${projects[@]}"

    write_summary "${slice_name}" "${timings_file}"

    return 0
}

main "$@"
