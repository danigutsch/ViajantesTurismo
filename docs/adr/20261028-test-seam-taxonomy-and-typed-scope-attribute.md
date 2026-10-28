# ADR-044: Test Seam Taxonomy and Typed Scope Attribute

**Status**: Accepted — 2026-10-28

## Context

The repository documented a tagging model (`Scope`, `Surface`, `Area`, `Category`, `Host`) but nothing enforced it. A
scan found 1,990 tests without a valid `Scope`: 1,637 with none and 353 with values outside the documented set
(`api-integration` 236, `web-integration` 70, `Unit` 24, `database-integration` 19, `browser` 4). Ten projects each
carried their own `TestTraits` copy of seam values, and the seam of a test was inferred from its project, folder, or
namespace.

SharedKernel now ships `PandaBear.SharedKernel.Testing.Xunit` with typed attributes (`TestScope`, `TestHost`,
`TestFocus`, `TestPriority`, `TestArea`, `TestSurface`) and the analyzer `SKTEST018` (missing, unknown, or conflicting
scope, with a code fix for a missing scope). The enums live in `PandaBear.Testing`, which has no xUnit dependency.

## Decision

### One required seam: `Scope`

Every test declares exactly one `Scope`. The allowed values, definitions, and tie-breaks are in the
[taxonomy table](../../tests/README.md#scope-taxonomy). The values are unchanged from the documented set; "web UI" does
not get its own value. It stays split into `ui-integration` (components or pages rendered without a browser) and
`system` (a real browser, `Host=browser`).

Out-of-set values map as follows:

| Legacy value           | New `Scope`                         | Notes                                     |
|------------------------|-------------------------------------|-------------------------------------------|
| `api-integration`      | `integration`                       | `Host=test-server` where it applies       |
| `database-integration` | `integration`                       | `Host=aspire`; a real database container  |
| `web-integration`      | `ui-integration` or `integration`   | By class: rendering only, or a test host  |
| `Unit`                 | `unit`                              | Case only                                 |
| `browser`              | `system`                            | `Host=browser`                            |

### Typed attribute over `[Trait]`

Scope is expressed with `[TestScope(TestScope.X)]`, which implements xUnit v3 `ITraitAttribute` and emits the same
`Scope=<value>` trait as `[Trait("Scope", ...)]`. `--filter-trait`, reports, and CI treat both alike. A homogeneous
project uses one `[assembly: TestScope(...)]`; a mixed project tags per class. Method-level tags are the exception.
The literal `[Trait("Scope", ...)]` form stays accepted by the analyzer; constants for seam values are not kept.

References: xUnit v3 [`ITraitAttribute`](https://api.xunit.net/v3/3.0.1/Xunit.v3.ITraitAttribute.html) and
[`TraitAttribute`](https://api.xunit.net/v3/3.0.1/Xunit.TraitAttribute.html).

### Optional traits

| Trait      | Vocabulary                                                                                             |
|------------|--------------------------------------------------------------------------------------------------------|
| `Host`     | `in-process`, `test-server`, `aspire`, `browser`                                                       |
| `Focus`    | `core-behavior`, `domain-rule`, `edge-case`, `regression`, `security`, `drift-guard`, `migration`      |
| `Priority` | `smoke`, `critical`                                                                                    |
| `Area`     | Repository-specific string constants, defined once                                                     |
| `Surface`  | Repository-specific string constants, defined once                                                     |

`Host=in-memory` is renamed `in-process`. The test-server and in-process hosts are different costs, so they stay apart.

### Legacy `Category` values

| `Category`                  | Replacement                      |
|-----------------------------|----------------------------------|
| `regression`, `drift-guard` | `TestFocus`                      |
| `edge-case`                 | `TestFocus(EdgeCase)`            |
| `happy-path`                | `TestFocus(CoreBehavior)`        |
| Others (`endpoint`, ...)    | Stay as `Category` until touched |

Migration happens when a test is touched, not in one sweep. `Category` stays valid in the meantime.

### Enforcement

`SKTEST018` is enabled at `suggestion` while tagging proceeds and moves to `warning` in the change that finishes
tagging. CI lane selection then uses `--filter-trait "Scope=..."` instead of project or namespace names.

## Consequences

### Pros

- Seam is a property of the test, not of its location. Moving a test or aligning namespaces does not change filtering.
- The enum argument cannot be misspelled and the analyzer can validate it statically.
- One definition per seam value; per-project `TestTraits` copies go away.

### Cons

- Mixed projects need per-class tags; a class that mixes seams must be split.
- `Category` and the typed traits coexist until legacy tests are touched.

## Alternatives

- **Plain `[Trait("Scope", ...)]` with constants**: stringly typed, verbose, and the allowed set needs a string option.
- **One attribute class per seam** (`[UnitTest]`): more public API and one analyzer match per type.
- **Keep inferring from project and namespace**: blocks aligning HostedTests namespaces with folders.
