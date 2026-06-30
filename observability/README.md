# Local Observability Stack

This folder contains source-controlled local observability backend configuration for the optional
Aspire AppHost stack.

## Enable

```bash
ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run
```

The stack is disabled by default. Regular local runs can use the Aspire dashboard without starting
Grafana, Loki, Tempo, Prometheus, or the OpenTelemetry Collector.

## Resources

- `opentelemetry-collector`: receives OTLP telemetry and routes logs, traces, and metrics.
- `grafana`: local UI with provisioned datasources and dashboards.
- `loki`: local log backend.
- `tempo`: local trace backend.
- `prometheus`: local metric backend that scrapes the collector Prometheus exporter.

## Layout

```text
observability/
  grafana/
    dashboards/
    provisioning/
  loki/
  otel-collector/
  prometheus/
  tempo/
```

Keep this folder backend configuration focused. Component-specific telemetry contracts and samples
belong with the component that owns the signal.
