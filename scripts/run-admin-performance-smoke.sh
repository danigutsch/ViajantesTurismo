#!/usr/bin/env bash

set -euo pipefail

if ! command -v k6 >/dev/null 2>&1; then
  printf 'k6 is required but was not found on PATH.\n' >&2
  exit 1
fi

if [[ -z "${VT_API_BASE_URL:-}" ]]; then
  printf 'VT_API_BASE_URL is required, for example http://127.0.0.1:5001\n' >&2
  exit 1
fi

k6 run \
  -e "VT_API_BASE_URL=${VT_API_BASE_URL}" \
  -e "VT_K6_PROFILE=${VT_K6_PROFILE:-smoke}" \
  "$@" \
  tests/performance/k6/scenarios/admin-smoke.js
