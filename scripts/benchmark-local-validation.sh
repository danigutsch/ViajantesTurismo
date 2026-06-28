#!/usr/bin/env bash

set -euo pipefail

usage() {
    local exit_code="${1:-1}"

    cat >&2 << 'EOF'
Usage: bash scripts/benchmark-local-validation.sh [--skip-restore] [--skip-build] [--skip-tests] [--all-slices] [--slice <name>] [--output <path>]

Slice names: fast-validation, admin-integration, admin-system, mediator-heavy
EOF

    return "${exit_code}"
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
}

run_timed() {
    local phase="$1"
    local description="$2"
    local log_file="$3"

    shift 3

    local started_at_epoch
    local started_at_utc
    local completed_at_epoch
    local completed_at_utc
    local duration_seconds
    local exit_code
    local status

    started_at_epoch=$(date +%s)
    started_at_utc=$(date -u +'%Y-%m-%dT%H:%M:%SZ')

    echo "==> ${description}"

    set +e
    "$@" > "${log_file}" 2>&1
    exit_code="$?"
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

    if [[ ${exit_code} -ne 0 ]]; then
        echo "${description} failed. See ${log_file}." >&2
    fi

    return "${exit_code}"
}

run_test_slice() {
    local projects_file="$1"
    local project_path
    local exit_code=0

    while IFS= read -r project_path || [[ -n "${project_path}" ]]; do
        [[ -z "${project_path}" ]] && continue
        if ! dotnet test --project "${project_path}" --no-restore --no-build; then
            exit_code=1
        fi
    done < "${projects_file}"

    return "${exit_code}"
}

validate_slices() {
    local slice
    local projects_file

    for slice in "$@"; do
        projects_file="scripts/ci-test-slices/${slice}.txt"
        if [[ ! -f "${projects_file}" ]]; then
            echo "Unknown slice '${slice}'. Expected ${projects_file}." >&2
            return 1
        fi
    done
}

main() {
    local skip_restore=false
    local skip_build=false
    local skip_tests=false
    local all_slices=false
    local output="TestResults/local-validation-benchmark-timings.tsv"
    local -a requested_slices=()
    local -a default_slices=(fast-validation admin-integration mediator-heavy admin-system)

    while [[ $# -gt 0 ]]; do
        local argument="$1"

        case "${argument}" in
            --skip-restore)
                skip_restore=true
                shift
                ;;
            --skip-build)
                skip_build=true
                shift
                ;;
            --skip-tests)
                skip_tests=true
                shift
                ;;
            --all-slices)
                all_slices=true
                shift
                ;;
            --slice)
                if [[ $# -lt 2 ]]; then
                    usage 1
                fi

                local slice_name="$2"
                requested_slices+=("${slice_name}")
                shift 2
                ;;
            --output)
                if [[ $# -lt 2 ]]; then
                    usage 1
                fi

                local output_path="$2"
                output="${output_path}"
                shift 2
                ;;
            -h | --help)
                usage 0
                return 0
                ;;
            *)
                echo "Unknown argument: ${argument}" >&2
                usage 1
                ;;
        esac
    done

    local -a slices=()
    if [[ "${skip_tests}" != true ]] && [[ "${all_slices}" == true || ${#requested_slices[@]} -gt 0 ]]; then
        slices=("${requested_slices[@]}")
        if [[ ${#slices[@]} -eq 0 ]]; then
            slices=("${default_slices[@]}")
        fi

        validate_slices "${slices[@]}"
    fi

    mkdir -p "$(dirname "${output}")"
    timings_file="${output}"
    printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' \
        "phase" \
        "description" \
        "started_at_utc" \
        "completed_at_utc" \
        "duration_seconds" \
        "status" \
        "log_file" > "${timings_file}"

    if [[ "${skip_restore}" != true ]]; then
        run_timed \
            "restore" \
            "Restore solution in locked mode" \
            "${output%.tsv}-restore.log" \
            dotnet restore ViajantesTurismo.slnx --locked-mode
    fi

    if [[ "${skip_build}" != true ]]; then
        run_timed \
            "build" \
            "Build solution without restore" \
            "${output%.tsv}-build.log" \
            dotnet build ViajantesTurismo.slnx --no-restore
    fi

    if [[ ${#slices[@]} -gt 0 ]]; then

        local slice
        local projects_file
        for slice in "${slices[@]}"; do
            projects_file="scripts/ci-test-slices/${slice}.txt"
            run_timed \
                "test_${slice}" \
                "Run ${slice} test slice without restore or build" \
                "${output%.tsv}-${slice}.log" \
                run_test_slice "${projects_file}"
        done
    fi

    echo "Timing data written to ${timings_file}."
}

main "$@"
