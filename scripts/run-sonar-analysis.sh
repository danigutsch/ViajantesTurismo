#!/usr/bin/env bash

set -euo pipefail

sonar_token="${SONAR_TOKEN:-}"
sonar_organization="${SONAR_ORGANIZATION:-}"
sonar_project_key="${SONAR_PROJECT_KEY:-}"

if [[ -z "${sonar_token}" ]]; then
    echo "Missing required environment variable: SONAR_TOKEN" >&2
    exit 1
fi

if [[ -z "${sonar_organization}" ]]; then
    echo "Missing required environment variable: SONAR_ORGANIZATION" >&2
    exit 1
fi

if [[ -z "${sonar_project_key}" ]]; then
    echo "Missing required environment variable: SONAR_PROJECT_KEY" >&2
    exit 1
fi

if [[ "${GITHUB_ACTIONS:-}" == "true" ]]; then
    echo "::add-mask::${sonar_token}"
fi

coverage_report="TestResults/sonar-coverage.xml"

mkdir -p TestResults

dotnet tool run dotnet-sonarscanner begin \
    "/o:${sonar_organization}" \
    "/k:${sonar_project_key}" \
    "/d:sonar.token=${sonar_token}" \
    "/d:sonar.cs.vscoveragexml.reportsPaths=${coverage_report}" \
    "/d:sonar.qualitygate.wait=true" \
    "/d:sonar.qualitygate.timeout=300"

cleanup() {
    exit_code=$?

    if [[ ${exit_code} -eq 0 ]]; then
        dotnet tool run dotnet-sonarscanner end "/d:sonar.token=${sonar_token}"
    fi

    exit "${exit_code}"
}

trap cleanup EXIT

dotnet build ViajantesTurismo.slnx --no-restore

playwright_script="$(find tests -name playwright.ps1 -path '*/bin/Debug/*' | head -1)"

if [[ -z "${playwright_script}" ]]; then
    echo "Playwright install script was not found under tests/*/bin/Debug after build." >&2
    exit 1
fi

pwsh "${playwright_script}" install --with-deps

dotnet tool run dotnet-coverage collect \
    "dotnet test --solution ViajantesTurismo.slnx --no-build" \
    -f xml \
    -o "${coverage_report}"
