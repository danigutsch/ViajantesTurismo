#!/bin/bash

set -euo pipefail

echo "🔧 Running one-time container setup..."

WORKSPACE_FOLDER="${1:-${PWD}}"
BUILD_DIR="${WORKSPACE_FOLDER}/.build"
DOTNET_INSTALL_DIR="/home/vscode/.dotnet"

required_sdk_version="$(python3 - "${WORKSPACE_FOLDER}/global.json" <<'PY'
import json
import sys

with open(sys.argv[1], 'r', encoding='utf-8') as file:
    print(json.load(file)['sdk']['version'])
PY
)"

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

if ! dotnet --list-sdks | grep -q "^${required_sdk_version} "; then
    install_script="$(mktemp)"
    trap 'rm -f "${install_script}"' EXIT

    curl -fsSL "https://dot.net/v1/dotnet-install.sh" -o "${install_script}"
    bash "${install_script}" --version "${required_sdk_version}" --install-dir "${DOTNET_INSTALL_DIR}"
fi

export DOTNET_ROOT="${DOTNET_INSTALL_DIR}"
export PATH="${DOTNET_INSTALL_DIR}:${PATH}"

resolved_sdk_version="$(dotnet --version)"

for profile in /home/vscode/.bashrc /home/vscode/.profile; do
    if ! grep -Fq 'export DOTNET_ROOT="/home/vscode/.dotnet"' "${profile}"; then
        cat <<'EOF' >> "${profile}"
export DOTNET_ROOT="/home/vscode/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
EOF
    fi
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
