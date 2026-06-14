#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "${script_dir}/.." && pwd)"

cd "${repo_root}"

# shellcheck source=scripts/validate-sonar-analysis-config.sh
source "${script_dir}/validate-sonar-analysis-config.sh"
load_local_env "${repo_root}"

bash scripts/validate-sonar-analysis-config.sh

sonar_token="${SONAR_TOKEN:-}"
sonar_organization="${SONAR_ORGANIZATION:-}"
sonar_project_key="${SONAR_PROJECT_KEY:-}"
sonar_exclusions="**/Migrations/**,.devcontainer/**,.vscode/**"
sonar_coverage_exclusions="benchmarks/**,samples/**,scripts/**,tests/performance/**,src/SharedKernel/SharedKernel.Mediator.SourceGenerator/IsExternalInit.cs"
sonar_cpd_exclusions="benchmarks/**,src/SharedKernel/SharedKernel.Mediator.Analyzers/SharedKernelMediatorAnalyzer.cs,src/SharedKernel/SharedKernel.Mediator.CodeFixes/MissingHandlerCodeFix.cs,src/SharedKernel/SharedKernel.Mediator.CodeFixes/MissingRequestInterfaceCodeFix.cs,src/SharedKernel/SharedKernel.Mediator.SourceGenerator/AppMediatorEmitter.cs,src/SharedKernel/SharedKernel.Mediator.SourceGenerator/GeneratedDispatchEmitter.cs"

if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
    echo "::add-mask::${sonar_token}"
fi

coverage_report="TestResults/sonar-coverage.xml"
coverage_reports_file="TestResults/coverage-reports.txt"
coverage_html_dir="TestResults/CoverageReport"
build_log="TestResults/sonar-build.log"
playwright_install_log="TestResults/sonar-playwright-install.log"
coverage_collection_log="TestResults/sonar-coverage-collection.log"
reportgenerator_log="TestResults/sonar-reportgenerator.log"
sonar_begin_log="TestResults/sonarscanner-begin.log"
sonar_end_log="TestResults/sonarscanner-end.log"
skip_tests="${SONAR_ANALYSIS_SKIP_TESTS:-false}"
timing_file="TestResults/ci-phase-timings.tsv"
manifest_file="TestResults/ci-validation-manifest.json"

mkdir -p TestResults

workflow_started_epoch=$(date +%s)
workflow_started_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
    "phase" \
    "description" \
    "started_at_utc" \
    "completed_at_utc" \
    "duration_seconds" \
    "status" \
    "log_file" > "${timing_file}"

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
        "${log_file}" >> "${timing_file}"
}

write_manifest() {
    local manifest_file="$1"
    local status="$2"
    local workflow_started_utc="$3"
    local completed_at_utc="$4"
    local skip_tests="$5"
    local timing_file="$6"
    local build_log="$7"
    local playwright_install_log="$8"
    local coverage_collection_log="$9"
    local reportgenerator_log="${10}"
    local sonar_begin_log="${11}"
    local sonar_end_log="${12}"
    local coverage_report="${13}"
    local coverage_reports_file="${14}"
    local escaped_status
    local escaped_workflow_started_utc
    local escaped_completed_at_utc
    local escaped_timing_file
    local escaped_build_log
    local escaped_playwright_install_log
    local escaped_coverage_collection_log
    local escaped_reportgenerator_log
    local escaped_sonar_begin_log
    local escaped_sonar_end_log
    local escaped_coverage_report
    local escaped_coverage_reports_file

    json_escape_into escaped_status "${status}"
    json_escape_into escaped_workflow_started_utc "${workflow_started_utc}"
    json_escape_into escaped_completed_at_utc "${completed_at_utc}"
    json_escape_into escaped_timing_file "${timing_file}"
    json_escape_into escaped_build_log "${build_log}"
    json_escape_into escaped_playwright_install_log "${playwright_install_log}"
    json_escape_into escaped_coverage_collection_log "${coverage_collection_log}"
    json_escape_into escaped_reportgenerator_log "${reportgenerator_log}"
    json_escape_into escaped_sonar_begin_log "${sonar_begin_log}"
    json_escape_into escaped_sonar_end_log "${sonar_end_log}"
    json_escape_into escaped_coverage_report "${coverage_report}"
    json_escape_into escaped_coverage_reports_file "${coverage_reports_file}"

    {
        echo '{'
        printf '  "job_type": "sonarcloud",\n'
        printf '  "status": "%s",\n' "${escaped_status}"
        printf '  "workflow_started_at_utc": "%s",\n' "${escaped_workflow_started_utc}"
        printf '  "completed_at_utc": "%s",\n' "${escaped_completed_at_utc}"
        printf '  "skip_tests": %s,\n' "${skip_tests}"
        echo '  "files": {'
        printf '    "timing_file": "%s",\n' "${escaped_timing_file}"
        printf '    "coverage_report": "%s",\n' "${escaped_coverage_report}"
        printf '    "coverage_reports_file": "%s",\n' "${escaped_coverage_reports_file}"
        printf '    "build_log": "%s",\n' "${escaped_build_log}"
        printf '    "playwright_install_log": "%s",\n' "${escaped_playwright_install_log}"
        printf '    "coverage_collection_log": "%s",\n' "${escaped_coverage_collection_log}"
        printf '    "reportgenerator_log": "%s",\n' "${escaped_reportgenerator_log}"
        printf '    "sonar_begin_log": "%s",\n' "${escaped_sonar_begin_log}"
        printf '    "sonar_end_log": "%s"\n' "${escaped_sonar_end_log}"
        echo '  },'
        echo '  "artifacts": ['
        echo '    "coverage-report",'
        echo '    "sonar-coverage",'
        echo '    "sonar-analysis-log",'
        echo '    "sonar-analysis-manifest"'
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
        done < "${timing_file}"

        printf '\n'
        echo '  ]'
        echo '}'
    } > "${manifest_file}"

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

cleanup() {
    local exit_code="$1"
    local completed_at_epoch
    local completed_at_utc
    local duration_seconds
    local total_status="success"

    trap - EXIT

    if [[ ${exit_code} -eq 0 ]]; then
        local sonar_end_exit_code

        set +e
        run_with_log "sonar_end" "Finalizing SonarScanner" "${sonar_end_log}" \
            dotnet tool run dotnet-sonarscanner end "/d:sonar.token=${sonar_token}"
        sonar_end_exit_code="$?"
        set -e

        if [[ ${sonar_end_exit_code} -eq 0 ]]; then
            grep -F "QUALITY GATE STATUS:" "${sonar_end_log}" || true
        else
            exit_code="${sonar_end_exit_code}"
        fi
    fi

    completed_at_epoch=$(date +%s)
    completed_at_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')
    duration_seconds=$((completed_at_epoch - workflow_started_epoch))

    if [[ ${exit_code} -ne 0 ]]; then
        total_status="failure"
    fi

    write_timing_row \
        "total_validation" \
        "Build, test, coverage, and SonarCloud analysis" \
        "${workflow_started_utc}" \
        "${completed_at_utc}" \
        "${duration_seconds}" \
        "${total_status}" \
        "-"

    write_manifest \
        "${manifest_file}" \
        "${total_status}" \
        "${workflow_started_utc}" \
        "${completed_at_utc}" \
        "${skip_tests}" \
        "${timing_file}" \
        "${build_log}" \
        "${playwright_install_log}" \
        "${coverage_collection_log}" \
        "${reportgenerator_log}" \
        "${sonar_begin_log}" \
        "${sonar_end_log}" \
        "${coverage_report}" \
        "${coverage_reports_file}"

    return "${exit_code}"
}

trap 'cleanup "$?"; exit $?' EXIT

run_with_log "sonar_begin" "Starting SonarScanner" "${sonar_begin_log}" \
    dotnet tool run dotnet-sonarscanner begin \
        "/o:${sonar_organization}" \
        "/k:${sonar_project_key}" \
        "/d:sonar.token=${sonar_token}" \
        "/d:sonar.projectBaseDir=${repo_root}" \
        "/d:sonar.coverageReportPaths=${coverage_report}" \
        "/d:sonar.exclusions=${sonar_exclusions}" \
        "/d:sonar.coverage.exclusions=${sonar_coverage_exclusions}" \
        "/d:sonar.cpd.exclusions=${sonar_cpd_exclusions}" \
        "/d:sonar.qualitygate.wait=true" \
        "/d:sonar.qualitygate.timeout=300"

run_with_log "build_solution" "Building solution" "${build_log}" \
    dotnet build ViajantesTurismo.slnx --no-restore

if [[ "${skip_tests}" == "true" ]]; then
    if [[ ! -f "${coverage_report}" ]]; then
        echo "Expected coverage report '${coverage_report}' was not found for Sonar reuse mode." >&2
        exit 1
    fi

    echo "Skipping Playwright installation and test execution because SONAR_ANALYSIS_SKIP_TESTS=true."
    exit 0
fi

playwright_script="$(find tests -name playwright.ps1 -path '*/bin/Debug/*' | head -1)"

if [[ -z "${playwright_script}" ]]; then
    echo "Playwright install script was not found under tests/*/bin/Debug after build." >&2
    exit 1
fi

run_with_log "install_playwright" "Installing Playwright browsers" "${playwright_install_log}" \
    bash scripts/install-playwright.sh "${playwright_script}" chromium

run_with_log "collect_test_coverage" "Running tests with coverage" "${coverage_collection_log}" \
    bash scripts/collect-test-coverage.sh "${coverage_reports_file}"

coverage_reports="$(< "${coverage_reports_file}")"

rm -rf "${coverage_html_dir}"

run_with_log "generate_coverage_report" "Generating coverage report" "${reportgenerator_log}" \
    dotnet tool run reportgenerator \
        "-reports:${coverage_reports}" \
        "-targetdir:${coverage_html_dir}" \
        "-reporttypes:Html;SonarQube"

cp "${coverage_html_dir}/SonarQube.xml" "${coverage_report}"
