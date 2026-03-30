#!/usr/bin/env bash

set -euo pipefail

print_linux_dependency_guidance() {
    cat >&2 <<'EOF'
If browser launch still fails on Linux after browser download, install the required runtime libraries manually.

Ubuntu example:
    sudo apt-get update
    sudo apt-get install -y libnspr4 libnss3 libasound2t64

Then rerun:
    bash scripts/install-playwright.sh
EOF

    return 0
}

main() {
    local playwright_script="${1:-}"

    if [[ -z "${playwright_script}" ]]; then
        local -a search_roots=()

        if [[ -d ".build" ]]; then
            search_roots+=(".build")
        fi

        if [[ -d "tests" ]]; then
            search_roots+=("tests")
        fi

        if [[ "${#search_roots[@]}" -gt 0 ]]; then
            playwright_script="$(find "${search_roots[@]}" -type f -name playwright.ps1 | head -1)"
        fi
    fi

    if [[ -z "${playwright_script}" ]]; then
        echo "Playwright install script was not found in the build output after build verification." >&2
        return 1
    fi

    if ! command -v pwsh >/dev/null 2>&1; then
        echo "PowerShell (pwsh) is required to run the generated Playwright installer." >&2
        return 1
    fi

    local -a install_args=(install)
    local os_name
    os_name="$(uname -s)"

    if [[ "${os_name}" = "Linux" ]] && [[ -f /etc/os-release ]]; then
        # shellcheck disable=SC1091
        . /etc/os-release

        if [[ "${ID:-}" = "ubuntu" ]] && [[ "${VERSION_ID:-}" =~ ^(22\.04|24\.04)$ ]]; then
            install_args+=(--with-deps)
        else
            echo "Playwright system dependency installation is only used automatically on supported Ubuntu versions (22.04, 24.04)." >&2
            echo "Current OS: ${PRETTY_NAME:-unknown}. Installing browsers without --with-deps." >&2
            print_linux_dependency_guidance
        fi
    else
        install_args+=(--with-deps)
    fi

    pwsh "${playwright_script}" "${install_args[@]}"

    return 0
}

main "$@"
