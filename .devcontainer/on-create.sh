#!/usr/bin/env bash

set -euo pipefail

echo "🔧 Running one-time container setup..."

WORKSPACE_FOLDER="${1:-${PWD}}"
BUILD_DIR="${WORKSPACE_FOLDER}/.build"

# Ensure expected writable caches/directories exist for the non-root user.
sudo mkdir -p \
    /home/vscode/.nuget \
    /home/vscode/.nuget/packages \
    /home/vscode/.local/share/NuGet/v3-cache \
    "${BUILD_DIR}"

sudo chown -R vscode:vscode \
    /home/vscode/.nuget \
    /home/vscode/.local/share/NuGet \
    "${BUILD_DIR}"

dotnet tool restore

# Configure repository as safe for git once.
if ! git config --global --get-all safe.directory | grep -Fxq "${WORKSPACE_FOLDER}"; then
    git config --global --add safe.directory "${WORKSPACE_FOLDER}"
fi

echo "✅ One-time container setup completed."
