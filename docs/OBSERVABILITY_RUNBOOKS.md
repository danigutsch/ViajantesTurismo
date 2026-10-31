# Observability Runbooks and Alert Templates

Alerts are optional. An alert exists only when a condition is actionable: someone would do something
specific when it fires. Rules are provisioned as code from
`observability/grafana/provisioning/alerting/` and evaluated by Grafana, because the local Grafana
container mounts the whole `provisioning` folder while Prometheus mounts only `prometheus.yml`. The
contact point is a local-only sink, so no alert notifies anyone today; production routing and portable
Prometheus rule files are tracked as a follow-up under the observability epic.

## Alert templates

Every rule in `rules.yaml` follows one of these archetypes: query `A` (Prometheus, instant), reduce `B`
(last), threshold `C`. Copy the nearest rule and change the expression, threshold, `for`, and runbook
anchor.

| Archetype | Expression shape | Example rule |
| --- | --- | --- |
| Any occurrence | `sum(increase(<counter>[5m]))` greater than 0, plus `sum(<counter> unless <counter> offset 5m)` for counters created at their first event | `unhandled-exceptions`, `collector-refusing-telemetry` |
| Error ratio | `sum(rate(<counter>{outcome="error"}[5m])) / clamp_min(sum(rate(<counter>[5m])), <floor>)` greater than a ratio, with `for: 10m` | `http-5xx-ratio`, `catalog-event-errors` |
| Latency percentile | `histogram_quantile(0.95, sum by (le) (rate(<histogram>_bucket[5m])))` greater than seconds | none yet |
| Stalled progress | input rate above 0 while output rate equals 0 | none yet |
| Backlog or queue age | gauge or age percentile above threshold | none yet |

Rules must have `summary`, `description`, and `runbook` annotations, group only by bounded labels, and
set `noDataState: OK` unless absence itself is the fault (`collector-self-telemetry-down` uses
`Alerting`). The `viajantes.message` notification template renders the three annotations once per alert,
and the webhook contact point sets it as `message`.

`increase()` returns 0 for a counter series created at its first event, because the window holds one
sample before the second event. Rules on counters that appear lazily therefore add
`sum(<counter> unless <counter> offset <window>)`, which counts series that did not exist one window ago.
The error-ratio rules do not, so the first error of a new series is invisible to them until a second one
arrives.

Error-ratio rules clamp the denominator with a floor (`http-5xx-ratio` 0.1 requests per second,
`catalog-event-errors` 0.01 events per second). Below the floor the rule behaves like an absolute error
rate, not a percentage.

## Collector refusing telemetry

**What it means.** A receiver refused spans, metric points, or log records. Privacy processors use
`error_mode: propagate`, so a processor expression failure rejects the payload instead of forwarding it
unsanitized. Telemetry is lost; nothing leaked.

**First checks.** Open the `collector-gateway` dashboard, then the Collector container logs for the
processor error. Check whether a recent instrumentation change introduced an attribute the transform
statements cannot handle.

**Rollback.** Revert the instrumentation or the `observability/otel-collector/privacy.yaml` change.

## Collector export failing

**What it means.** The gateway could not send telemetry to a backend (Aspire dashboard, Tempo, Loki, or
the Prometheus exporter).

**First checks.** The `collector-gateway` dashboard shows the failing exporter. Check that the backend
container is running and healthy in the Aspire dashboard.

**Rollback.** Restart the failing backend resource. When a sending queue is full the Collector rejects new data by default
(`block_on_overflow` is false), so telemetry stays lost until the backend recovers.

## Collector self telemetry down

**What it means.** Prometheus cannot scrape the Collector internal metrics on port 8888 (`up` is 0 for the
`otel-collector-internal` job, or the series is missing). The refusal and export alerts cannot fire while
this holds, so a silent Collector looks the same as a healthy one.

**First checks.** The Prometheus targets page, the `collector-gateway` dashboard, and whether the
Collector container is running and still exposes `service.telemetry.metrics` on 0.0.0.0:8888.

**Rollback.** Restore `observability/otel-collector/config.yaml` and `observability/prometheus/prometheus.yml`
to the last known good version and restart the Collector.

## HTTP 5xx ratio

**What it means.** More than 5 percent of responses were 5xx for 10 minutes.

**First checks.** `http-platform` shows which service. Then `traces` (error traces), `logs`
(errors), and `runtime` (memory, thread pool, database pool).

**Rollback.** Roll back the latest deployment of the affected service.

## Unhandled exceptions

**What it means.** A request failed with an exception no code handled
(`unhandled_exceptions_total`, log EventId 4001).

**First checks.** The `error_type` label on the `http-platform` panel, then the matching log record and
its trace.

**Rollback.** Roll back the change that introduced the exception type.

## Catalog event errors

**What it means.** More than 10 percent of Catalog integration events ended in error for 10 minutes.
The public catalog may fall behind Admin.

**First checks.** `catalog-pipeline`: which event type and stage (intake, stream, projection) fails.
Then `event-store` and `runtime` for database trouble.

**Rollback.** Fix the cause, then let the integration event worker retry; events are idempotent.

## Branding invalidation failures

**What it means.** A Branding cache invalidation reported `failed`. Public pages may serve stale
branding until the bounded freshness window expires.

**First checks.** `branding-cache` for the source revision per instance and the invalidation outcomes.

**Rollback.** Restart the affected instance to reload Branding settings.
