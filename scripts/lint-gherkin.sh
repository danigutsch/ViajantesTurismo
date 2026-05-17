#!/usr/bin/env bash

set -euo pipefail

filtered_args=()
tmp_file="$(mktemp)"
trap 'rm -f "${tmp_file}"' EXIT

for arg in "$@"; do
    if [[ "${arg}" == tests/**/*.feature ]]; then
        find tests -name '*.feature' -not -path '*/bin/*' -not -path '*/obj/*' | sort > "${tmp_file}"

        while IFS= read -r file; do
            filtered_args+=("${file}")
        done < "${tmp_file}"
        continue
    fi

    filtered_args+=("${arg}")
done

npm exec --yes --package gherkin-lint@4.2.4 -- \
    gherkin-lint "${filtered_args[@]}"
