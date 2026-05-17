#!/usr/bin/env bash

set -euo pipefail

commit_message_file="${1:-}"

if [[ -z "${commit_message_file}" ]]; then
    echo "validate-commit-message requires the commit message file path" >&2
    exit 1
fi

header="$(grep -m 1 -vE '^[[:space:]]*(#.*)?$' "${commit_message_file}" || true)"
allowed_types='build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test'
pattern="^(${allowed_types})(\([a-z0-9._/-]+\))?!?: .+"

if [[ -z "${header}" ]]; then
    echo "Commit message must contain a non-empty header." >&2
    exit 1
fi

if [[ "${header}" == Merge\ * || "${header}" == Revert\ * ]]; then
    exit 0
fi

if [[ ${#header} -gt 100 ]]; then
    echo "Commit header exceeds 100 characters." >&2
    exit 1
fi

if [[ ! "${header}" =~ ${pattern} ]]; then
    echo "Commit message must follow Conventional Commits: <type>[optional scope]: <description>" >&2
    exit 1
fi
