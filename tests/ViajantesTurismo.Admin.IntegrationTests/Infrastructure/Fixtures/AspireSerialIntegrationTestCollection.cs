using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Fixtures;

[ExcludeFromCodeCoverage]
[CollectionDefinition(IntegrationTestCollections.Serial, DisableParallelization = true)]
public sealed class AspireSerialIntegrationTestLane : ICollectionFixture<AspireSerialIntegrationTestFixture>;
