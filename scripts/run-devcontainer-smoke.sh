#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd -- "${script_dir}/.." && pwd)"
workspace_folder="${repo_root}"
log_dir="${DEVCONTAINER_SMOKE_LOG_DIR:-${repo_root}/TestResults/devcontainer-smoke}"
devcontainer_cli_version="${DEVCONTAINER_CLI_VERSION:-0.84.1}"
devcontainer_node_version="${DEVCONTAINER_NODE_VERSION:-20.20.1}"
devcontainer_cli_prefix="${DEVCONTAINER_CLI_PREFIX:-${repo_root}/TestResults/devcontainer-tools/devcontainers-cli-${devcontainer_cli_version}}"
keep_container="${DEVCONTAINER_SMOKE_KEEP_CONTAINER:-0}"
run_tests="${DEVCONTAINER_SMOKE_RUN_TESTS:-0}"
container_id=""
devcontainer_up_args=()

download_with_tls() {
    local url="$1"
    local output_path="$2"

    curl \
        --fail \
        --silent \
        --show-error \
        --location \
        --proto '=https' \
        --proto-redir '=https' \
        --tlsv1.2 \
        --output "${output_path}" \
        "${url}"

    return 0
}

write_devcontainer_wrapper() {
    local wrapper_path="$1"
    local node_version_dir="$2"
    local cli_version_dir="$3"

    cat >"${wrapper_path}" <<EOF
#!/usr/bin/env bash
set -euo pipefail
exec "${node_version_dir}/bin/node" "${cli_version_dir}/package/devcontainer.js" "\$@"
EOF

    chmod +x "${wrapper_path}"
    return 0
}

verify_node_archive() {
    local node_archive="$1"
    local checksums_file="$2"

    (
        cd -- "$(dirname "${node_archive}")"
        grep "  $(basename "${node_archive}")$" "${checksums_file}" | sha256sum --check --status
    )

    return 0
}

verify_cli_archive() {
    local cli_archive="$1"
    local metadata_file="$2"

    python3 - "${cli_archive}" "${metadata_file}" <<'PY'
import base64
import hashlib
import json
import sys

archive_path = sys.argv[1]
metadata_path = sys.argv[2]

with open(metadata_path, encoding="utf-8") as metadata_stream:
    metadata = json.load(metadata_stream)

integrity = metadata["dist"]["integrity"]
algorithm, encoded_digest = integrity.split("-", 1)
if algorithm != "sha512":
    raise SystemExit(f"Unsupported npm integrity algorithm: {algorithm}")

expected_digest = base64.b64decode(encoded_digest)

archive_hash = hashlib.sha512()
with open(archive_path, "rb") as archive_stream:
    for chunk in iter(lambda: archive_stream.read(1024 * 1024), b""):
        archive_hash.update(chunk)

if archive_hash.digest() != expected_digest:
    raise SystemExit("Dev Container CLI archive integrity check failed.")
PY

    return 0
}

get_cli_tarball_url() {
    local metadata_file="$1"

    python3 - "${metadata_file}" <<'PY'
import json
import sys

metadata_path = sys.argv[1]

with open(metadata_path, encoding="utf-8") as metadata_stream:
    metadata = json.load(metadata_stream)

print(metadata["dist"]["tarball"])
PY

    return 0
}

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

require_supported_devcontainer_cli_host() {
    local os_name
    local architecture_name

    os_name="$(uname -s)"
    architecture_name="$(uname -m)"

    if [[ "${os_name}" != "Linux" || "${architecture_name}" != "x86_64" ]]; then
        printf "The repo-owned Dev Container CLI bootstrap currently supports only Linux x86_64 hosts.\n" >&2
        printf "Current host: %s %s\n" "${os_name}" "${architecture_name}" >&2
        printf "Set DEVCONTAINER_CLI_PREFIX to a compatible preinstalled CLI path or use the supported host path.\n" >&2
        exit 1
    fi

    return 0
}

run_devcontainer_cli() {
    local args=("$@")

    "${devcontainer_cli_prefix}/bin/devcontainer" "${args[@]}"
}

ensure_devcontainer_cli() {
    if [[ -x "${devcontainer_cli_prefix}/bin/devcontainer" ]]; then
        return 0
    fi

    local tmp_dir
    local node_archive
    local node_checksums
    local cli_metadata
    local cli_archive
    local node_root
    local cli_root
    local cli_tarball_url

    require_command curl
    require_command python3
    require_command tar
    require_command sha256sum
    require_supported_devcontainer_cli_host

    (
        set -euo pipefail

        tmp_dir="$(mktemp -d)"
        trap 'rm -rf "${tmp_dir}"' EXIT

        node_archive="${tmp_dir}/node-v${devcontainer_node_version}-linux-x64.tar.xz"
        node_checksums="${tmp_dir}/SHASUMS256.txt"
        cli_metadata="${tmp_dir}/devcontainer-cli-metadata.json"
        cli_archive="${tmp_dir}/cli-${devcontainer_cli_version}.tgz"
        node_root="${devcontainer_cli_prefix}/node/v${devcontainer_node_version}"
        cli_root="${devcontainer_cli_prefix}/cli/${devcontainer_cli_version}"

        mkdir -p "${devcontainer_cli_prefix}/bin" "${devcontainer_cli_prefix}/node" "${devcontainer_cli_prefix}/cli"

        download_with_tls \
            "https://nodejs.org/dist/v${devcontainer_node_version}/$(basename "${node_archive}")" \
            "${node_archive}"
        download_with_tls \
            "https://nodejs.org/dist/v${devcontainer_node_version}/SHASUMS256.txt" \
            "${node_checksums}"
        verify_node_archive "${node_archive}" "${node_checksums}"

        download_with_tls \
            "https://registry.npmjs.org/@devcontainers/cli/${devcontainer_cli_version}" \
            "${cli_metadata}"
        cli_tarball_url="$(get_cli_tarball_url "${cli_metadata}")"
        download_with_tls \
            "${cli_tarball_url}" \
            "${cli_archive}"
        verify_cli_archive "${cli_archive}" "${cli_metadata}"

        rm -rf "${node_root}" "${cli_root}"
        mkdir -p "${node_root}" "${cli_root}"

        tar -xJf "${node_archive}" --strip-components=1 -C "${node_root}"
        tar -xzf "${cli_archive}" -C "${cli_root}"

        write_devcontainer_wrapper \
            "${devcontainer_cli_prefix}/bin/devcontainer" \
            "${node_root}" \
            "${cli_root}"
    )

    return 0
}

extract_container_id() {
    local up_log_path="$1"

    grep -aoE '"containerId":"[^"]+' "${up_log_path}" | tail -1 | cut -d'"' -f4 || true
    return 0
}

devcontainer_up_reported_error() {
    local up_log_path="$1"

    grep -q '"outcome":"error"' "${up_log_path}"
    return $?
}

configure_git_worktree_mount() {
    local git_common_dir=""
    local git_common_dir_absolute=""

    if [[ ! -f "${workspace_folder}/.git" ]]; then
        return 0
    fi

    git_common_dir="$(git -C "${workspace_folder}" rev-parse --git-common-dir 2>/dev/null || true)"
    if [[ -z "${git_common_dir}" ]]; then
        return 0
    fi

    if [[ "${git_common_dir}" == /* ]]; then
        git_common_dir_absolute="${git_common_dir}"
    else
        git_common_dir_absolute="$(cd -- "${workspace_folder}/${git_common_dir}" && pwd)"
    fi

    devcontainer_up_args+=(--mount "type=bind,source=${git_common_dir_absolute},target=${git_common_dir_absolute}")
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
    local up_reported_error="1"
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
    ensure_devcontainer_cli
    configure_git_worktree_mount

    mkdir -p "${log_dir}"

    trap 'cleanup "$?"' EXIT

    print_step "Building and starting the devcontainer"
    set +e
    run_devcontainer_cli up --workspace-folder "${workspace_folder}" --log-level trace "${devcontainer_up_args[@]}" \
        2>&1 | tee "${up_log_path}"
    up_exit_code="${PIPESTATUS[0]}"
    set -e

    container_id="$(extract_container_id "${up_log_path}")"
    if [[ -n "${container_id}" ]]; then
        write_metadata "${metadata_path}"
    fi

    set +e
    devcontainer_up_reported_error "${up_log_path}"
    up_reported_error="$?"
    set -e

    if [[ "${up_exit_code}" -ne 0 || "${up_reported_error}" -eq 0 ]]; then
        printf "devcontainer up failed. Inspect '%s' for details.\n" "${up_log_path}" >&2
        if [[ "${up_exit_code}" -ne 0 ]]; then
            return "${up_exit_code}"
        fi

        return 1
    fi

    if [[ -z "${container_id}" ]]; then
        printf "Failed to determine the devcontainer container ID from '%s'.\n" "${up_log_path}" >&2
        return 1
    fi

    print_step "Verifying toolchains inside the devcontainer"
    run_devcontainer_cli exec --workspace-folder "${workspace_folder}" bash -lc '
        set -euo pipefail
        dotnet --version
        git --version
        docker version --format "{{.Server.Version}}"
    ' 2>&1 | tee "${verify_log_path}"

    if [[ "${run_tests}" == "1" ]]; then
        print_step "Installing Playwright browsers inside the devcontainer"
        run_devcontainer_cli exec --workspace-folder "${workspace_folder}" bash -lc '
            set -euo pipefail
            DEVCONTAINER_SMOKE=1 bash scripts/install-playwright.sh chromium
        ' 2>&1 | tee "${playwright_log_path}"

        print_step "Running test suite inside the devcontainer"
        run_devcontainer_cli exec --workspace-folder "${workspace_folder}" bash -lc '
            set -euo pipefail
            export GITHUB_ACTIONS=true
            bash scripts/run-ci-test-slice.sh \
                --slice-name "Devcontainer Fast Validation" \
                --projects-file scripts/ci-test-slices/fast-validation.txt
            bash scripts/run-ci-test-slice.sh \
                --slice-name "Devcontainer Admin Integration Tests" \
                --projects-file scripts/ci-test-slices/admin-integration.txt
            bash scripts/run-ci-test-slice.sh \
                --slice-name "Devcontainer Mediator Heavy Tests" \
                --projects-file scripts/ci-test-slices/mediator-heavy.txt
            bash scripts/run-ci-test-slice.sh \
                --slice-name "Devcontainer Admin System Tests" \
                --projects-file scripts/ci-test-slices/admin-system.txt
        ' 2>&1 | tee "${test_log_path}"
    fi

    print_step "Devcontainer smoke validation completed successfully"
    printf "Logs written to '%s'.\n" "${log_dir}"
    return 0
}

main "$@"
