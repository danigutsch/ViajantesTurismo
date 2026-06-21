#!/usr/bin/env bash

set -euo pipefail

if ! command -v curl >/dev/null 2>&1; then
  printf 'curl is required but was not found on PATH.\n' >&2
  exit 1
fi

if [[ -z "${VT_API_BASE_URL:-}" ]]; then
  printf 'VT_API_BASE_URL is required, for example http://127.0.0.1:5510\n' >&2
  exit 1
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/.." && pwd)"
profile="${VT_K6_PROFILE:-smoke}"
use_docker="${VT_K6_USE_DOCKER:-auto}"
api_base_url="${VT_API_BASE_URL%/}"
docker_k6_image="${VT_K6_DOCKER_IMAGE:-grafana/k6:0.49.0}"
results_dir="${VT_K6_RESULTS_DIR:-tests/performance/results}"

if [[ "${results_dir}" = /* ]]; then
  printf 'VT_K6_RESULTS_DIR must be relative to the repository root.\n' >&2
  exit 1
fi

if [[ -z "${results_dir}" || "${results_dir}" == *".."* || "${results_dir}" == *"//"* ]]; then
  printf 'VT_K6_RESULTS_DIR must stay inside the repository root and must not contain .. segments.\n' >&2
  exit 1
fi

if [[ ! "${profile}" =~ ^[A-Za-z0-9_-]+$ ]]; then
  printf 'VT_K6_PROFILE may contain only letters, numbers, underscores, and hyphens.\n' >&2
  exit 1
fi

timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
summary_file="${results_dir}/admin-smoke-${profile}-${timestamp}.json"
mkdir -p "${repo_root}/${results_dir}"
cd "${repo_root}"

if [[ "${use_docker}" == "auto" ]]; then
  if command -v k6 >/dev/null 2>&1; then
    use_docker="0"
  elif command -v docker >/dev/null 2>&1; then
    use_docker="1"
  else
    printf 'k6 is required but was not found on PATH, and docker is unavailable.\n' >&2
    exit 1
  fi
fi

if [[ "${use_docker}" != "0" && "${use_docker}" != "1" ]]; then
  printf 'VT_K6_USE_DOCKER must be auto, 0, or 1.\n' >&2
  exit 1
fi

api_health_url="${api_base_url}/health"
for _attempt in {1..30}; do
  if curl --silent --show-error --fail "${api_health_url}" >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

if ! curl --silent --show-error --fail "${api_health_url}" >/dev/null 2>&1; then
  printf 'Admin API is not reachable at %s\n' "${api_health_url}" >&2
  exit 1
fi

if [[ "${use_docker}" == "0" ]]; then
  if ! command -v k6 >/dev/null 2>&1; then
    printf 'k6 is required when VT_K6_USE_DOCKER=0.\n' >&2
    exit 1
  fi

  k6 run \
    --summary-export "${summary_file}" \
    -e "VT_API_BASE_URL=${api_base_url}" \
    -e "VT_K6_PROFILE=${profile}" \
    "$@" \
    tests/performance/k6/scenarios/admin-smoke.js
  exit 0
fi

if ! command -v docker >/dev/null 2>&1; then
  printf 'docker is required when VT_K6_USE_DOCKER=1.\n' >&2
  exit 1
fi

docker_api_base_url="${api_base_url}"
docker_add_host_args=()
if [[ "${docker_api_base_url}" =~ ^https?://(127\.0\.0\.1|localhost)(:|/|$) ]]; then
  docker_api_base_url="${docker_api_base_url/127.0.0.1/host.docker.internal}"
  docker_api_base_url="${docker_api_base_url/localhost/host.docker.internal}"
  docker_add_host_args+=(--add-host host.docker.internal:host-gateway)
fi

docker_env_args=(
  -e "VT_API_BASE_URL=${docker_api_base_url}"
  -e "VT_K6_PROFILE=${profile}"
)

if [[ -n "${VT_K6_VUS:-}" ]]; then
  docker_env_args+=( -e "VT_K6_VUS=${VT_K6_VUS}" )
fi

if [[ -n "${VT_K6_DURATION:-}" ]]; then
  docker_env_args+=( -e "VT_K6_DURATION=${VT_K6_DURATION}" )
fi

docker run --rm \
  "${docker_add_host_args[@]}" \
  -v "${repo_root}:/work" \
  -w /work \
  "${docker_k6_image}" run \
  --summary-export "${summary_file}" \
  "${docker_env_args[@]}" \
  "$@" \
  tests/performance/k6/scenarios/admin-smoke.js
