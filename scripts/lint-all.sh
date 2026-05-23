#!/usr/bin/env bash

set -euo pipefail

shopt -s globstar nullglob

fix_mode=false
skip_markdown=false
for arg in "$@"; do
    case "${arg}" in
        --fix)
            fix_mode=true
            ;;
        --skip-markdown)
            skip_markdown=true
            ;;
        *)
            echo "Unknown argument: ${arg}" >&2
            echo "Usage: bash scripts/lint-all.sh [--fix] [--skip-markdown]" >&2
            exit 1
            ;;
    esac
done

shellcheck_targets=(
    ./.devcontainer/*.sh
    ./scripts/*.sh
    ./setup-dev.sh
)

run_shellcheck() {
    if command -v shellcheck >/dev/null 2>&1; then
        shellcheck \
            --exclude=SC1091 \
            --source-path=SCRIPTDIR \
            -- "$@"
        return
    fi

    if command -v docker >/dev/null 2>&1; then
        docker run --rm \
            --volume "${PWD}:/workspace" \
            --workdir /workspace \
            koalaman/shellcheck:stable \
            --exclude=SC1091 \
            --source-path=SCRIPTDIR \
            "$@"
        return
    fi

    echo "shellcheck requires either local shellcheck or docker." >&2
    exit 1
}

if [[ "${fix_mode}" == true ]]; then
    if command -v shfmt >/dev/null 2>&1; then
        shfmt -w -i 4 -ci -bn -sr -- "${shellcheck_targets[@]}"
    elif command -v docker >/dev/null 2>&1; then
        docker run --rm \
            --volume "${PWD}:/workspace" \
            --workdir /workspace \
            mvdan/shfmt:v3.11.0 \
            -w -i 4 -ci -bn -sr "${shellcheck_targets[@]}"
    else
        echo "Optional: shfmt requires local install or docker. Skipping shell autofix." >&2
    fi
fi

run_shellcheck "${shellcheck_targets[@]}"
bash scripts/lint-json.sh
bash scripts/lint-gherkin.sh tests/**/*.feature

if [[ "${skip_markdown}" != true ]]; then
    if ! command -v docker >/dev/null 2>&1; then
        echo "docker is required for markdown lint. Install docker or run with --skip-markdown." >&2
        exit 1
    fi

    markdownlint_args=(
        --config /workspace/.markdownlint-cli2.jsonc
        --globs "**/*.md"
    )

    if [[ "${fix_mode}" == true ]]; then
        markdownlint_args+=(--fix)
    fi

    docker run --rm \
        --volume "${PWD}:/workspace" \
        --workdir /workspace \
        davidanson/markdownlint-cli2:v0.18.1 \
        "${markdownlint_args[@]}"

    if command -v mdspell >/dev/null 2>&1; then
        mdspell --en-us --report "docs/**/*.md" || true
    else
        echo "Optional: Install markdown-spellcheck (mdspell) to enable spell checking. Skipping..." >&2
    fi
fi
