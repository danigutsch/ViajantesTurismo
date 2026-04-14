#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "${script_dir}/.." && pwd)"
workspace_folder="${repo_root}"
log_dir="${DEVCONTAINER_SMOKE_LOG_DIR:-${repo_root}/TestResults/devcontainer-smoke}"
devcontainer_cli_version="${DEVCONTAINER_CLI_VERSION:-0.84.1}"
keep_container="${DEVCONTAINER_SMOKE_KEEP_CONTAINER:-0}"
run_tests="${DEVCONTAINER_SMOKE_RUN_TESTS:-0}"
container_id=""

print_step() {
    local message="$1"

    printf "\n==> %s\n" "${message}"
    return 0
}

require_command() {
    local command_name="$1"

    if ! command -v "${command_name}" >/dev/null 2>&1; then
        printf "Required command '%s' was not found on PATH.\n" "${command_name}" >&2
        exit 1
    fi

    return 0
}

run_devcontainer_cli() {
    local args=("$@")

    npm exec --yes --package "@devcontainers/cli@${devcontainer_cli_version}" -- "${args[@]}"
    return 0
}

extract_container_id() {
    local up_log_path="$1"

    grep -aoE '"containerId":"[^"]+' "${up_log_path}" | tail -1 | cut -d'"' -f4 || true
    return 0
}

write_metadata() {
    local metadata_path="$1"

    cat >"${metadata_path}" <<EOF
workspace_folder=${workspace_folder}
log_dir=${log_dir}
devcontainer_cli_version=${devcontainer_cli_version}
run_tests=${run_tests}
container_id=${container_id}
EOF

    return 0
}

cleanup() {
    local exit_code="$1"

    trap - EXIT

    if [[ -n "${container_id}" ]]; then
        if [[ "${keep_container}" == "1" ]]; then
            printf "Keeping devcontainer '%s' because DEVCONTAINER_SMOKE_KEEP_CONTAINER=1.\n" "${container_id}"
        else
            print_step "Cleaning up devcontainer ${container_id}"
            docker rm -f "${container_id}" >/dev/null 2>&1 || true
        fi
    fi

    if [[ "${exit_code}" -ne 0 ]]; then
        printf "Devcontainer smoke validation failed. Inspect logs under '%s'.\n" "${log_dir}" >&2
    fi

    return "${exit_code}"
}

main() {
    local up_log_path="${log_dir}/devcontainer-up.log"
    local verify_log_path="${log_dir}/devcontainer-verify.log"
    local playwright_log_path="${log_dir}/devcontainer-playwright.log"
    local test_log_path="${log_dir}/devcontainer-test.log"
    local metadata_path="${log_dir}/metadata.txt"
    local up_exit_code="0"
    local workspace_folder_override=""
    local current_arg=""

    while [[ $# -gt 0 ]]; do
        current_arg="$1"

        case "${current_arg}" in
            --run-tests)
                run_tests="1"
                ;;
            --help|-h)
                printf "Usage: %s [--run-tests] [workspace-folder]\n" "$0"
                exit 0
                ;;
            --*)
                printf "Unknown option '%s'.\n" "${current_arg}" >&2
                printf "Usage: %s [--run-tests] [workspace-folder]\n" "$0" >&2
                exit 1
                ;;
            *)
                if [[ -n "${workspace_folder_override}" ]]; then
                    printf "Usage: %s [--run-tests] [workspace-folder]\n" "$0" >&2
                    exit 1
                fi

                workspace_folder_override="${current_arg}"
                ;;
        esac

        shift
    done

    if [[ -n "${workspace_folder_override}" ]]; then
        workspace_folder="$(cd -- "${workspace_folder_override}" && pwd)"
    fi

    if [[ ! -f "${workspace_folder}/.devcontainer/devcontainer.json" ]]; then
        printf "Could not find .devcontainer/devcontainer.json under '%s'.\n" "${workspace_folder}" >&2
        exit 1
    fi

    require_command docker
    require_command npm

    mkdir -p "${log_dir}"

    trap 'cleanup "$?"' EXIT

    print_step "Building and starting the devcontainer"
    set +e
    run_devcontainer_cli devcontainer up --workspace-folder "${workspace_folder}" --log-level trace \
        2>&1 | tee "${up_log_path}"
    up_exit_code="${PIPESTATUS[0]}"
    set -e

    container_id="$(extract_container_id "${up_log_path}")"
    if [[ -n "${container_id}" ]]; then
        write_metadata "${metadata_path}"
    fi

    if [[ "${up_exit_code}" -ne 0 ]]; then
        printf "devcontainer up failed. Inspect '%s' for details.\n" "${up_log_path}" >&2
        return "${up_exit_code}"
    fi

    if [[ -z "${container_id}" ]]; then
        printf "Failed to determine the devcontainer container ID from '%s'.\n" "${up_log_path}" >&2
        return 1
    fi

    print_step "Verifying toolchains inside the devcontainer"
    run_devcontainer_cli devcontainer exec --workspace-folder "${workspace_folder}" bash -lc '
        set -euo pipefail
        dotnet --version
        node --version
        git --version
        docker version --format "{{.Server.Version}}"
    ' 2>&1 | tee "${verify_log_path}"

    if [[ "${run_tests}" == "1" ]]; then
        print_step "Installing Playwright browsers inside the devcontainer"
        run_devcontainer_cli devcontainer exec --workspace-folder "${workspace_folder}" bash -lc '
            set -euo pipefail
            bash scripts/install-playwright.sh
        ' 2>&1 | tee "${playwright_log_path}"

        print_step "Running test suite inside the devcontainer"
        run_devcontainer_cli devcontainer exec --workspace-folder "${workspace_folder}" bash -lc '
            set -euo pipefail
            dotnet test --solution ViajantesTurismo.slnx --no-build
        ' 2>&1 | tee "${test_log_path}"
    fi

    print_step "Devcontainer smoke validation completed successfully"
    printf "Logs written to '%s'.\n" "${log_dir}"
    return 0
}

main "$@"
