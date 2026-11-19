using ViajantesTurismo.ArchitectureTests.Repository;

namespace ViajantesTurismo.ArchitectureTests.Conventions;

/// <summary>Checks the ownership of public browsing components, client registration, and feature tests.</summary>
public sealed class PublicWebFeatureOwnershipTests
{
    [Theory]
    [InlineData("src/ViajantesTurismo.Public.Web/Components/Pages/Home/Home.razor")]
    [InlineData("src/ViajantesTurismo.Public.Web/Components/Pages/Gallery/Gallery.razor")]
    [InlineData("src/ViajantesTurismo.Public.Web/Components/Pages/Tours/Index.razor")]
    [InlineData("src/ViajantesTurismo.Public.Web/Components/Pages/Tours/Details.razor")]
    [InlineData("src/ViajantesTurismo.Public.Web/Components/Shared/TourImageRenditions.cs")]
    [InlineData("src/ViajantesTurismo.Public.Web/Catalog/PublicCatalogClientServiceCollectionExtensions.cs")]
    [InlineData("src/ViajantesTurismo.Public.Web/Branding/PublicBrandingServiceCollectionExtensions.cs")]
    [InlineData("tests/ViajantesTurismo.Public.WebTests/Components/Pages/Home/HomePageTests.cs")]
    [InlineData("tests/ViajantesTurismo.Public.WebTests/Components/Pages/Gallery/GalleryPageTests.cs")]
    [InlineData("tests/ViajantesTurismo.Public.WebTests/Components/Pages/Tours/TourPagesTests.cs")]
    public void Public_workflow_files_live_with_their_features(string relativePath)
    {
        // Arrange
        var expectedPath = Path.Combine(RepositoryRoot.Locate(), relativePath);

        // Act
        var exists = File.Exists(expectedPath);

        // Assert
        exists.ShouldBeTrue($"Expected a feature-owned file at {expectedPath}.");
    }
}
