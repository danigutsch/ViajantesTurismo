# Error handling and OWASP alignment

## Purpose and scope

This page defines how ViajantesTurismo reports failures to users, what it records about them, and how new or unknown errors are triaged. It also records
where the repository stands against the relevant OWASP guidance, so gaps are visible and tracked instead of assumed.

It follows these OWASP sources:

- [Error Handling Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Error_Handling_Cheat_Sheet.html)
- [Logging Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html)
- [OWASP Top 10:2025 A09 Security Logging and Alerting Failures](https://owasp.org/Top10/2025/A09_2025-Security_Logging_and_Alerting_Failures/)
- [OWASP Top 10:2025 A10 Mishandling of Exceptional Conditions](https://owasp.org/Top10/2025/A10_2025-Mishandling_of_Exceptional_Conditions/)

A full ASVS mapping is intentionally not included. Only requirement 16.5.1 of ASVS 5.0 (a generic message for unexpected
or security-sensitive errors, with no stack traces, queries, keys or tokens) was checked against the ASVS text. Add other
requirements only after each is checked the same way.

Related pages: the [security baseline](security-baseline.md), the [threat model](threat-model.md), [OpenTelemetry custom telemetry](../OPEN_TELEMETRY.md),
[API I/O and streaming contracts](../API_IO_AND_STREAMING.md), and [production readiness](../operations/production-readiness.md).

## User-facing error rule

The rule is tiered. Pick the tier by what the caller knows about the failure.

### Tier 1: validation errors with a known field

- Field-keyed validation errors go to the `EditContext` through `ValidationMessageStore`, applied by the SharedKernel
  `ServerValidationBinder` with a list of known fields and a length cap per message.
- The form shows an `ErrorSummary` that links to each invalid field and receives focus when it appears, following the
  [GOV.UK error summary pattern](https://design-system.service.gov.uk/components/error-summary/).
- Messages are authored by our own validators, have a bounded length, and never echo user input or file names.

### Tier 2: everything else

- Any failure without a known field, and every non-validation failure, shows fixed copy chosen by the caller
  (`HttpOutcomeMessages` from SharedKernel, with the copy strings from `UserFacingErrorMessage`).
- Unhandled faults in a page are caught by the `RecoveryErrorBoundary` in the layout and logged through
  `IUiFailureReporter`, which records the exception type and trace id only.
- Server validation text is never joined into a free-form message.
- The RFC 9457 `detail` member exists so the client can help the user correct the problem. Clients must not parse it.
- Users never see a stack trace, exception text, or implementation details.

### Unexpected errors

- Unexpected errors are logged server-side with the exception type only, plus the trace id. Exception messages and stack traces are not logged
  as free text because they can carry personal data.
- The user sees the trace id so support can find the matching trace and log entries.

### What domain errors may echo

Domain errors, such as those in `src/ViajantesTurismo.Admin.Domain/Tours/BookingErrors.cs`, may interpolate domain enum
member names, integers and limits that the domain computed or validated. They must not interpolate caller-supplied free
text, file names, request identifiers or exception messages. The contract enums travel as numbers, so an unknown string
fails in deserialization with a message that does not contain it, and an out-of-range number echoes only as an integer.
A listing of the valid values helps the client correct the request, as RFC 9457 intends for `detail`. The decision and
the evidence are in [ADR-043](../adr/20261028-domain-error-messages-may-echo-domain-terms.md).

## Mapping to OWASP recommendations

Status as of 2026-10-27, from a read-only audit. Runtime behavior was not exercised. Evidence lists repository paths only.

| OWASP recommendation | Status | Evidence |
| --- | --- | --- |
| Do not log sensitive data; sanitize log content | Implemented | `src/ViajantesTurismo.ServiceDefaults/ServiceDefaultsExtensions.cs` (`IncludeFormattedMessage=false`, `IncludeScopes=false`, `LogRecordPrivacyProcessor`, `ActivityPrivacyProcessor`, `RecordException=false`); `docs/security/security-baseline.md` forbids PII, tokens, and CSV rows in logs |
| Fail closed on telemetry privacy | Implemented | `observability/otel-collector/privacy.yaml` |
| Structured logs with stable identifiers | Implemented | `docs/OPEN_TELEMETRY.md` (stable `EventId` values and names) |
| Rate limiting and input size limits | Implemented | `docs/security/security-baseline.md` (named rate-limit policies, import size caps) |
| Do not send exception text or stack traces to clients | Implemented | No exception text is sent to clients; development exception pages are gated by environment in `src/ViajantesTurismo.Management.Web/Program.cs` and `src/ViajantesTurismo.Public.Web/Program.cs` |
| Global exception handler on every service | Partial | The Admin and Catalog API hosts (the Catalog API also hosts Branding) register `AddUnhandledExceptionHandler` and `UseExceptionHandler` in their `Program.cs`; Management.Web and Public.Web keep `/Error` pages |
| Consistent 4xx/5xx mapping and monitoring | Partial | The mapping exists; the API hosts export `unhandled_exceptions_total`, but no alert or 5xx ratio rule consumes it yet |
| Validation text authored by us, no echo of input | Implemented | Domain errors echo only domain enum names, integers and limits, never caller-supplied text; see the rule under Tier 2 and [ADR-043](../adr/20261028-domain-error-messages-may-echo-domain-terms.md). The documentation 404 uses a fixed detail |
| Log security events (authentication, authorization, rate limit, validation failures) | Partial | The Admin and Catalog API hosts call `AddSecurityEventTelemetry` (JWT bearer failure, challenge, forbidden, rate-limit rejection; `EventId` 4101 and 4102); validation failures are not logged as security events |
| Handle UI-level errors without losing the session | Implemented | `src/ViajantesTurismo.Management.Web/Components/Layout/MainLayout.razor` wraps page content in the SharedKernel `RecoveryErrorBoundary`, which shows fixed recovery copy, resets on navigation and logs through `IUiFailureReporter`; `blazor-error-ui` remains as the last resort. Handled failures are logged and counted in `handled_ui_failures_total` (meter `PandaBear.Blazor`, tags `error_type` and `operation`) |
| Incident response process | Partial | Notes only in `docs/operations/production-readiness.md`; `SECURITY.md` covers vulnerability reporting only |
| Protect logs from tampering and unauthorized access | Partial | The observability stack under `observability/` is local-only |
| Alert on errors and suspicious activity | Missing | The signals now exist (`unhandled_exceptions_total`, `authentication_events_total`, `rate_limit_rejections_total`), but `observability/` has no alert rules; `docs/operations/production-readiness.md` says thresholds are undecided |
| Summarize repeated errors | Missing | No aggregation of repeated errors |
| Never swallow exceptions silently | Partial | The malware-scanner catch in `src/ViajantesTurismo.Admin.ApiService/Customers/CustomerImportEndpoints.cs` now logs `EventId` 1 `CustomerImportScannerFailed` with the failure type and still fails closed; the Management.Web pages that catch `Exception` report through `IUiFailureReporter` (`EventId` 7001, exception type and trace id only) and still show fixed copy |
| Honeytokens | Missing | Not expected now |

## Triage flow for new and unknown errors

Triage uses regular telemetry only. There is no separate error-tracking system.

1. Unhandled failures are logged with `EventId` 4001 and counted in `unhandled_exceptions_total` tagged `error_type`, `route_template`, and `handled`.
   Handled failures that would otherwise be swallowed are logged with their own stable `EventId`.
2. An alert fires when a first-seen `error_type` appears, or when the 5xx ratio breaches its threshold.
3. The responder opens the trace id in the logs and traces backend and classifies the error as known or new.
4. The fix starts with a failing test, as required by the repository `AGENTS.md`. Then add or adjust fixed copy or a mapped error.
5. Close the alert, or silence it with a recorded reason.

Current state:

- Adopted in the Admin and Catalog API hosts: the SharedKernel exception handler (`AddUnhandledExceptionHandler`), which answers 500 with an RFC 9457
  `application/problem+json` body holding only `status`, `title`, `type`, and `traceId`; and `AddSecurityEventTelemetry` for authentication events and
  rate-limit rejections. `ServiceDefaults` exports the `PandaBear.AspNetCore` meter, so `unhandled_exceptions_total`, `authentication_events_total`,
  and `rate_limit_rejections_total` reach the OpenTelemetry pipeline.
- Existing RFC 9457 error bodies and the error documentation links are unchanged; the handler only covers exceptions that no endpoint maps.
- Not adopted: Management.Web and Public.Web render an error page through `UseExceptionHandler("/Error")`, so they do not register the handler.
  Reporting from those hosts is tracked separately.
- Exists today: structured logs with stable `EventId` values, the privacy processors, trace ids, and the local Prometheus, Loki, Tempo, and Grafana stack.
- Open decisions, not yet made: the production telemetry sink, alert rules, alert routing, alert thresholds (including the 5xx ratio), and the owner of triage.
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
