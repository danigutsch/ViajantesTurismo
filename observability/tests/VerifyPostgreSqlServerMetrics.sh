#!/usr/bin/env bash
set -euo pipefail

if [[ $# != 4 || ! $2 =~ ^[0-9]{5}$ || ! $3 =~ ^[0-9]{5}$ || ! $4 =~ ^(healthy|disabled|denied|unavailable)$ ]]; then
  printf 'Usage: %s <collector-bin> <postgres-port> <metrics-port-base> <healthy|disabled|denied|unavailable>\n' "$0" >&2
  exit 2
fi
collector_bin=$1
db_port=$((10#$2))
metrics_port=$((10#$3))
scenario=$4
internal_port=$((metrics_port + 1))
otlp_port=$((metrics_port + 2))
if [[ ! -x ${collector_bin} || ${db_port} -gt 65535 || ${otlp_port} -gt 65535 ]]; then
  printf 'An executable Collector and valid loopback ports are required.\n' >&2
  exit 2
fi
probe_dir=$(mktemp -d -p /tmp vt1219-collector.XXXXXXXX)
printf 'Scenario %s at PostgreSQL port %s\n' "${scenario}" "${db_port}"
cat > "${probe_dir}/config.yaml" <<YAML
receivers:
  otlp:
    protocols:
      http:
        endpoint: 127.0.0.1:${otlp_port}
processors:
  memory_limiter:
    check_interval: 1s
    limit_mib: 128
  batch:
    timeout: 50ms
exporters:
  prometheus:
    endpoint: 127.0.0.1:${metrics_port}
service:
  telemetry:
    metrics:
      level: normal
      readers:
        - pull:
            exporter:
              prometheus:
                host: 127.0.0.1
                port: ${internal_port}
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [prometheus]
YAML

collector_args=(--config "${probe_dir}/config.yaml")
if [[ ${scenario} != disabled ]]; then
  : "${VT_POSTGRES_SERVER_METRICS_USER:?Set the username for the synthetic PostgreSQL fixture}"
  : "${VT_POSTGRES_SERVER_METRICS_PASSWORD:?Set the password for the synthetic PostgreSQL fixture}"
  collector_args+=(--config observability/otel-collector/postgresql-server.yaml
    --set "receivers.sqlquery/postgresql_server.port=${db_port}"
    --set "receivers.sqlquery/postgresql_server.collection_interval=1s")
fi

failure_pattern='connection refused'
if [[ ${scenario} == denied ]]; then
  failure_pattern='permission denied for database'
fi
env -i PATH="${PATH}" VT_POSTGRES_SERVER_METRICS_HOST=127.0.0.1 \
  VT_POSTGRES_SERVER_METRICS_USER="${VT_POSTGRES_SERVER_METRICS_USER:-}" \
  VT_POSTGRES_SERVER_METRICS_PASSWORD="${VT_POSTGRES_SERVER_METRICS_PASSWORD:-}" \
  "${collector_bin}" "${collector_args[@]}" > "${probe_dir}/collector.log" 2>&1 &
collector_pid=$!
cleanup() {
  kill "${collector_pid}" 2>/dev/null || true
  wait "${collector_pid}" 2>/dev/null || true
}
trap cleanup EXIT
trap 'exit 130' INT
trap 'exit 143' TERM

sampled=$(date +%s%N)
payload=$(jq -cn --arg sampled "${sampled}" '{
  resourceMetrics:[{scopeMetrics:[{scope:{name:"vt-proof"},metrics:[
    {name:"vt_proof_telemetry",gauge:{dataPoints:[{timeUnixNano:$sampled,asInt:"1"}]}}
  ]}]}]
}')
for ((attempt=0; attempt<40; attempt++)); do
  if curl -fsS --max-time 1 "http://127.0.0.1:${otlp_port}/v1/metrics" \
    -H 'Content-Type: application/json' --data "${payload}" > "${probe_dir}/otlp-reply.json" 2>/dev/null; then
    break
  fi
  kill -0 "${collector_pid}" 2>/dev/null || {
    printf 'Collector exited during startup. See %s/collector.log\n' "${probe_dir}" >&2
    exit 1
  }
  sleep .25
done

for ((attempt=0; attempt<40; attempt++)); do
  curl -fsS --max-time 1 "http://127.0.0.1:${metrics_port}/metrics" > "${probe_dir}/data.prom"
  curl -fsS --max-time 1 "http://127.0.0.1:${internal_port}/metrics" > "${probe_dir}/collector.prom"
  if ! rg -q '^vt_proof_telemetry[{ ]' "${probe_dir}/data.prom"; then
    sleep .25
    continue
  fi
  case "${scenario}" in
    healthy)
      if rg -q '^postgresql_server_connections_limit[{ ]' "${probe_dir}/data.prom"; then
        break
      fi
      ;;
    disabled)
      break
      ;;
    *)
      if rg -q "Error scraping metrics.*sqlquery/postgresql_server.*${failure_pattern}" "${probe_dir}/collector.log"; then
        break
      fi
      ;;
  esac
  sleep .25
done

rg -q '^vt_proof_telemetry[{ ]' "${probe_dir}/data.prom"
printf 'Existing OTLP metric exported.\n'
if rg -qi "vt_fixture|:${db_port}|database[_.]name|db[_.]namespace|server[_.]address|statement|username|client[_.]address" "${probe_dir}/data.prom"; then
  printf 'Unexpected data in the exported telemetry.\n' >&2
  exit 1
fi

case "${scenario}" in
  healthy)
    rg '^postgresql_server_connections_(used|limit)[{ ]' "${probe_dir}/data.prom"
    awk '/^postgresql_server_connections_used[{ ]/ { used = $NF; used_count++ }
         /^postgresql_server_connections_limit[{ ]/ { limit = $NF; limit_count++ }
         END { exit !(used_count == 1 && limit_count == 1 && used >= 1 && used <= limit) }' "${probe_dir}/data.prom"
    ;;
  disabled)
    if rg -q '^postgresql_server_connection' "${probe_dir}/data.prom"; then
      exit 1
    fi
    printf 'No PostgreSQL metrics while disabled.\n'
    ;;
  *)
    if rg -q '^postgresql_server_connection' "${probe_dir}/data.prom"; then
      exit 1
    fi
    rg -q "Error scraping metrics.*sqlquery/postgresql_server.*${failure_pattern}" "${probe_dir}/collector.log"
    printf 'Collector reported %s.\n' "${failure_pattern}"
    awk '/^otelcol_scraper_scraped_metric_points_total\{.*postgresql_server/ && $NF == 0 { print; ok = 1 }
         END { exit !ok }' "${probe_dir}/collector.prom"
    ;;
esac
printf 'Fixture evidence directory %s\n' "${probe_dir}"
