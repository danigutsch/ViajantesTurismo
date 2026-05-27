# Fixture Seam Guidance

Fixture seams support the test taxonomy. They are not the taxonomy itself.

## Principles

- Match fixture scope to the layer being tested.
- Prefer named lifecycle operations over generic container access.
- Keep test bodies dependent on business-visible entrypoints, not host internals.
- Keep fixture seams aligned with Aspire-managed hosting for full-host Admin tests.
- Do not expose public `IServiceProvider` access or generic "run inside scope" helpers from shared Admin fixtures.

## Guidance by test type

The Admin hosted seams are:

- API integration SUT seam expressed through typed contract clients
- browser SUT seam expressed through a hosted web entrypoint only
- deterministic support seam expressed through named lifecycle operations and typed setup clients

New full-host Admin fixtures must follow those exact seam boundaries instead of inventing broader host contracts.

### Unit, behavior, component, and architecture tests

- Prefer no heavyweight shared fixture at all.
- Use local setup or narrow class fixtures only when they improve readability or cost.
- Do not invent host abstractions for tests that do not host the app.

### API integration tests

- Use fixture seams to provide:
    - typed contract clients for bookings, customers, and tours
    - only the minimal host metadata needed to create those clients
    - named data lifecycle methods such as `Seed` and `Reset`
- Do not expose `IServiceProvider`, generic scope runners, or arbitrary service lookup to test bodies.
- Keep scope creation private inside fixture implementation.

### Contract tests

- Prefer the published contract artifact itself as the seam, such as a generated OpenAPI document, a serialized payload sample, or a schema fragment.
- If hosting is required to produce that artifact, keep the fixture focused on contract generation only.
- Do not add seed/reset operations or generic API clients to a contract-test fixture unless that specific artifact truly requires them.
- Do not broaden a contract fixture into a general-purpose integration host.

### System UI / E2E tests

- Keep browser entrypoints primary.
- Allow narrow API-assisted setup only to create deterministic owned data or reset state.
- Keep the browser SUT seam separate from the setup/reset support seam.
- Keep host and infrastructure details behind fixture, page, and workflow helpers.
- Express destructive or baseline-changing operations with named methods such as `Seed` and `ClearDatabase`, not generic plumbing helpers.

## xUnit fixture scope

- Use per-test class setup when isolation matters most.
- Use class fixtures when one class intentionally shares expensive setup.
- Use collection fixtures when multiple classes must intentionally share one context.
- Use assembly fixtures only when the whole assembly truly shares one context and that context is safe under the required parallelism model.

Assembly-wide sharing is a specialized tool, not the default architecture.

## Tagging reminder

Use tag dimensions such as `Scope`, `Surface`, `Area`, `Category`, and `Host` so host migration does not force the taxonomy to change.
