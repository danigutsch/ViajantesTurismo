# Privacy classification and redaction

ViajantesTurismo treats GDPR and Brazil LGPD as baseline privacy requirements.
The engineering rule is: collect less, store less, expose less, log less.

## Technical taxonomy

- Personal: email, phone, name, address, nationality, occupation, social profile handles, customer
  identifiers, booking identifiers linked to a traveller, and other values that identify a person.
- Sensitive: national ID, health or medical information, allergy information, physical data, booking
  notes, and other confidential free text that can contain personal data.
- Credential: passwords, tokens, API keys, authentication headers, session or ticket identifiers,
  connection secrets, and payment secrets.
- Financial: payment references and non-public pricing or payment details linked to a customer or booking.
- Operational data: status codes, operation names, low-cardinality outcomes, and opaque correlation IDs.

These are technical handling classes only. They do not encode legal basis, consent, purpose, or
retention periods.

### Customer and booking baseline

| Field group | Examples | Classification | Logging rule |
| --- | --- | --- | --- |
| Customer identity and contact | Name, email, phone, address, nationality, occupation, social handles, emergency contacts | Personal | Do not log values; use bounded operation outcomes |
| Customer high-risk details | National identifiers, birth date, health, allergy, medical, accommodation, and physical information | Sensitive | Never log or attach to telemetry |
| Booking relationship and free text | Customer-linked booking identifiers, traveller assignments, notes, discount reasons, and accommodation requests | Personal or Sensitive | Do not log identifiers or content |
| Payment and pricing | Payment references, payment notes, customer-linked prices, discounts, and balances | Financial | Do not log values; expose only bounded payment status when needed |
| Authentication material | Passwords, tokens, cookies, tickets, API keys, and connection secrets | Credential | Never log, trace, or return in error payloads |

This table is the focused logging baseline. A complete model, DTO, storage, import/export, and event
flow inventory remains a separate architecture concern.

## Logging and telemetry

- Do not log request or response bodies that may contain personal data.
- Do not log personal data in URL paths, query strings, telemetry tags, metric dimensions, or log scopes.
- Prefer source-generated `LoggerMessage` methods.
- Apply `PersonalData`, `SensitiveData`, `CredentialData`, or `FinancialData` only to
  source-generated logging parameters that can carry the corresponding class. Do not decorate DTOs
  or operational fields merely because they appear near personal data.
- Prefer omitting sensitive values entirely. Classification and redaction are for a currently required
  structured field, not justification to add one.
- Default redaction erases classified values before logs leave the process.
- `AddRedaction()` plus `EnableRedaction()` protects both formatted messages and structured parameter
  values before providers run. The fallback remains the erasing redactor; do not replace it with a
  pass-through fallback.
- Classification attributes do not make arbitrary scope objects safe. Scopes must contain operational
  values only; application OTLP logging does not export scopes or preformatted messages.
- Log bounded exception type names, not exception objects, messages, stack traces, request or response
  bodies, headers, query values, customer identifiers, booking identifiers, or object-storage keys.
- Keep `http.route`, request method, server address, response status, operation, and low-cardinality
  outcome fields. Remove raw `url.path`, `url.full`, query values, and customer or booking identifiers
  before trace export.

Redaction reduces accidental disclosure risk. It is not permission to collect or log more data.

## Current implementation

Shared observability enables `Microsoft.Extensions.Compliance` logging redaction with an erasing
fallback redactor. Classified source-generated logging parameters are removed from formatted log
messages and structured state before providers receive them. Service defaults remove preconfigured
logging providers so only the sanitized OpenTelemetry pipeline is enabled by default, and also remove exception
payloads, raw paths, query values, and customer or booking identifiers from OTLP-bound logs and traces.
Scanner diagnostics retain status and bounded failure type only; scanner messages and storage keys are
not logged. ASP.NET Core, HttpClient, gRPC, Entity Framework Core, Npgsql, AWS SDK, and custom tracing
remain enabled. AWS tracing suppresses duplicate downstream HTTP spans and retains S3 source-tag
redaction. Npgsql does not enable parameter logging and suppresses its optional first-response event.

Provider failure spans can contain immutable exception events that an application processor cannot
remove after the activity stops. Every supported AppHost profile therefore routes normal
Aspire-annotated OTLP traffic through the pinned trusted Collector. The Collector drops all span events,
clears status descriptions, and removes explicit URL, header, body, query, parameter, identifier, and
AWS resource attributes before forwarding traces to the Aspire dashboard or optional backends. Raw
telemetry exists on the trusted application-to-Collector hop. A manually constructed direct exporter or
a process outside AppHost can bypass this boundary. Signal-specific OTLP endpoint variables can also
override the generic endpoint rewritten by AppHost. Deployments must reject or rewrite those overrides,
restrict direct backend access, and configure authenticated and encrypted Collector ingress and
downstream transport.

## References

- Microsoft Learn: <https://learn.microsoft.com/en-us/dotnet/core/extensions/compliance>
- Microsoft Learn data classification: <https://learn.microsoft.com/en-us/dotnet/core/extensions/data-classification>
- Microsoft Learn data redaction: <https://learn.microsoft.com/en-us/dotnet/core/extensions/data-redaction>
- [Document retention and legal-hold policy proposal](operations/document-retention-and-legal-hold.md)
- GDPR Article 5, 9, 25, and 32: <https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng>
- Brazil LGPD law: <https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm>
