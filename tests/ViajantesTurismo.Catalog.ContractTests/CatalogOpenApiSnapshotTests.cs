using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>
/// Verifies that generated Catalog OpenAPI documents match the committed canonical snapshots.
/// </summary>
public sealed class CatalogOpenApiSnapshotTests
{
    [Theory]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SnapshotCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    [InlineData("catalog")]
    [InlineData("public-catalog")]
    public void Generated_catalog_OpenApi_document_matches_the_canonical_snapshot(string boundaryName)
    {
        // Arrange
        var snapshots = CatalogOpenApiSnapshots.CreateSnapshotSet();

        // Act
        var canonicalSnapshot = snapshots.GetCanonicalSnapshot(boundaryName);
        var generatedArtifact = snapshots.GetGeneratedArtifact(boundaryName);
        var snapshotsMatch = JsonSnapshotArtifactSet.Equals(canonicalSnapshot, generatedArtifact);

        // Assert
        snapshotsMatch.ShouldBeTrue();
    }
}
