#!/usr/bin/env bash

set -euo pipefail

docker_uid="$(id -u)"
docker_gid="$(id -g)"
docker_user="${docker_uid}:${docker_gid}"

if command -v python3 > /dev/null 2>&1; then
    python3 scripts/lint-links.py "$@"
    exit 0
fi

if command -v docker > /dev/null 2>&1; then
    docker run --rm \
        --user "${docker_user}" \
        --env HOME=/tmp \
        --env PYTHONDONTWRITEBYTECODE=1 \
        --volume "${PWD}:/workspace" \
        --workdir /workspace \
        python:3.13-alpine \
        python3 -B scripts/lint-links.py "$@"
    exit 0
fi

echo "lint-links requires either local python3 or docker." >&2
exit 1
