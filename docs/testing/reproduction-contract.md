# Deterministic reproduction fixtures

Use the repository test host for the failing application boundary. A reproduction fixture creates
synthetic state inside that host and verifies a named outcome. Fixture owners keep the linked
entry-point inventory current when a route family or background service changes.

## HTTP and browser inventory

The [generated endpoint route map](../architecture/generated-endpoint-route-map.md) supplies the
individual Minimal API paths and operation names. Apply the classifications below by owning
application and route family. For the web applications, include component routes, convention-based
endpoints, authentication callbacks, and the framework mappings in each host.

| Entry points | Disposition | Owner and reason |
| --- | --- | --- |
| Admin API customers, tours, bookings and their command/read endpoints | Supported | Admin API. Compose the real routing, auth, validation and mediator pipeline with controlled state. |
| Admin API customer import | Follow-up | Admin API. Use the existing loopback Kestrel fixture when the bug concerns cancellation, multipart or disconnects. Synthetic fixture generation stays in the test. |
| Admin API payments, accounts, instructions, documents and holds | Follow-up | Admin API. Use the matching command/contract fixture and add a named synthetic business state before extending the artifact format. Exclude all documents and financial records from artifacts. |
| Catalog API management tours, public content and media metadata, plus Branding settings | Supported | Catalog and Branding API. The existing hosts can exercise authorization and conditional-update outcomes with small synthetic records. |
| Catalog and Branding upload, storage, scan and public media transfer | Follow-up | Catalog and Branding API. Select an existing protocol test and generated media fixture when the failure depends on real provider or scanner behavior. |
| Public Catalog/Branding reads, Public Web home, galleries, tours, localized content, theme, robots and sitemap | Supported | Catalog API and Public Web. Use known locale, route category and seeded publication state with the existing public hosts. |
| Management Web customers, tours, bookings, Catalog, Branding, documents and finance pages | Supported | Management Web. Use components for render/form behavior and an existing System browser fixture when the workflow crosses application boundaries. |
| Management document download and media preview proxies | Supported | Management Web. The existing proxy tests can verify access and downstream outcomes while retaining HTTP status and headers by name alone. |
| Management login, logout, OIDC callbacks and cookie continuation | Follow-up | Management Web. Use the owned conformance realm and synthetic operator. Capture only the expected redirect category or access result. |
| Error pages, health/readiness, OpenAPI, static assets, and framework routes such as Blazor transport | Excluded from fixture artifacts | Owning host and platform. Use existing contract and smoke tests for framework behavior and sanitized service state for diagnostics. |

## Background and integration inventory

| Entry points | Disposition | Owner and reason |
| --- | --- | --- |
| Admin transport relay and Catalog worker consumers, including tour projection | Supported | Catalog worker. Reuse transport fixtures for delivery claims and the projection runner for deterministic projection state. |
| Catalog-hosted Branding outbox and Admin worker projection/inbox | Supported | Branding and Admin worker. Use the existing PostgreSQL projection fixture for duplicate, retry and version outcomes. |
| Catalog projection and media reconciliation, Branding logo reclamation | Follow-up | Catalog and Branding Infrastructure. Use the existing scoped services and add a fake clock and owned media objects as needed for each new scenario. |
| Admin document draft, audit and idempotency retention | Follow-up | Admin Infrastructure. Use a scoped disposable database and fixed time. Cover hold and retention policy according to the owning test contract. |
| Migration/setup and payment protection startup validation | Excluded from fixture artifacts | Migration Service and Admin Infrastructure. Use the reviewed SQL migration rehearsals and startup checks. These entry points act on store or key material. |
| External AI, malware scanner, object storage, identity provider and OTLP transport | Excluded from cross-environment reproduction | Owning adapter. Use deterministic infrastructure doubles unless a protocol test needs a test-owned, pinned provider. |

## Supported scenario setup

Use the pinned SDK and solution test runner. Build and run the chosen class through
`dotnet test --project <project> --filter-class <class>`. Select the documented
[hosted-test prerequisites](../../tests/ViajantesTurismo.HostedTests/README.md) for any
PostgreSQL, container or browser case. Let each fixture own and dispose its records or database
lease. Seed dates, resources, permissions and delivery order explicitly. Await a real application
state or a bounded synchronization signal when work spans hosts.

| Scenario | Minimal input and reset | Existing test seam and expected outcome |
| --- | --- | --- |
| Admin Tour retirement | Synthetic staff with each relevant role, one tour with a chosen booking state, fresh API state per scenario | `TourRetirementEndpointTests` in Admin.ApiServiceTests exercises denied and accepted transitions through HTTP. |
| Catalog management update and public read | One published synthetic record and a fixed locale. Seed a second revision for the stale-write case. | `CatalogApiEndpointTests` in Catalog.ApiServiceTests exercises the production HTTP boundary and controlled read/write state. |
| Public localized browsing and admission | One synthetic published tour, fixed locale and independent in-process hosts. Recreate hosts to reset quotas. | `PublicCatalogDeploymentTrustTests` in Catalog.ApiServiceTests proves visitor isolation with controlled addresses. |
| Management document proxy | A synthetic Admin or unauthorized operator and an in-memory downstream outcome. Create a fresh host per test. | `DocumentArtifactProxyEndpointTests` in Management.WebTests checks access and the safe download envelope. |
| Management browser form | A synthetic role and one owned booking or tour. Start the System stack with its conformance realm, use a fresh browser context and a unique scenario record. | Existing `Admin/System` tests provide accessible selectors and bounded UI waits. Verify the named validation or workflow status. |
| Catalog projection retry | One synthetic tour event, the known projection checkpoint and a controlled retry outcome. Recreate the runner fixture each time. | `CatalogProjectionRunnerTests` in Catalog.UnitTests inspects projection progress against the known state. |
| Branding duplicate delivery | One synthetic, versioned Branding event, an owned PostgreSQL database and a repeat delivery with the same synthetic identity. | `BrandingProjectionPostgreSqlTests` in HostedTests verifies the stored projection outcome. |

## Run the Admin Tour retirement fixture

Use a source checkout with the repository SDK and [pinned packages](../sharedkernel/development.md) staged.
Run the fixture from the repository root. It uses the in-process Admin test server and the repository's test
bearer issuer. Each attempt creates a new host, tour store, unit of work and client. The application clock is fixed
and the scenario seeds at most one synthetic booking.

```sh
dotnet test --project tests/ViajantesTurismo.Admin.ApiServiceTests/ViajantesTurismo.Admin.ApiServiceTests.csproj \
  -c Release --filter-class '*TourRetirementReproductionTests'
```

Each case runs twice. An Admin with no active booking gets `202` and can query the pending retirement with `200`.
An Admin with a pending booking gets `409`. A Reader with a valid test token gets `403`. Both rejections leave the
tour and booking state intact and perform no save. The test checks the real routing, permission, mediator and
domain boundaries. Browser paths and identity-provider conformance have separate fixtures in the inventory.

Set `VIAJANTESTURISMO_REPRODUCTION_MANIFESTS=1` for the same command to keep the six approved attempts in
`TestResults/reproduction/`. The filename is the attempt ID. The manifest uses the Git revision stamped in the
test assembly and the trace ID observed on the server. Standard test runs use temporary files and dispose them.
The registered outcome codes are `accepted-pending`, `active-bookings-conflict`, and `forbidden`. The fixture
registers `admin-api` as its only diagnostic resource. To repeat a report, check out its revision and run the
fixture class again.

## Reproduction artifact contract

The reproducible input is source-controlled fixture code plus a stable scenario identifier, schema
version and revision. Generate test data from reviewed constants or a recorded deterministic seed.
Local output may contain a sanitized JSON manifest and the existing MTP result. Persist only the
following fields in a reproduction manifest.

| Field | Allowed value |
| --- | --- |
| `schemaVersion`, `revision`, `scenario` | Supported format version, source SHA and a registered fixture-case name |
| `seed` | Optional fixture generator seed for synthetic state |
| `attemptId`, `traceIds` | Fresh opaque run ID and trace IDs observed for this test, when available |
| `result`, `outcomeCode`, `resourceStates` | Controlled values from the fixture, with allowlisted diagnostic resource names |
| `startedUtc`, `durationMs` | UTC start time and elapsed time up to ten minutes |

Write the manifest as UTF-8 JSON with a 64 KiB size limit using the allowlisted fields. Reject
unknown fields and values outside the documented format in any reader. Keep existing safe MTP
results separate. Store only in the local results directory or the existing repository-scoped test
artifact, with the CI slice's seven-day retention and access permissions.

Exclude real identities and production data, secrets, credentials, tokens, cookies, CORS or
forwarding header values, connection strings, SQL parameters, signed URLs, request/response
bodies, message payloads, generated documents, uploads and raw exception text. Keep browser
screenshots, traces, HAR, process dumps and broad log capture out of the reproduction artifact.
Run browser and integration scenarios only against local or approved non-production test hosts.

Propagate the standard trace context on test-owned clients. Use a generated attempt ID for local
file names and one attempt Activity. Record trace IDs actually emitted by the participating hosts.
Use existing asynchronous envelope correlation if it is already safe and available. Record a
missing link when a hop has no trace. Keep test identities, route parameters, payload values,
attempt IDs and trace IDs out of metric labels. Select only the relevant failure events from the
existing sanitized diagnostics path, with its existing time and size limits.

## Implementation acceptance

- API and browser fixtures must cover a repeatable accepted or rejected result under the real
  authorization and validation boundary. Use two independent resets and verify the same outcome.
  Keep synthetic fixture data and reviewed outputs in the scenario, outside the manifest fields.
- Background fixtures must verify the observed delivery, retry or rejection under bounded
  cancellation. Repeat through the appropriate application and persistence boundary.
- Both groups must verify that malformed or sensitive input never becomes an artifact field,
  including headers, token-shaped values, message bodies, and browser state. Prove an actionable
  application failure with a focused regression test before changing its production path.

Existing diagnostic capture research covers separate failure bundles and local retrieval.
[Integration and system test diagnostics](../ci/test-diagnostics.md)

- [ASP.NET Core integration tests and WebApplicationFactory](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
- [OpenTelemetry handling sensitive data](https://opentelemetry.io/docs/security/handling-sensitive-data/)
- [W3C trace context and privacy](https://www.w3.org/TR/trace-context/#privacy)
