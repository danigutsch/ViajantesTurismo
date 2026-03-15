#!/bin/bash
# Install git hooks for Unix/Linux/macOS
# This script copies the pre-commit hook to the .git/hooks directory

set -euo pipefail

hook_source="scripts/pre-commit"

if ! hook_dir=$(git rev-parse --git-path hooks 2>/dev/null); then
    echo "Error: Not a git repository"
    exit 1
fi

hook_dest="${hook_dir}/pre-commit"

if [[ ! -f "${hook_source}" ]]; then
    echo "Error: Hook file not found at ${hook_source}"
    exit 1
fi

# Create hooks directory if it doesn't exist
mkdir -p "${hook_dir}"

# Copy the hook and make it executable
cp "${hook_source}" "${hook_dest}"
chmod +x "${hook_dest}"

echo "✓ Pre-commit hook installed successfully"
echo "  The hook will lint markdown, shell scripts, PowerShell, and .NET code before each commit"
echo "  Use 'git commit --no-verify' to bypass the hook if needed"
