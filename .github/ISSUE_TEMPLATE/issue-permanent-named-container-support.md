# Permanent/named containers for test assemblies

**Summary:**
Make all test assemblies use permanent/named containers (e.g. Postgres, Redis) to enable cross-assembly (multi-project) test fixture support and infrastructure reuse. This will improve efficiency and test stability.

**Motivation:**
- Current tests create ephemeral resources per test run, limiting cross-assembly sharing and increasing setup time.
- Named containers would allow a persistent database/test infra for tests in different assemblies.

**Action items:**
- Research best practices in Aspire/distributed test frameworks for named container infrastructure.
- Update Admin and related test assemblies to use permanent/named containers where safe.
- Ensure local dev/CI usage does not conflict and avoids data bleed or test pollution.

**References:**
- xUnit fixture model
- Aspire resource lifecycle documentation
- Possible tools: Testcontainers, Aspire test orchestration extensions

**Acceptance criteria:**
- Named containers enabled for all accepted shared services across test assemblies
- Documented approach in tests/ASSEMBLY_FIXTURE_GUIDANCE.md
