using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>
/// Verifies that the streamed media delivery operations describe their binary responses.
/// </summary>
public sealed class CatalogOpenApiMediaDeliveryContractTests
{
    [Theory]
    [InlineData("public-catalog", "/api/v1/public/catalog/media/{id}/{width}/{format}")]
    [InlineData("catalog", "/api/v1/catalog/media/images/{id}/preview/{width}/{format}")]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Media_delivery_declares_image_media_types_and_not_found(string boundary, string path)
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot(boundary).AsObject();
        var operation = document["paths"].ShouldNotBeNull().AsObject()[path].ShouldNotBeNull().AsObject()["get"].ShouldNotBeNull().AsObject();
        var responses = operation["responses"].ShouldNotBeNull().AsObject();

        // Act
        var okMediaTypes = responses["200"].ShouldNotBeNull().AsObject()["content"].ShouldNotBeNull().AsObject()
            .Select(item => item.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        // Assert
        okMediaTypes.ShouldBe(["image/avif", "image/jpeg", "image/png", "image/webp"]);
        responses.ContainsKey("404").ShouldBeTrue();
    }
}
