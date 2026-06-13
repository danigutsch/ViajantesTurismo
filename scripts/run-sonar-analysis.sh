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

mkdir -p TestResults

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

run_with_log "Starting SonarScanner" "${sonar_begin_log}" \
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

cleanup() {
    local exit_code="$1"

    trap - EXIT

    if [[ ${exit_code} -eq 0 ]]; then
        local sonar_end_exit_code

        set +e
        run_with_log "Finalizing SonarScanner" "${sonar_end_log}" \
            dotnet tool run dotnet-sonarscanner end "/d:sonar.token=${sonar_token}"
        sonar_end_exit_code="$?"
        set -e

        if [[ ${sonar_end_exit_code} -eq 0 ]]; then
            grep -F "QUALITY GATE STATUS:" "${sonar_end_log}" || true
        else
            exit_code="${sonar_end_exit_code}"
        fi
    fi

    return "${exit_code}"
}

trap 'cleanup "$?"; exit $?' EXIT

run_with_log "Building solution" "${build_log}" \
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

run_with_log "Installing Playwright browsers" "${playwright_install_log}" \
    bash scripts/install-playwright.sh "${playwright_script}"

run_with_log "Running tests with coverage" "${coverage_collection_log}" \
    bash scripts/collect-test-coverage.sh "${coverage_reports_file}"

coverage_reports="$(< "${coverage_reports_file}")"

rm -rf "${coverage_html_dir}"

run_with_log "Generating coverage report" "${reportgenerator_log}" \
    dotnet tool run reportgenerator \
        "-reports:${coverage_reports}" \
        "-targetdir:${coverage_html_dir}" \
        "-reporttypes:Html;SonarQube"

cp "${coverage_html_dir}/SonarQube.xml" "${coverage_report}"
