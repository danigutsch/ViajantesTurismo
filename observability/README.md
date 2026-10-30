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
is the telemetry privacy boundary.

## Resources

- Always present: `opentelemetry-collector` receives OTLP telemetry, removes sensitive resource
  attributes from every signal, drops all span events, sanitizes trace status/attributes, and forwards
  signals to the Aspire dashboard.
- Optional: `grafana` provides the provisioned local UI.
- Optional: `loki` stores logs.
- Optional: `tempo` stores sanitized traces.
- Optional: `prometheus` scrapes the Collector metric exporter.

Raw telemetry exists between the application and this trusted Collector. The AppHost forwarding
contract covers resources carrying Aspire's standard OTLP annotation; a manually constructed direct
exporter can bypass it. Keep backend endpoints inaccessible to application workloads except through
the gateway.

The privacy processors use `error_mode: propagate` so an expression failure rejects the affected OTLP
payload instead of forwarding unsanitized telemetry. This deliberately trades telemetry availability
for privacy. Monitor Collector processor and receiver errors so rejected telemetry is visible to
operators.

## Dashboards and alerts

Every dashboard under `grafana/dashboards/` opens with a text panel stating its purpose, audience, and
the decision it supports, and every panel carries a description naming the metric, unit, and
operational question. Signal inventory and keep-or-remove verdicts live in
`docs/OBSERVABILITY_CATALOG.md`.

Alert rules are provisioned from `grafana/provisioning/alerting/`. Alerts are optional and exist only for
actionable conditions; each has a runbook in `docs/OBSERVABILITY_RUNBOOKS.md`, which also lists the
alert templates. The contact point is a local-only sink.

Panels carry a `verify` key: `data` panels must return series once smoke traffic ran, `idle-ok` panels may
be empty on a healthy system.

## Verifying dashboards

Static contract (CI, `ObservabilityAssetsTests` in the architecture tests):

```bash
dotnet test --project tests/ViajantesTurismo.ArchitectureTests -- --filter-class "*ObservabilityAssetsTests"
```

Live check against a running stack:

```bash
ASPIRE_ENABLE_OBSERVABILITY_STACK=1 dotnet tool run aspire run   # separate terminal
dotnet run observability/tests/SmokeTraffic.cs                   # give panels data
dotnet run observability/tests/VerifyQueries.cs -- http://127.0.0.1:<grafana-port>
dotnet run observability/tests/VerifyDashboards.cs -- http://127.0.0.1:<grafana-port> <screenshot-dir>
```

`VerifyQueries.cs` runs every panel query through Grafana's datasource proxy. `VerifyDashboards.cs` renders
each dashboard in headless Chromium with Playwright, fails on "No data" for `data` panels or a panel
error, and writes screenshots for review. Install browsers once with the Playwright tool if needed.

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
  tests/
  otel-collector/
    privacy.yaml
    aspire.yaml
    config.yaml
  prometheus/
  tempo/
```

Keep this folder backend configuration focused. Component-specific telemetry contracts and samples
belong with the component that owns the signal.
