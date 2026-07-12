using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Fixtures;

[ExcludeFromCodeCoverage]
[SharedKernel.Testing.SerialTestJustification("Baseline-isolation tests reset PostgreSQL public tables and must not overlap with other database-backed integration tests.")]
[CollectionDefinition(IntegrationTestCollections.Serial, DisableParallelization = true)]
public sealed class AspireSerialIntegrationTestLane;
