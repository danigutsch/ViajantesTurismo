namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks ownership of the Catalog endpoints, projection, and media storage.</summary>
public sealed class CatalogFeatureOwnershipTests
{
    [Theory]
    [InlineData("src/ViajantesTurismo.Catalog.Api/Tours/CatalogTourEndpoints.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Api/Media/CatalogMediaEndpoints.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Api/PublicContent/CatalogPublicContentEndpoints.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Application/Tours/CatalogProjectionRunner.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Application/Media/MediaImageOriginalStoredIntegrationHandler.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Infrastructure/Tours/CatalogProjectionHostedService.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Infrastructure/Tours/EfCatalogTourReadModelStore.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Infrastructure/Media/EfPublicMediaImageStore.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Infrastructure/Media/MediaObjectReconciliationHostedService.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Infrastructure/Media/LocalMediaObjectStore.cs")]
    [InlineData("src/ViajantesTurismo.Catalog.Infrastructure/PublicContent/EfPublicContentStore.cs")]
    public void Workflow_files_live_with_their_features(string relativePath)
    {
        // Arrange
        var expectedPath = Path.Combine(RepositoryRoot.Locate("ViajantesTurismo.slnx"), relativePath);

        // Act
        var exists = File.Exists(expectedPath);

        // Assert
        exists.ShouldBeTrue($"Expected a feature-owned file at {expectedPath}.");
    }
}
