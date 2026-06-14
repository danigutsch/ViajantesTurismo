#!/usr/bin/env bash

set -euo pipefail

repo_root="$(git rev-parse --show-toplevel)"

git config core.hooksPath "$repo_root/.githooks"

chmod +x "$repo_root/.githooks/commit-msg"
chmod +x "$repo_root/.githooks/pre-commit"

printf '%s\n' "Configured core.hooksPath to $repo_root/.githooks"
printf '%s\n' "Optional hooks enabled: commit-msg, pre-commit"
printf '%s\n' "Install gitleaks locally if you want pre-commit secret scanning to run."
