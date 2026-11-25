# Ownership of shared constants

This inventory groups application constants by meaning and trait key. The same literal can describe separate contracts.

## Test values

| Value or family | Owner and use |
| --- | --- |
| `Scope` | `SharedKernel.Testing.TestScope` and `TestScopeAttribute` define the scope used by test filters. `TestScope.Unit` emits `Scope=unit`. |
| `Host=aspire`, `Host=test-server` | Use `TestHostAttribute` with the pinned `TestHost` enum. It emits the same `Host` key and values that the local traits emitted. |
| `endpoint`, `security` | The pinned `SharedKernel.Testing.TestTraitValues` owns these values. Use the constants directly and preserve each caller's existing category key. |
| `Area=branding`, `Area=catalog`, `Area=management-web`, `Area=shared` | `ViajantesTurismo.Testing.TestAreas` owns labels shared by several application test projects. The test-only project has no SUT dependency. |
| `Capability=Payments` | `SharedKernel.Testing.TestTraitNames.CapabilityName` owns the key. `ViajantesTurismo.Admin.Testing.AdminTestTraitValues` owns the Admin payment label used by the unit and hosted suites. |
| `Category=contract`, `Category=drift-guard`, `Category=snapshot`, `Surface=openapi` | `SharedKernel.Testing.TestTraitValues` owns these repository-neutral values. The typed Contract scope and DriftGuard focus emit separate keys. |
| `Category=database-integration` | `SharedKernel.IntegrationTesting.IntegrationTestTraitValues` owns the value used by the Admin and Catalog hosted suites with `TestTraitNames.CategoryName`. The typed Integration scope emits a separate key. |
| `Capability=integration-event-transport`, `Category=integration-event-transport` | `SharedKernel.Testing.TestTraitValues.IntegrationEventTransport` owns the value used by unit, contract, and hosted tests. `TestTraitNames` supplies the capability and category keys. |

Area and surface remain different dimensions. For example, the Catalog area and Catalog surface each describe
the dimension on which a test is selected. Application-specific values that serve only one bounded context
stay with that context's test support.

## Production values

| Value or family | Owner and use |
| --- | --- |
| API `robots.txt` body | `ViajantesTurismo.ServiceDefaults.ApiCrawlerPolicy` gives Admin and Catalog one application-owned crawler policy. The web hosts have their own policies. |
| Public content `culture` and legacy `language` query keys | `ViajantesTurismo.Catalog.Contracts.Http.PublicContentCultureQuery` owns the keys used by the Catalog client, cache normalization, and Public Web redirects. |
| `Keycloak` token exchange selector | `ViajantesTurismo.Resources.TokenExchangeProviderNames` owns the configuration value set by AppHost and validated by Management Web. |
| Finalized HTML artifact content type | The Admin HTTP contract `DocumentArtifactResponse` owns the media type and its UTF-8 wire form. The client and both download endpoints reference that contract. |
| Branding routes namespace string | `ProductionApiHttpAnalyzer.BrandingRoutesNamespace` identifies the Branding route symbols inspected by that analyzer. The literal namespace is part of analyzer behavior. |

The Branding and Catalog upload `Content` fields belong to different validation contracts. The customer mobile
field in domain validation and the CSV import column are independent. Branding's Catalog URL version and the
Management Web location normalizer also have separate owners.

## Enforcement

The focused architecture guard checks the migrated production consumers. The application naming audit found one
redundant product qualifier in a constant name. Both repositories' `AGENTS.md` files direct contributors to omit
repository names from constants, types, and members when their namespace identifies the owner.

SharedKernel owns its producer naming instruction and API surface. Shared trait values follow the
[candidate and compatibility workflow](sharedkernel/development.md).
