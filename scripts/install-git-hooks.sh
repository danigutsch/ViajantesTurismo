#!/usr/bin/env bash

set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"
target_hooks_path=".githooks"
existing_hooks_path="$(git config --get core.hooksPath || true)"

if [[ -n "${existing_hooks_path}" && "${existing_hooks_path}" != "${target_hooks_path}" ]]; then
    printf '%s\n' "Refusing to overwrite existing core.hooksPath: ${existing_hooks_path}" >&2
    printf '%s\n' "Set it to ${target_hooks_path} manually if you want to switch to the repository-owned hooks." >&2
    exit 1
fi

git config core.hooksPath "${target_hooks_path}"

chmod +x "${repo_root}/.githooks/commit-msg"
chmod +x "${repo_root}/.githooks/pre-commit"

printf '%s\n' "Configured core.hooksPath to ${target_hooks_path}"
printf '%s\n' "Optional hooks enabled: commit-msg, pre-commit"
printf '%s\n' "Install gitleaks locally if you want pre-commit secret scanning to run."
