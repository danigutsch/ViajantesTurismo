#!/bin/bash
# Install git hooks for Unix/Linux/macOS
# This script copies the repository git hooks to the .git/hooks directory

set -euo pipefail

hook_sources=("scripts/pre-commit" "scripts/commit-msg")
hook_names=("pre-commit" "commit-msg")

if ! hook_dir=$(git rev-parse --git-path hooks 2>/dev/null); then
    echo "Error: Not a git repository"
    exit 1
fi

# Create hooks directory if it doesn't exist
mkdir -p "${hook_dir}"

for i in "${!hook_sources[@]}"; do
    hook_source="${hook_sources[${i}]}"
    hook_dest="${hook_dir}/${hook_names[${i}]}"

    if [[ ! -f "${hook_source}" ]]; then
        echo "Error: Hook file not found at ${hook_source}"
        exit 1
    fi

    cp "${hook_source}" "${hook_dest}"
    chmod +x "${hook_dest}"
done

echo "✓ Git hooks installed successfully"
echo "  pre-commit lints and formats staged files before each commit"
echo "  commit-msg validates Conventional Commits with commitlint"
echo "  Use 'git commit --no-verify' to bypass hooks if needed"
