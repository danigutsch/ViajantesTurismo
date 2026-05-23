#!/usr/bin/env bash

set -euo pipefail

docker_uid="$(id -u)"
docker_gid="$(id -g)"
docker_user="${docker_uid}:${docker_gid}"

if ! command -v docker >/dev/null 2>&1; then
    echo "docker is required for markdown lint" >&2
    exit 1
fi

args=("$@")
fix_mode=false
globs=()

for arg in "${args[@]}"; do
    case "${arg}" in
        --fix)
            fix_mode=true
            ;;
        *)
            globs+=("${arg}")
            ;;
    esac
done

if [[ ${#globs[@]} -eq 0 ]]; then
    globs=("**/*.md")
fi

markdownlint_args=(
    --config /workspace/.markdownlint-cli2.jsonc
)

for pattern in "${globs[@]}"; do
    markdownlint_args+=(--globs "${pattern}")
done

if [[ "${fix_mode}" == true ]]; then
    markdownlint_args+=(--fix)
fi

docker run --rm \
    --user "${docker_user}" \
    --env HOME=/tmp \
    --volume "${PWD}:/workspace" \
    --workdir /workspace \
    davidanson/markdownlint-cli2:v0.18.1 \
    "${markdownlint_args[@]}"
