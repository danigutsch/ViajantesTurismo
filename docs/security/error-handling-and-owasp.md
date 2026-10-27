# Error handling and OWASP alignment

## Purpose and scope

This page defines how ViajantesTurismo reports failures to users, what it records about them, and how new or unknown errors are triaged. It also records
where the repository stands against the relevant OWASP guidance, so gaps are visible and tracked instead of assumed.

It follows these OWASP sources:

- [Error Handling Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Error_Handling_Cheat_Sheet.html)
- [Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)
- [OWASP Top 10:2025 A09 Security Logging and Alerting Failures](https://owasp.org/Top10/2025/A09_2025-Security_Logging_and_Alerting_Failures/)
- [OWASP Top 10:2025 A10 Mishandling of Exceptional Conditions](https://owasp.org/Top10/2025/A10_2025-Mishandling_of_Exceptional_Conditions/)

An ASVS mapping is intentionally not included because it was not verified. Add it only after each requirement is checked against the ASVS text.

Related pages: the [security baseline](security-baseline.md), the [threat model](threat-model.md), [OpenTelemetry custom telemetry](../OPEN_TELEMETRY.md),
[API I/O and streaming contracts](../API_IO_AND_STREAMING.md), and [production readiness](../operations/production-readiness.md).

## User-facing error rule

The rule is tiered. Pick the tier by what the caller knows about the failure.

### Tier 1: validation errors with a known field

- Field-keyed validation errors go to the `EditContext` through `ValidationMessageStore` (see `EditContextValidationHelper`).
- The form shows an error summary that links to each invalid field and receives focus when it appears, following the
  [GOV.UK error summary pattern](https://design-system.service.gov.uk/components/error-summary/).
- Messages are authored by our own validators, have a bounded length, and never echo user input or file names.

### Tier 2: everything else

- Any failure without a known field, and every non-validation failure, shows fixed copy chosen by the caller (`HttpOutcomeUserMessage`,
  `UserFacingErrorMessage`).
- Server validation text is never joined into a free-form message.
- The RFC 9457 `detail` member exists so the client can help the user correct the problem. Clients must not parse it.
- Users never see a stack trace, exception text, or implementation details.

### Unexpected errors

- Unexpected errors are logged server-side with the exception type only, plus the trace id. Exception messages and stack traces are not logged
  as free text because they can carry personal data.
- The user sees the trace id so support can find the matching trace and log entries.

## Mapping to OWASP recommendations

Status as of 2026-10-27, from a read-only audit. Runtime behavior was not exercised. Evidence lists repository paths only.

| OWASP recommendation | Status | Evidence |
| --- | --- | --- |
| Do not log sensitive data; sanitize log content | Implemented | `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs` (`IncludeFormattedMessage=false`, `IncludeScopes=false`, `LogRecordPrivacyProcessor`, `ActivityPrivacyProcessor`, `RecordException=false`); `docs/security/security-baseline.md` forbids PII, tokens, and CSV rows in logs |
| Fail closed on telemetry privacy | Implemented | `observability/otel-collector/privacy.yaml` |
| Structured logs with stable identifiers | Implemented | `docs/OPEN_TELEMETRY.md` (stable `EventId` values and names) |
| Rate limiting and input size limits | Implemented | `docs/security/security-baseline.md` (named rate-limit policies, import size caps) |
| Do not send exception text or stack traces to clients | Implemented | No exception text is sent to clients; development exception pages are gated by environment in `src/ViajantesTurismo.Management.Web/Program.cs` and `src/ViajantesTurismo.Public.Web/Program.cs` |
| Global exception handler on every service | Partial | `src/ViajantesTurismo.Admin.ApiService/Program.cs` has `AddProblemDetails` and `UseExceptionHandler`; the Catalog API, which hosts Branding, has none |
| Consistent 4xx/5xx mapping and monitoring | Partial | The mapping exists; 5xx responses are not monitored |
| Validation text authored by us, no echo of input | Partial | Two echoes remain: the caller identifier in the documentation 404, and enum states in booking errors |
| Log security events (authentication, authorization, rate limit, validation failures) | Partial | No dedicated 401, 403, JWT-failure, rate-limit, or validation-failure event logging |
| Handle UI-level errors without losing the session | Partial | `blazor-error-ui` exists in `src/ViajantesTurismo.Management.Web/Components/Layout/MainLayout.razor`; there is no `ErrorBoundary` |
| Incident response process | Partial | Notes only in `docs/operations/production-readiness.md`; `SECURITY.md` covers vulnerability reporting only |
| Protect logs from tampering and unauthorized access | Partial | The observability stack under `observability/` is local-only |
| Alert on errors and suspicious activity | Missing | `observability/` has Prometheus, Loki, Tempo, and Grafana dashboards but no alert rules; `docs/operations/production-readiness.md` says thresholds are undecided |
| Summarize repeated errors | Missing | No aggregation of repeated errors |
| Never swallow exceptions silently | Missing | The malware-scanner catch in `src/ViajantesTurismo.Admin.ApiService/Customers/CustomerImportEndpoints.cs` and about a dozen pages in `src/ViajantesTurismo.Management.Web/Components/Pages` catch without logging |
| Honeytokens | Missing | Not expected now |

## Triage flow for new and unknown errors

Triage uses regular telemetry only. There is no separate error-tracking system.

1. Unhandled failures, and handled failures that are swallowed, are logged with a stable `EventId` and counted in `unhandled_exceptions_total`
   tagged `error_type`, `route_template`, and `handled`.
2. An alert fires when a first-seen `error_type` appears, or when the 5xx ratio breaches its threshold.
3. The responder opens the trace id in the logs and traces backend and classifies the error as known or new.
4. The fix starts with a failing test, as required by the repository `AGENTS.md`. Then add or adjust fixed copy or a mapped error.
5. Close the alert, or silence it with a recorded reason.

Current state:

- Exists today: structured logs with stable `EventId` values, the privacy processors, trace ids, and the local Prometheus, Loki, Tempo, and Grafana stack.
- Being delivered in two places: the tiered user-facing rule in the web client in this repository,
  and the `unhandled_exceptions_total` counter, its tags, and the exception handler in SharedKernel,
  which this repository adopts once a prerelease candidate exists.
- Open decisions, not yet made: the production telemetry sink, alert routing, alert thresholds (including the 5xx ratio), and the owner of triage.
  This page does not set values for them. They belong in the deployment runbook referenced from `docs/operations/production-readiness.md`.

## Reuse

The reusable parts are tracked for extraction into SharedKernel:

```text
https://github.com/danigutsch/PandaBear.SharedKernel/issues/188
https://github.com/danigutsch/PandaBear.SharedKernel/issues/189
https://github.com/danigutsch/PandaBear.SharedKernel/issues/190
https://github.com/danigutsch/PandaBear.SharedKernel/issues/191
https://github.com/danigutsch/PandaBear.SharedKernel/issues/192
https://github.com/danigutsch/PandaBear.SharedKernel/issues/193
```

Library changes belong in SharedKernel and need application compatibility validation before adoption, as described in `docs/sharedkernel/development.md`.
