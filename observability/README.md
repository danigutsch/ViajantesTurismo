# Local Observability Stack

This folder contains source-controlled configuration for the always-on local OpenTelemetry Collector
gateway and the optional Aspire AppHost observability backends.

## Trusted gateway and optional backends

The pinned OpenTelemetry Collector gateway starts for every supported AppHost profile. Normal
Aspire-annotated application OTLP traffic is routed through it and sanitized before reaching the
built-in Aspire dashboard.

Enable the additional local backends with:

```bash
ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run
```

Grafana, Loki, Tempo, and Prometheus are disabled by default. The Collector remains present because it
is the trace privacy boundary.

## Resources

- Always present: `opentelemetry-collector` receives OTLP telemetry, drops all span events, sanitizes
  trace status/attributes, and forwards signals to the Aspire dashboard.
- Optional: `grafana` provides the provisioned local UI.
- Optional: `loki` stores logs.
- Optional: `tempo` stores sanitized traces.
- Optional: `prometheus` scrapes the Collector metric exporter.

Raw telemetry exists between the application and this trusted Collector. The AppHost forwarding
contract covers resources carrying Aspire's standard OTLP annotation; a manually constructed direct
exporter can bypass it. Keep backend endpoints inaccessible to application workloads except through
the gateway.

The trace processors use `error_mode: propagate` so an expression failure rejects the affected OTLP
payload instead of forwarding an unsanitized trace. This deliberately trades telemetry availability
for privacy. Monitor Collector processor and receiver errors so rejected telemetry is visible to
operators.

## Local security boundary

This source-controlled stack is for local development only. The checked-in Collector YAML does not
require TLS or client authentication; Aspire may inject development-certificate TLS when an HTTPS
launch profile is active. The Tempo exporter uses `tls.insecure: true`, Loki uses HTTP, and local
Grafana permits anonymous Administrator access.

Do not deploy this wiring unchanged. Deployments must configure authenticated, encrypted Collector
ingress and downstream transport, keep backend endpoints inaccessible to application workloads, use
non-anonymous backend access, and supply credentials through deployment-managed secrets.

## Layout

```text
observability/
  grafana/
    dashboards/
    provisioning/
  loki/
  otel-collector/
    privacy.yaml
    aspire.yaml
    config.yaml
  prometheus/
  tempo/
```

Keep this folder backend configuration focused. Component-specific telemetry contracts and samples
belong with the component that owns the signal.
