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
- optional shared hosted-support seam expressed through a narrow host contract when needed
- browser SUT seam expressed through a hosted web entrypoint only
- deterministic support seam expressed through typed setup clients and fixture-owned baseline control

New full-host Admin fixtures must follow those exact seam boundaries instead of inventing broader host contracts.

### Unit, behavior, component, and architecture tests

- Prefer no heavyweight shared fixture at all.
- Use local setup or narrow class fixtures only when they improve readability or cost.
- Do not invent host abstractions for tests that do not host the app.

### API integration tests

- Use fixture seams to provide:
    - typed contract clients for bookings, customers, and tours
    - a narrow shared host contract only when multiple hosted fixtures benefit from the same client/base-address contract
    - only the minimal host metadata needed to create those clients
    - fixture-owned baseline control for clean-slate setup where required
- Do not expose `IServiceProvider`, generic scope runners, or arbitrary service lookup to test bodies.
- Keep scope creation private inside fixture implementation.

### Contract tests

- Prefer the published contract artifact itself as the seam, such as a generated OpenAPI document, a serialized payload sample, or a schema fragment.
- If hosting is required to produce that artifact, keep the fixture focused on contract generation only.
- Do not add seed/reset operations or generic API clients to a contract-test fixture unless that specific artifact truly requires them.
- Do not broaden a contract fixture into a general-purpose integration host.

### System UI / E2E tests

- Keep browser entrypoints primary.
- Allow narrow API-assisted setup through fixture-owned setup clients while keeping baseline control inside fixture or base-class infrastructure.
- Use a narrow shared host contract only when it keeps multiple hosted fixtures aligned
  without making browser tests depend on a universal host abstraction.
- Keep the browser SUT seam separate from the setup/reset support seam.
- Keep host and infrastructure details behind fixture, page, and workflow helpers.
- Express destructive or baseline-changing operations through infrastructure-owned fixture/base-class paths, not test-body plumbing.

## Migration guidance

- Prefer a narrow shared host contract only for hosted fixtures that genuinely share the same
  client/base-address contract.
- Existing hosted fixtures may converge on `Client` and `BaseUri` while keeping baseline control behind fixture infrastructure.
- Keep browser-only concerns such as `WebAppUrl`, page objects, and workflow helpers outside the shared host contract.

## xUnit fixture scope

- Use per-test class setup when isolation matters most.
- Use class fixtures when one class intentionally shares expensive setup.
- Use collection fixtures when multiple classes must intentionally share one context.
- Use assembly fixtures only when the whole assembly truly shares one context and that context is safe under the required parallelism model.

Assembly-wide sharing is a specialized tool, not the default architecture.

## Tagging reminder

Use tag dimensions such as `Scope`, `Surface`, `Area`, `Category`, and `Host` so host migration does not force the taxonomy to change.
