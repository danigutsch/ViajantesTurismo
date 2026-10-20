#!/bin/bash

set -euo pipefail

echo "🔧 Running one-time container setup..."

WORKSPACE_FOLDER="${1:-${PWD}}"
BUILD_DIR="${WORKSPACE_FOLDER}/.build"
DOTNET_INSTALL_DIR="/home/vscode/.dotnet"
DOTNET_ROOT_MARKER_BEGIN="# >>> ViajantesTurismo DOTNET_ROOT >>>"
DOTNET_ROOT_MARKER_END="# <<< ViajantesTurismo DOTNET_ROOT <<<"
DOTNET_ENV_FILE="${WORKSPACE_FOLDER}/.devcontainer/.dotnet-env"

required_sdk_version="$(sed -nE 's/^[[:space:]]*"version"[[:space:]]*:[[:space:]]*"([^"]+)".*/\1/p' "${WORKSPACE_FOLDER}/global.json" | head -n 1)"
if [[ -z "${required_sdk_version}" ]]; then
    echo "Could not read the required .NET SDK version from ${WORKSPACE_FOLDER}/global.json." >&2
    exit 1
fi

# Ensure expected writable caches/directories exist for the non-root user.
sudo mkdir -p \
    /home/vscode/.nuget \
    /home/vscode/.nuget/packages \
    /home/vscode/.local/share/NuGet/v3-cache \
    "${DOTNET_INSTALL_DIR}" \
    "${BUILD_DIR}"

sudo chown -R vscode:vscode \
    /home/vscode/.nuget \
    /home/vscode/.local/share/NuGet \
    "${DOTNET_INSTALL_DIR}" \
    "${BUILD_DIR}"

use_local_dotnet=false

bash "${WORKSPACE_FOLDER}/scripts/ensure-dotnet-sdk.sh" \
    --install \
    --global-json "${WORKSPACE_FOLDER}/global.json" \
    --install-dir "${DOTNET_INSTALL_DIR}"

if [[ -f "${DOTNET_INSTALL_DIR}/sdk/${required_sdk_version}/dotnet.dll" ]]; then
    use_local_dotnet=true
fi

if [[ "${use_local_dotnet}" == "true" ]]; then
    export PATH="${DOTNET_INSTALL_DIR}:${PATH}"
    cat > "${DOTNET_ENV_FILE}" <<EOF
export PATH="${DOTNET_INSTALL_DIR}:\$PATH"
EOF
else
    rm -f "${DOTNET_ENV_FILE}"
fi

resolved_sdk_version="$(dotnet --version)"

for profile in /home/vscode/.bashrc /home/vscode/.profile; do
    content=""
    if [[ -f "${profile}" ]]; then
        IFS= read -r -d '' content < "${profile}" || true
    fi
    if [[ "${content}" == *"${DOTNET_ROOT_MARKER_BEGIN}"* ]]; then
        prefix="${content%%"${DOTNET_ROOT_MARKER_BEGIN}"*}"
        suffix="${content#*"${DOTNET_ROOT_MARKER_BEGIN}"}"
        if [[ "${suffix}" == *"${DOTNET_ROOT_MARKER_END}"* ]]; then
            suffix="${suffix#*"${DOTNET_ROOT_MARKER_END}"}"
            while [[ "${suffix}" == $'\n'* ]]; do
                suffix="${suffix#$'\n'}"
            done
            content="${prefix}${suffix}"
        fi
    fi
    if [[ "${use_local_dotnet}" == "true" ]]; then
        if [[ -n "${content}" && "${content}" != *$'\n' ]]; then
            content+=$'\n'
        fi
        content+="${DOTNET_ROOT_MARKER_BEGIN}"$'\n'
        content+="export PATH=\"/home/vscode/.dotnet:\$PATH\""$'\n'
        content+="${DOTNET_ROOT_MARKER_END}"$'\n'
    fi
    printf '%s' "${content}" > "${profile}"
done

if [[ "${resolved_sdk_version}" != "${required_sdk_version}" ]]; then
    echo "Resolved .NET SDK ${resolved_sdk_version} does not match required ${required_sdk_version}." >&2
    exit 1
fi

dotnet tool restore

# Configure repository as safe for git once.
if ! git config --global --get-all safe.directory | grep -Fxq "${WORKSPACE_FOLDER}"; then
    git config --global --add safe.directory "${WORKSPACE_FOLDER}"
fi

echo "✅ One-time container setup completed."
