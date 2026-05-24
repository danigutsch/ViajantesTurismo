# Admin Test Architecture Guide

This file is the canonical quick-reference for the Admin test taxonomy.

Generic web UI test projects may support Admin-facing surfaces, but they are not themselves part of an Admin-specific project taxonomy.
Do not assume different web applications share the same UI technology, runtime model, or test stack.

It is intentionally normative.

Use it together with `docs/TEST_GUIDELINES.md`:

- `tests/README.md`: short source-of-truth matrix
- `docs/TEST_GUIDELINES.md`: deeper rules and examples

## Target Taxonomy

### Canonical project families

The repository uses the following permanent test families:

| Family | Meaning |
| --- | --- |
| `UnitTests` | isolated logic |
| `BehaviorTests` | stakeholder-readable business scenarios |
| `WebTests` | stack-specific lower-cost UI tests |
| `UiIntegrationTests` | hosted UI composition below full browser workflow depth |
| `IntegrationTests` | real non-UI boundary collaboration, especially APIs and infrastructure |
| `SystemTests` | full running system through an external entrypoint |
| `ContractTests` | explicit public or external boundary compatibility |
| `ArchitectureTests` | structural rules |
| `Performance` | workload-driven non-functional validation |

Execution intent does not define a project family by default.
Use categories such as `smoke`, `regression`, `load`, `stress`, `spike`, and
`soak` as tags or scenario groupings unless a separate project becomes clearly
justified.

| Project | System under test | Allowed primary entrypoints | Host / runtime model | Real infra | Prohibited reach-through | Aspire fit |
| --- | --- | --- | --- | --- | --- | --- |
| `ViajantesTurismo.Admin.UnitTests` | Domain and application-facing logic that can run fully in memory | Direct type construction, factory methods, value objects, domain methods, small in-memory handlers | No external host | No | HTTP, browser, `IServiceProvider`, database, distributed app bootstrapping | Not applicable |
| `ViajantesTurismo.Admin.BehaviorTests` | Business rules and workflows expressed in Gherkin | Reqnroll step definitions calling domain/application seams through focused test context | In-process scenario execution with Reqnroll/xUnit fixtures | No | Browser/UI automation, raw HTTP API calls, shared static scenario state | Not primary target |
| Web UI test projects | Project-specific UI behavior below full browser workflow depth | The web application's UI surface for that project | Entry points appropriate to that web stack, such as component rendering, hosted route checks, or app-visible UI composition seams | Depends on project and layer | Cross-project assumptions about UI stack, host model, or test tooling | Project-specific |
| `ViajantesTurismo.Admin.IntegrationTests` | Admin API request/response behavior and persistence interactions | `HttpClient` against the Admin API, JSON contracts, deterministic database lifecycle methods | Aspire-managed host ownership for full-host execution | PostgreSQL, real API pipeline | Generic `IServiceProvider` escape hatches from test bodies, direct controller/endpoint invocation, browser assertions | Canonical |
| `ViajantesTurismo.Admin.SystemTests` | End-user Admin flows through the real UI | Playwright page navigation, semantic UI assertions, helper pages/workflows, narrow API-assisted owned-data setup | Browser-driven tests against Aspire-managed running services | PostgreSQL, Redis, API, Blazor UI | Direct assertions on private service state, generic container access, non-deterministic list scanning | Canonical |
| `ViajantesTurismo.Admin.Testing` | Shared test-only contracts, traits, and helper abstractions | Reusable helper types consumed by test projects | No standalone host | No | Business logic ownership, app runtime ownership, test-project-specific orchestration | Should stay host-agnostic |

If current project names differ from the canonical family name, treat that as
migration work rather than as a competing taxonomy.

## Boundary Rules

The canonical seams for hosted Admin tests are:

- API integration SUT seam:
    - typed contract clients `IBookingsApiClient`, `ICustomersApiClient`, and `IToursApiClient`
    - exposed through a narrow integration host contract
    - lifecycle methods limited to named baseline operations such as `Seed(...)` and `Reset(...)`
- UI integration and E2E browser SUT seam:
    - browser-visible web entrypoint only, such as `Uri WebAppUri`
    - no generic API or DI reach-through as part of the browser SUT seam
- UI support seam for deterministic setup:
    - typed contract clients plus named lifecycle methods such as `Seed(...)` and `ClearDatabase(...)`
    - kept separate from the browser SUT seam

These seams define the architecture.

### Unit tests

- Verify pure behavior in memory.
- Prefer direct object construction and explicit inputs.
- Do not start hosts, use HTTP, or depend on seeded databases.

### Behavior tests

- Express business rules in stakeholder-readable language.
- Keep steps at intent level and mechanics in helpers/context objects.
- Do not turn behavior tests into browser or infrastructure tests.

### Web tests

- Verify UI behavior in the web project's own appropriate lower-cost test layer.
- Choose entrypoints and tooling based on that project's actual web stack.
- Do not assume all web projects use Razor components, bUnit, or the same host model.
- Do not use Playwright or real backend infrastructure in this layer unless that project explicitly defines this as its UI integration layer.

### UI integration tests

- Verify hosted UI composition, route-level rendering, and web-app wiring below full browser E2E.
- Prefer app-visible entrypoints over internal service reach-through.
- Reuse typed contract clients where the running web application composes over them.
- Do not collapse this layer into bUnit component tests or Playwright E2E.
- This layer is defined by responsibility, not by a promise that every web project will implement it the same way.

### Integration tests

- Verify the API surface through real HTTP and persistence boundaries.
- Canonical API SUT seam: typed contract clients (`IBookingsApiClient`, `ICustomersApiClient`, `IToursApiClient`)
- Own state through named fixture lifecycle methods such as `Seed` and `Reset`.
- Do not depend on generic service-container reach-through from test bodies.
- Keep API integration focused on the API surface and Aspire-managed full-host execution.

### E2E tests

- Verify real user journeys through Playwright against the Admin web app.
- Canonical browser SUT seam: hosted web entrypoint only.
- Canonical support seam: typed contract clients plus named deterministic setup/reset operations.
- Prefer deterministic navigation by known IDs/routes and semantic UI assertions.
- Use serial execution only for justified clean-slate or destructive-reset scenarios.
- Keep infrastructure and host plumbing behind the fixture and helper layers.

### Shared test helpers

- Keep helper code reusable and host-agnostic where practical.
- Do not move domain/application behavior into shared test infrastructure.
- Avoid creating a generic abstraction that lets any test escape into raw DI or host internals.

## Naming rules

- Name projects by **system under test** and **scope**.
- Do not encode temporary host technology in permanent project names.
- Prefer `SystemTests` over `E2ETests` for the long-term full-system browser lane.
- Use `ContractTests` only when a public or external compatibility boundary matters independently.
- Treat snapshot testing as a technique inside a project before promoting it to a project family.

## Tagging Model

Keep tags orthogonal and stable across host-model migration.

### Scope

- `Scope=unit`
- `Scope=behavior`
- `Scope=component`
- `Scope=ui-integration`
- `Scope=integration`
- `Scope=e2e`
- `Scope=architecture`

### Surface

- `Surface=domain`
- `Surface=application`
- `Surface=api`
- `Surface=web`
- `Surface=workflow`
- `Surface=solution`

### Area

- `Area=bookings`
- `Area=customers`
- `Area=tours`
- `Area=payments`
- `Area=shared`

### Intent

- `Category=smoke`
- `Category=regression`
- `Category=happy-path`
- `Category=edge-case`

### Host

- `Host=in-memory`
- `Host=aspire`
- `Host=browser`

## Rules

- Prefer the cheapest layer that proves the behavior.
- Keep host model separate from business scope.
- Full-host integration and E2E execution use Aspire-managed hosting.
- Do not add host abstractions that conflict with Aspire-managed execution.
