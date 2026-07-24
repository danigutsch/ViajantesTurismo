# API request shape policy

Choose each HTTP input location by its meaning. This repository has no universal body-only,
no-route, or no-query rule. Request binding must preserve resource semantics, privacy, generated
OpenAPI accuracy, and client compatibility.

<!-- api-request-shape-policy:start -->

## Decision

| Input location | Use it for | Representative current contract | Required contract behavior |
| --- | --- | --- | --- |
| Route | Resource or subresource identity needed to address the operation | `/api/v1/customers/{id}` and `/api/v1/catalog/tours/{id}/images` | OpenAPI uses `in: path`; route parameters are required. Keep sensitive data out of route values. |
| Query | Filtering, paging, sorting, localization, or other read modifiers | `/api/v1/public/catalog/content/{key}?language=...&culture=...` | OpenAPI uses `in: query` and records requiredness accurately. Optional modifiers remain optional. |
| Header | Standard protocol metadata or an approved operation control orthogonal to the resource representation | Optional `Idempotency-Key` on document generation and regeneration | OpenAPI uses `in: header`. Validate bounded values and do not log header contents. |
| JSON body | Command state, complex input, and sensitive or personal fields | Customer create/update and `POST /api/v1/bookings/{id}/payments` | Use a typed request body and the correct media type. Required command bodies remain required. |
| `multipart/form-data` | File uploads with associated form metadata | Customer CSV import and Catalog tour image upload | Model fields under the multipart request body, never as a query object. Preserve request limits and upload controls. |
| Request stream | Large binary input when a raw stream is materially better than multipart | No current public request contract uses a raw stream; current multipart files are streamed after binding | Require an explicit content type and length policy, bounded reads, cancellation, authorization, rate limiting, and content inspection or scanning. |

Resource identity stays in routes. Moving an identifier into a body solely to satisfy a signature
style rule weakens normal HTTP addressing and generated client ergonomics. Query parameters remain
appropriate for optional read modifiers that do not change resource identity.

Command state belongs in a body. For example, a booking ID identifies the target of
`POST /api/v1/bookings/{id}/payments`, while payment details are JSON command state. Customer
personal, identification, contact, address, emergency-contact, and medical fields likewise remain
in JSON bodies rather than URL parameters.

## Security and privacy

Sensitive data must never be placed in route or query values. URLs commonly reach access logs,
browser history, referrer data, caches, metrics, traces, copied links, and support tooling. This
prohibition includes credentials, tokens, personal details, medical information, payment details,
and uploaded file contents or metadata that can identify a person.

Bodies, forms, and headers are not automatically safe storage. Keep default telemetry redaction,
avoid payload and header logging, authorize before processing protected data, validate all inputs,
and retain the existing rate limits. An approved header carries protocol or operation metadata; it
is not a workaround for hiding application state.

For uploads, preserve authentication and authorization, request-size and per-file limits, media-type
and filename checks, bounded streaming, cancellation, malware scanning, image signature/probe checks,
and fail-closed scanner behavior where currently required. This policy does not relax antiforgery or
other upload controls at browser boundaries.

Query modifiers can affect caching. Add or change a query parameter only with the corresponding
cache-key, variation, normalization, and invalid-input behavior. The Catalog `language` alias is
normalized to the canonical `culture` cache key before output-cache lookup.

## Binding and `[AsParameters]`

`[AsParameters]` groups members for Minimal API binding; it does not imply query binding. Binding
metadata on each member remains authoritative. `CommitCustomerImportFormDto` and
`MediaImageUploadFormDto` use `[FromForm]` members, so their generated contract is a
`multipart/form-data` request body.

Do not infer transport location from a DTO shape or from the presence of `[AsParameters]`. Use
explicit binding metadata when inference would be ambiguous, then verify the generated OpenAPI
document.

## OpenAPI and client implications

Canonical OpenAPI artifacts are the review boundary. Contract tests must prove representative
parameter location and requiredness, JSON request bodies, multipart request bodies, and the absence
of sensitive URL inputs. Do not maintain a manual endpoint-count inventory; generated artifacts and
focused semantic assertions are less likely to drift.

Changing an input between route, query, header, form, body, and stream changes the HTTP contract and
usually generated client code. Review it as a compatibility change, refresh the canonical artifact
only when intentional, and update contract-owned clients and tests together. See
[API compatibility gates](API_COMPATIBILITY.md) and [API client boundaries](API_CLIENT_BOUNDARIES.md).

## Analyzer decision

Do not add a global body-only, no-route, or no-query analyzer or configuration. Architecture coverage
deterministically allowlists the global build/import surfaces, analyzer package and project
registrations, `.globalconfig` files, and configured repository diagnostic IDs. It does not infer rule
intent from source-token text. The owning style and testing analyzer tests enumerate their supported
diagnostic contracts; neither contains a request-location rule. Admin and Catalog contract tests prove
the intentional mixed request locations above.

A future request-shape analyzer needs all of the following evidence:

1. one precise semantic rule with explicit opt-in scope, not a blanket preference for one location;
2. at least two demonstrated production violations of that same rule; and
3. failing tests that prove each actionable production gap before endpoint code changes.

Until then, documentation, canonical OpenAPI, focused contract tests, and review are the smaller and
safer enforcement path.

## Change discipline

Do not churn production endpoint signatures to make them visually uniform. Change a production
request shape only when the existing public contract is objectively unsafe or semantically wrong.
Add the failing contract or security regression test first, then preserve authorization, validation,
privacy, cache behavior, upload controls, and client compatibility in the fix.

<!-- api-request-shape-policy:end -->

## Related guidance

- [API versioning](API_VERSIONING.md)
- [Domain validation](DOMAIN_VALIDATION.md#api-integration)
- [Coding guidelines](CODING_GUIDELINES.md#endpoint-mapping)
- [Security baseline](security/security-baseline.md)
