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
| Error ratio | Error count divided by all events over five minutes, with a one-event denominator floor and a sustained threshold | `http-5xx-ratio`, `catalog-event-errors` |
| Latency percentile | `histogram_quantile(0.95, sum by (le) (rate(<histogram>_bucket[5m])))` greater than seconds | `customer-import-latency` |
| Stalled progress | input rate above 0 while output rate equals 0 | none yet |
| Backlog or queue age | gauge or age percentile above threshold | none yet |

Rules must have `summary`, `description`, and `runbook` annotations, group only by bounded labels, and
set `noDataState: OK` unless absence itself is the fault (`collector-self-telemetry-down` uses
`Alerting`). The `viajantes.message` notification template renders the three annotations once per alert,
and the webhook contact point sets it as `message`.

The Collector export and error ratio rules count each series once. A series with a baseline uses
its reset-aware `increase()`. A series that first appeared within the lookback contributes its
current value using `<counter> unless <counter> offset <window>`. The ratio uses at least one event
as its denominator, which makes low-volume errors visible to the existing ten-minute hold.

## Customer import failures

**What it means.** The Admin API reported a scanner or workflow failure after a customer import
or commit began. File validation, row rejection, unresolved conflicts, and cancellation have
separate outcomes.

**First checks.** Open the Operation Health dashboard for the affected route stage. Check scanner
availability when the API returns a scanner-unavailable problem. Check Admin database and workflow
errors around the same time when the API reports an internal error.

**Recovery.** Restore the failed dependency or roll back an Admin deployment if the errors began
after it. The alert clears when the ten-minute window contains no new failures.

## Customer import latency

**What it means.** Completed imports exceeded a 30-second p95 for five minutes, with at least
three completed imports in the 15-minute window. This signals that work after form binding is
consuming a significant share of the two-minute request deadline.

**First checks.** Compare the route stage and size buckets on the Operation Health dashboard.
Check scanner response time and Admin database activity during the affected window.

**Recovery.** Restore the degraded dependency or roll back a deployment that raised processing
time. The alert clears when enough completed requests are fast again or the sample count recedes.

## Catalog media upload failures

**What it means.** Catalog intake could not obtain a scanner decision or write the original
object. Expected validation and security rejections and gallery conflicts have their own
outcomes.

**First checks.** Open Operation Health for the `upload_outcome` and scan or store latency.
Check scanner health and connectivity for `scanner_unavailable`. Check Catalog connectivity to
its media object store and the configured original-write permissions for `storage_failed`.

**Recovery.** Restore the affected dependency and confirm a synthetic upload can complete. The
alert clears after ten minutes without that failure.

## Catalog media upload latency

**What it means.** Media scanning or the original object write has a p95 above ten seconds for
five minutes, with at least three observations of the affected stage in 15 minutes. The initial
threshold identifies dependency stalls early in the two-minute multipart request budget.

**First checks.** Use the `upload_stage` on Operation Health to identify the dependency. Compare its
latency and failure history with the dependency's health and queue or connection metrics.

**Recovery.** Restore the slow dependency and confirm latency returns to normal with new uploads.
The alert can also clear as the number of observations falls below the minimum.

## Management session ticket failures

**Availability.** Live data requires ViajantesTurismo to adopt the separately approved SharedKernel
BFF producer. Until adoption, the provisioned panel permits an empty result and the alert treats
the absent series as OK.

**What it means.** The Management Web ticket store could not protect or read a stored cookie
ticket, or its backing cache operation failed. A cache miss and normal expiry do not raise this
signal.

**First checks.** Open Operation Health and check the operation. For `protect` and `unprotect`,
verify access to the data protection key ring, certificate, and shared application identity on
each replica. For `cache_get`, `cache_set`, and `cache_remove`, check cache availability and
the Management Web database connection.

**Recovery.** Restore the key ring, certificate access, or cache connection. Verify a synthetic
sign-in and an authenticated request. The alert clears after ten minutes without that failure.

## Collector refusing telemetry

**What it means.** A receiver refused spans, metric points, or log records. Privacy processors use
`error_mode: propagate`, so a processor expression failure rejects the payload instead of forwarding it
unsanitized. Telemetry is lost; nothing leaked.

**First checks.** Open the `collector-gateway` dashboard, then the Collector container logs for the
processor error. Check whether a recent instrumentation change introduced an attribute the transform
statements cannot handle.

**Rollback.** Revert the instrumentation or the `observability/otel-collector/privacy.yaml` change.

## Collector export failing

**What it means.** The gateway could not send telemetry to a backend or rejected new telemetry before
sending it because an exporter queue was full.

**First checks.** Inspect the send failure and queue utilization panels in `collector-gateway`.
The `otelcol_exporter_enqueue_failed_spans_total`, `otelcol_exporter_enqueue_failed_metric_points_total`,
and `otelcol_exporter_enqueue_failed_log_records_total` counters identify rejections by exporter.
Check that the affected backend is healthy in the Aspire dashboard.

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

**What it means.** The five-minute 5xx ratio stayed above 5 percent for 10 consecutive minutes.
Each series with a five-minute baseline contributes its increase. A series without that baseline contributes
its current counter value, so its first failure is visible at low traffic. The denominator is at least one response.

**First checks.** `http-platform` shows which service. Then `traces` (error traces), `logs`
(errors), and `runtime` (memory, thread pool, database pool).

**Rollback.** Roll back the latest deployment of the affected service.

## Unhandled exceptions

**What it means.** A request failed with an exception no code handled
(`unhandled_exceptions_total`, log EventId 4001).

**First checks.** The `error_type` label on the `http-platform` panel, then the matching log record and
its trace.

**Rollback.** Roll back the change that introduced the exception type.

## Migration initialization failed

**What it means.** The one-shot MigrationService reported a database initialization error. The
worker records `success`, `error`, or `cancelled` after migrations and any development data initialization.
The alert selects `error` from the histogram count. The five-minute lookback lets Prometheus detect
the first exported measurement even when the process exits before a second measurement.

**First checks.** Open Recent database initializations and Recent database initialization duration
in `platform-smoke`. Inspect the MigrationService exit status, `DatabaseInitialization` trace,
and failure event. Use the database readiness and release schema history to identify the step.
For a process that never reached initialization, check its startup logs and the configured
database connection sources in the deployment environment.

**Recovery.** Correct the dependency, configuration, or migration failure. Check the applied schema
state and follow the deployment's migration recovery procedure before retrying startup.

## Catalog event errors

**What it means.** The five-minute Catalog integration event error ratio stayed above 10 percent for
10 consecutive minutes. It uses the same first-sample handling as the HTTP ratio and a denominator of at
least one event. The public catalog may fall behind Admin.

**First checks.** `catalog-pipeline`: which event type and stage (intake, stream, projection) fails.
Then `event-store` and `runtime` for database trouble.

**Rollback.** Fix the cause, then let the integration event worker retry; events are idempotent.

## Branding invalidation failures

**What it means.** A Branding cache invalidation reported `failed`. Public pages may serve stale
branding until the bounded freshness window expires.

**First checks.** `branding-cache` for the source revision per instance and the invalidation outcomes.

**Rollback.** Restart the affected instance to reload Branding settings.

## Document operation failures

**What it means.** A document request returned a server error or threw an exception, or a retention
batch failed. The alert fires after one minute when a failure lands in the preceding ten minutes,
including the first sample of a new failure series. It clears after the failure leaves that window.
Validation or state rejections and cooperative cancellations have their own outcomes.

**First checks.** Open `document-operations` to identify the operation and whether the failure came
from a request or a retention batch. Filter logs for `DocumentOperationCompleted` with `Outcome`
equal to `failed`. Use the trace attached to that event, or search for the `admin.document.operation`
span with the matching `document.correlation`. That correlation also appears in ordinary document
API audit evidence and audit retention evidence. Previews count the authorized revision read. A
download counts when the API opens the artifact for delivery. Use HTTP telemetry to diagnose the
subsequent client transfer. The panels show cumulative attempts and can be empty in idle deployments.
Request counts cover operations that reach the authorized endpoint handler.

**Retention response.** Corroborate the failure with the polling cycle and its bounded failure type.
Check database and hold-store availability, then confirm a scheduled batch completes. Retention
handles draft, audit, and idempotency records independently. Keep legal holds in effect while
investigating. A successful batch can retain held items. Start with the
[retention policy](operations/document-retention-and-legal-hold.md) for evidence and schedule details.
