#!/usr/bin/env bash

set -euo pipefail

status=0
eol_output="$(git ls-files --eol)"

while IFS= read -r line; do
    path="${line#*$'\t'}"

    if [[ "${line}" == *"attr/text eol=lf"* && ("${line}" == i/crlf* || "${line}" == i/mixed* || "${line}" == *" w/crlf "* || "${line}" == *" w/mixed "*) ]]; then
        echo "Expected LF line endings: ${path}" >&2
        status=1
    fi

    if [[ "${line}" == *"attr/text eol=crlf"* && ("${line}" == i/lf* || "${line}" == i/mixed* || "${line}" == *" w/lf "* || "${line}" == *" w/mixed "*) ]]; then
        echo "Expected CRLF line endings: ${path}" >&2
        status=1
    fi
done <<< "${eol_output}"

if [[ "${status}" -ne 0 ]]; then
    echo "Line-ending check failed. See docs/LINE_ENDINGS.md for the repository policy." >&2
fi

exit "${status}"
