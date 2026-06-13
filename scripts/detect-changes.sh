#!/usr/bin/env bash

set -euo pipefail

set_output() {
    local name="$1"
    local value="$2"

    echo "${name}=${value}" >> "${GITHUB_OUTPUT}"

    return 0
}

set_change_outputs() {
    local build_required_value="$1"
    local fast_validation_required_value="$2"
    local admin_integration_required_value="$3"
    local admin_system_required_value="$4"
    local mediator_heavy_required_value="$5"

    set_output build_required "${build_required_value}"
    set_output fast_validation_required "${fast_validation_required_value}"
    set_output admin_integration_required "${admin_integration_required_value}"
    set_output admin_system_required "${admin_system_required_value}"
    set_output mediator_heavy_required "${mediator_heavy_required_value}"

    return 0
}

require_all_validation() {
    local message="$1"

    echo "${message}"
    set_change_outputs true true true true true

    return 0
}

matches_any_pattern() {
    local file="$1"

    shift

    local pattern
    for pattern in "$@"; do
        # shellcheck disable=SC2053
        if [[ "${file}" == ${pattern} ]]; then
            return 0
        fi

        case "${file}" in
            *)
                ;;
        esac
    done

    return 1
}

if [[ -z "${GITHUB_OUTPUT:-}" ]]; then
    echo "GITHUB_OUTPUT is required" >&2
    exit 1
fi

shared_validation_patterns=(
    ".github/actions/**"
    ".github/workflows/**"
    ".config/dotnet-tools.json"
    "Directory.Build.props"
    "Directory.Build.targets"
    "Directory.Packages.props"
    "NuGet.Config"
    "global.json"
    "coverage.settings.xml"
    "scripts/**"
    "ViajantesTurismo.slnx"
)

admin_integration_patterns=(
    "src/ViajantesTurismo.Admin.ApiService/**"
    "src/ViajantesTurismo.Admin.Application/**"
    "src/ViajantesTurismo.Admin.Contracts/**"
    "src/ViajantesTurismo.Admin.Domain/**"
    "src/ViajantesTurismo.Admin.Infrastructure/**"
    "src/ViajantesTurismo.Resources/**"
    "src/ViajantesTurismo.ServiceDefaults/**"
    "tests/ViajantesTurismo.Admin.IntegrationTests/**"
    "tests/ViajantesTurismo.Admin.Testing/**"
)

admin_system_patterns=(
    "src/ViajantesTurismo.Admin.ApiService/**"
    "src/ViajantesTurismo.Admin.Infrastructure/**"
    "src/ViajantesTurismo.AppHost/**"
    "src/ViajantesTurismo.Management.Web/**"
    "src/ViajantesTurismo.Resources/**"
    "src/ViajantesTurismo.ServiceDefaults/**"
    "tests/ViajantesTurismo.Admin.SystemTests/**"
    "tests/ViajantesTurismo.Admin.Testing/**"
)

mediator_heavy_patterns=(
    "src/SharedKernel/SharedKernel.Mediator/**"
    "src/SharedKernel/SharedKernel.Mediator.Abstractions/**"
    "src/SharedKernel/SharedKernel.Mediator.Analyzers/**"
    "src/SharedKernel/SharedKernel.Mediator.CodeFixes/**"
    "src/SharedKernel/SharedKernel.Mediator.SourceGenerator/**"
    "tests/SharedKernel.Mediator.Analyzers.Tests/**"
    "tests/SharedKernel.Mediator.CodeFixes.Tests/**"
    "tests/SharedKernel.Mediator.GeneratorTests/**"
    "tests/SharedKernel.Mediator.PackageConsumptionTests/**"
    "tests/SharedKernel.Mediator.Testing.ReferenceDispatcher/**"
    "tests/SharedKernel.Mediator.Tests/**"
)

if [[ "${GITHUB_EVENT_NAME:-}" == "pull_request" ]]; then
    if [[ -z "${GITHUB_BASE_SHA:-}" || -z "${GITHUB_HEAD_SHA:-}" ]]; then
        require_all_validation "Build and test will run because pull request SHAs are unavailable."
        exit 0
    fi

    diff_range="${GITHUB_BASE_SHA}...${GITHUB_HEAD_SHA}"
elif [[ "${GITHUB_EVENT_NAME:-}" == "push" ]]; then
    if [[ "${GITHUB_BEFORE_SHA:-}" == "0000000000000000000000000000000000000000" ]]; then
        require_all_validation "Build and test will run for the first push on a branch."
        exit 0
    fi

    if [[ -z "${GITHUB_BEFORE_SHA:-}" || -z "${GITHUB_AFTER_SHA:-}" ]]; then
        require_all_validation "Build and test will run because push SHAs are unavailable."
        exit 0
    fi

    diff_range="${GITHUB_BEFORE_SHA}..${GITHUB_AFTER_SHA}"
else
    require_all_validation "Build and test will run for event '${GITHUB_EVENT_NAME:-unknown}'."
    exit 0
fi

build_required=false
fast_validation_required=false
admin_integration_required=false
admin_system_required=false
mediator_heavy_required=false
changed_files=""

if ! changed_files="$(git diff --name-only "${diff_range}")"; then
    require_all_validation "Build and test will run because the change range '${diff_range}' could not be evaluated."
    exit 0
fi

while IFS= read -r file; do
    if [[ -z "${file}" ]]; then
        continue
    fi

    case "${file}" in
        docs/** | README.md | CONTRIBUTING.md) ;;
        *)
            build_required=true
            fast_validation_required=true
            ;;
    esac

    shared_validation_match=false
    set +e
    matches_any_pattern "${file}" "${shared_validation_patterns[@]}"
    shared_validation_status=$?
    set -e
    if [[ ${shared_validation_status} -eq 0 ]]; then
        shared_validation_match=true
    fi

    if [[ "${shared_validation_match}" == "true" ]]; then
        admin_integration_required=true
        admin_system_required=true
        mediator_heavy_required=true
        continue
    fi

    admin_integration_match=false
    set +e
    matches_any_pattern "${file}" "${admin_integration_patterns[@]}"
    admin_integration_status=$?
    set -e
    if [[ ${admin_integration_status} -eq 0 ]]; then
        admin_integration_match=true
    fi

    if [[ "${admin_integration_match}" == "true" ]]; then
        admin_integration_required=true
    fi

    admin_system_match=false
    set +e
    matches_any_pattern "${file}" "${admin_system_patterns[@]}"
    admin_system_status=$?
    set -e
    if [[ ${admin_system_status} -eq 0 ]]; then
        admin_system_match=true
    fi

    if [[ "${admin_system_match}" == "true" ]]; then
        admin_system_required=true
    fi

    mediator_heavy_match=false
    set +e
    matches_any_pattern "${file}" "${mediator_heavy_patterns[@]}"
    mediator_heavy_status=$?
    set -e
    if [[ ${mediator_heavy_status} -eq 0 ]]; then
        mediator_heavy_match=true
    fi

    if [[ "${mediator_heavy_match}" == "true" ]]; then
        mediator_heavy_required=true
    fi
done <<< "${changed_files}"

if [[ "${build_required}" == "true" ]]; then
    echo "Build and test will run because non-documentation changes were detected."
else
    echo "Build and test will be skipped because only documentation changes were detected."
fi

set_change_outputs \
    "${build_required}" \
    "${fast_validation_required}" \
    "${admin_integration_required}" \
    "${admin_system_required}" \
    "${mediator_heavy_required}"
