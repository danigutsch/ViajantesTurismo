# STRIDE-A threat model

## Scope

This model covers the current Aspire-hosted application: Public Web, Management Web, Admin API,
Catalog API, Integration Event Worker, PostgreSQL databases, Redis cache, local media/object storage,
OpenTelemetry exporters, and external clients such as browsers and API clients.

## Actors and assets

- Anonymous visitor: reads published tour, gallery, theme, and marketing content through Public Web.
- Operator/editor: uses Management Web for tours, customers, bookings, public content, theme, media,
  and customer import workflows.
- Internal services: Admin API, Catalog API, Integration Event Worker, migration service, databases,
  cache, and telemetry pipeline.
- Assets: Customer PII, booking/payment state, medical and identification details, media files,
  public content, integration events, logs, traces, and configuration.

## Trust boundaries and data flows

1. Browser to Public Web: anonymous HTTPS requests for published content.
2. Browser to Management Web: operator HTTPS requests for administrative flows.
3. Management Web to Admin API and Catalog API: internal service-discovery HTTP calls.
4. Admin API to Admin database: customer, booking, tour, import, and integration-event transport writes.
5. Catalog API to Catalog database and object storage: public catalog, media metadata, and public content.
6. Admin integration events to Catalog consumers: tour/public catalog projection flow.
7. Integration Event Worker to Admin/Catalog databases: outbox/inbox and projection execution.
8. Services to telemetry exporters: logs, metrics, and traces without Customer PII payloads.

## STRIDE-A findings

| Area | Threat | Baseline control |
| --- | --- | --- |
| Public Web | Spoofed framing or browser capability abuse | Security headers and CSP for public pages |
| Management Web | Cross-site UI embedding or unsafe browser defaults | Management CSP allows Blazor SignalR only for `connect-src` |
| APIs | Cross-origin browser calls from untrusted origins | Explicit configured CORS allowlist, default deny |
| Public reads | High-volume scraping or accidental load spike | Public-read rate-limit policy with browsing-friendly threshold |
| Mutations/import | Abuse of write or import endpoints | Stricter mutation/import rate-limit policies |
| Customer PII | Disclosure through logs or EF diagnostics | Sensitive data logging remains development-gated; logging guidance forbids PII payloads |
| Upload and import | Oversized, malformed, or wrong content-type CSV upload | Customer import validation rejects non-CSV, empty, and oversized files with generic ProblemDetails |
| Media upload | Malware or unsafe binary content | Scanner integration remains tracked separately |
| Auditability | Sensitive business actions need durable audit trail | Audit design remains separate from baseline controls |
| Baseline drift | Controls removed during future refactors | .NET security baseline tool checks docs and wiring |

## Notes

Authorization provider and policy implementation are intentionally outside this epic and remain governed by
the authentication/authorization epic. This baseline does not weaken existing validation, privacy, or data
safety rules.
