# Assembly Fixture Guidance

Assembly fixtures are a sharing mechanism for expensive context. They are not the default answer for Admin test architecture.

## Use an assembly fixture only when

- the whole test assembly intentionally shares one expensive context
- that shared context is safe under the assembly's parallel execution model
- narrower scope choices such as local setup, class fixtures, or collection fixtures would express the lifetime incorrectly

## Do not use an assembly fixture to

- expose generic host plumbing to every test
- hide weak test boundaries
- avoid designing named lifecycle operations
- make a transitional host model look canonical

## Admin direction

- Existing assembly fixtures may remain while the repository is in transition.
- New test infrastructure should justify assembly-wide lifetime explicitly.
- Even under Aspire-managed hosting, fixture scope should be chosen deliberately rather than inherited by habit.

## Provider-service direction

- Share an expensive provider server only when the whole assembly uses it.
- Start each provider server when a test first needs it, behind a gate that starts it once. xUnit creates an assembly
  fixture whenever one selected test exists, so a focused run pays for every server the fixture starts up front; keep
  `InitializeAsync` free of container starts so the run pays only for the servers its tests use.
- Give every parallel test its own mutable namespace, such as a PostgreSQL database or S3 bucket.
- Track exact fixture-issued names and clean only those names with a fixture-owned timeout.
- Do not use fixed persistent container names or fall back to developer or production endpoints.
- Keep collection parallelism enabled; server sharing does not justify shared mutable test data.

Current examples are `SharedKernel.Npgsql.Tests`, `SharedKernel.EventSourcing.Npgsql.Tests`,
`SharedKernel.EntityFrameworkCore.Tests`, and the PostgreSQL seams of `ViajantesTurismo.HostedTests`. Each keeps
the Aspire server assembly-scoped, issues a GUID-owned database to each test instance, and drops only
fixture-owned databases with an independent cleanup timeout. Registries retain database names until a successful
drop and retry any remaining cleanup during assembly teardown. The Entity Framework Core fixture and
`ViajantesTurismo.HostedTests` start their servers on first demand, so database-free focused runs remain
container-free; the hosted assembly does the same for its other five containers (see its
[README](ViajantesTurismo.HostedTests/README.md#demand-driven-infrastructure)).
