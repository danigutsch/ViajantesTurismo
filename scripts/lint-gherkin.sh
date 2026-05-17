#!/usr/bin/env bash

set -euo pipefail

filtered_args=()
tmp_file="$(mktemp)"
trap 'rm -f "${tmp_file}"' EXIT

append_feature_file_list() {
    find tests -name '*.feature' -not -path '*/bin/*' -not -path '*/obj/*' | sort > "${tmp_file}"

    while IFS= read -r file; do
        filtered_args+=("${file}")
    done < "${tmp_file}"
}

for arg in "$@"; do
    if [[ "${arg}" == tests/**/*.feature || -z "${arg}" ]]; then
        append_feature_file_list
        continue
    fi

    if [[ "${arg}" == */bin/* || "${arg}" == */obj/* ]]; then
        continue
    fi

    filtered_args+=("${arg}")
done

if [[ ${#filtered_args[@]} -eq 0 ]]; then
    append_feature_file_list
fi

npm exec --yes --package gherkin-lint@4.2.4 -- \
    gherkin-lint "${filtered_args[@]}"
