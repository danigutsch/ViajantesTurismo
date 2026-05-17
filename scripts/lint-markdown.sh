#!/usr/bin/env bash

set -euo pipefail

args=("$@")
targets=()

for arg in "${args[@]}"; do
    case "${arg}" in
        -* )
            ;;
        * )
            targets+=("${arg}")
            ;;
    esac
done

if [[ ${#targets[@]} -eq 0 ]]; then
    targets=("**/*.md")
fi

npm exec --yes --package markdownlint-cli@0.48.0 -- \
    markdownlint "${targets[@]}" \
    --ignore node_modules \
    --ignore "**/bin/**" \
    --ignore "**/obj/**" \
    --ignore "**/TestResults/**" \
    --config .markdownlint.json "${args[@]}"
