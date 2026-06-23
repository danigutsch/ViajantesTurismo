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

install_ubuntu_26_chromium_dependencies() {
    local -a apt_command=()
    local current_user_id=""
    local -a packages=(
        libasound2t64
        libatk-bridge2.0-0t64
        libatk1.0-0t64
        libatspi2.0-0t64
        libcairo2
        libcups2t64
        libdbus-1-3
        libdrm2
        libgbm1
        libglib2.0-0t64
        libnspr4
        libnss3
        libpango-1.0-0
        libx11-6
        libxcb1
        libxcomposite1
        libxdamage1
        libxext6
        libxfixes3
        libxkbcommon0
        libxrandr2
    )

    current_user_id="$(id -u)"

    if [[ "${current_user_id}" = "0" ]]; then
        apt_command=(apt-get)
    elif command -v sudo >/dev/null 2>&1; then
        apt_command=(sudo apt-get)
    else
        echo "sudo is required to install Ubuntu 26.04 Playwright Chromium dependencies." >&2
        return 1
    fi

    "${apt_command[@]}" update
    "${apt_command[@]}" install -y --no-install-recommends "${packages[@]}"

    return 0
}

main() {
    local playwright_script="${1:-}"
    local -a browser_args=()

    if [[ -n "${playwright_script}" && "${playwright_script}" != *.ps1 ]]; then
        browser_args=("$@")
        playwright_script=""
    elif [[ $# -gt 1 ]]; then
        browser_args=("${@:2}")
    fi

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

    if [[ ${#browser_args[@]} -gt 0 ]]; then
        install_args+=("${browser_args[@]}")
    fi

    local os_name
    os_name="$(uname -s)"

    if [[ "${os_name}" = "Linux" ]] && [[ -f /etc/os-release ]]; then
        # shellcheck disable=SC1091
        . /etc/os-release

        if [[ "${ID:-}" = "ubuntu" ]] && [[ "${VERSION_ID:-}" =~ ^(22\.04|24\.04|26\.04)$ ]]; then
            if [[ "${VERSION_ID:-}" = "26.04" && -z "${PLAYWRIGHT_HOST_PLATFORM_OVERRIDE:-}" ]]; then
                # Playwright docs list Ubuntu 26.04 as supported, but the pinned 1.60.0 package
                # still lacks ubuntu26.04 manifests. Track v1.61 support in microsoft/playwright#40117.
                case "$(uname -m)" in
                    x86_64 | amd64) export PLAYWRIGHT_HOST_PLATFORM_OVERRIDE="ubuntu24.04-x64" ;;
                    aarch64 | arm64) export PLAYWRIGHT_HOST_PLATFORM_OVERRIDE="ubuntu24.04-arm64" ;;
                    *) echo "Unknown Ubuntu 26.04 architecture; using Playwright default platform detection." >&2 ;;
                esac
            fi

            if [[ "${VERSION_ID:-}" = "26.04" && "${DEVCONTAINER_SMOKE:-}" = "1" ]]; then
                install_ubuntu_26_chromium_dependencies
                echo "Skipping Playwright --with-deps inside Ubuntu 26.04 devcontainers because Playwright 1.60.0 still uses Ubuntu 24.04 dependency manifests." >&2
            else
                install_args+=(--with-deps)
            fi
        else
            echo "Playwright system dependency installation is only used automatically on supported Ubuntu versions (22.04, 24.04, 26.04)." >&2
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
