# Coverage Ownership

This document defines how coverage should be interpreted for repository quality decisions.

## Principle

Coverage belongs to the project or assembly that owns the behavior. Transitive coverage from a
higher-level application test can reveal execution paths, but it does not prove that the lower-level
library has focused behavior tests.

Examples:

- `SharedKernel.Mediator` behavior should be covered by mediator tests, not only by application
  code that happens to dispatch through mediator.
- API contract shape should be covered by contract tests, not only by full-host integration tests.
- UI rendering behavior should be covered by the owning web test project, not by unrelated system
  tests that pass through the same page.

## Ownership rules

| Source surface | Direct coverage owner | Notes |
| --- | --- | --- |
| Pure SharedKernel libraries | Matching `SharedKernel.*.Tests` project | Prefer fast unit or analyzer tests. |
| Source generators and analyzers | Matching generator/analyzer tests | Cover diagnostics, code fixes, and generated output. |
| Domain model | Domain or unit tests for the bounded context | Assert business rules through public domain behavior. |
| Application services | Unit or behavior tests for the use case | Keep infrastructure faked unless persistence is the behavior. |
| Infrastructure adapters | Focused integration tests | Assert real provider behavior where value exceeds setup cost. |
| API services | API integration and contract tests | Separate runtime behavior from published contract shape. |
| Web UI | Web/component/UI integration tests | System tests remain thin journey coverage. |
| AppHost and orchestration | Architecture tests or smoke tests | Avoid line-coverage targets for declarative wiring. |

## Measurement model

MTP coverage writes one Cobertura file per test project. Use reports to inspect target assemblies by
the test lane that produced them; do not rely on a single merged percentage to decide whether direct
coverage exists.

Useful local command:

```bash
dotnet test --solution ViajantesTurismo.slnx -- --coverage --coverage-output-format cobertura --coverage-output coverage.cobertura.xml --coverage-settings coverage.settings.xml
```

Aggregation is useful for exploration, but threshold decisions should still be made at the source
assembly and owning test-lane level.

## Threshold guidance

- Prefer 100 percent direct coverage for small pure libraries, analyzers, parsers, value objects,
  and deterministic mapping code when every branch represents behavior.
- Use lower documented minimums for integration adapters, external-provider glue, generated code,
  defensive framework paths, or behavior that is better proven by contract or system tests.
- Do not count transitive application coverage toward a package/library readiness threshold unless
  an explicit exception says why direct tests would be lower value.
- Keep generated code, EF migrations, source-generated output, and framework bootstrap glue out of
  ordinary threshold decisions unless the generated artifact is itself the published contract.

## Exceptions

Every exception should state:

1. source assembly or file pattern
2. why direct coverage is not valuable or not reliable
3. which test lane gives the closest useful signal
4. when to revisit the exception

Broad repository-wide coverage percentages should be treated as trend data, not a merge gate, until
per-assembly ownership and threshold reporting are proven in CI.

## Related issues

- #380
- #900
- #411
