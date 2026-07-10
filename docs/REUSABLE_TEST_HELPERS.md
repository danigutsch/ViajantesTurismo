# Reusable Test Helpers

This document records when repeated test setup should become a helper and when it should stay in the
test body.

## Rule

Add shared test helpers only when there are at least two current callers and the helper hides
mechanics rather than behavior. Keep the behavior under test visible in the test method.

## Helper candidates that are allowed

| Candidate | When useful | Preferred location |
| --- | --- | --- |
| Assertion wrappers | Repeated assertion pattern with no existing wrapper | `SharedKernel.Testing.Assertions` |
| Roslyn analyzer/code-fix workspace setup | Multiple analyzer/code-fix projects need the same workspace mechanics | Dedicated test support project |
| Typed API clients for setup | Multiple hosted fixtures need deterministic API-assisted setup | Owning contract/test project |
| Health-check smoke helper | Multiple services expose the same health endpoint contract | Project-local test infrastructure first |
| DI registration verification helper | Multiple modules need the same registration contract check | Owning SharedKernel test support only after reuse exists |

## Helper candidates to reject

- generic `IServiceProvider` escape hatches exposed to test bodies
- broad startup frameworks that hide which app or route is being tested
- helpers that mirror production implementation details instead of visible outcomes
- wrappers around a single assertion or setup call with only one current caller
- cross-project abstractions based only on similar names rather than a shared contract

## DI registration checks

Prefer focused checks that prove a named contract can be resolved with required dependencies. Avoid
asserting the entire service collection unless the registration list is the published contract.

Good signals:

- required service resolves from a minimal test container
- duplicate registrations are prevented when that is part of the contract
- scoped dependencies are resolved inside a scope, not from the root provider
- missing required options fail during startup validation when options are the behavior

Avoid exposing raw `ServiceProvider`, `IServiceScope`, or container internals from helper APIs.

## Startup and health-check smokes

Use the cheapest host model that proves the intended contract:

1. no-host composition test for pure DI modules
2. `WebApplicationFactory` or `TestServer` for in-process HTTP behavior
3. Aspire-hosted test only when distributed resources or service discovery are the behavior
4. Playwright/system test only when browser-visible behavior is the target

Health-check tests should assert stable status and essential dependency names, not incidental JSON
ordering or every private probe detail.

## Review checklist

- At least two current callers exist.
- The helper does not hide the observable behavior under test.
- The helper has a narrow name tied to a real contract.
- The helper stays in the closest owning test project unless it is host-agnostic and reusable.
- The helper does not expose raw DI, host, browser, or database plumbing to test bodies.
- The helper improves adding direct coverage under `docs/COVERAGE_OWNERSHIP.md`.
