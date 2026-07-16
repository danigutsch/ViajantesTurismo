using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>
/// Verifies the public multipart contract for Catalog tour image uploads.
/// </summary>
public sealed class CatalogOpenApiUploadContractTests
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Catalog_tour_image_upload_is_multipart_only_and_requires_file_and_alt_text()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("catalog").AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var uploadPath = paths["/api/v1/catalog/tours/{id}/images"].ShouldNotBeNull().AsObject();
        var uploadOperation = uploadPath["post"].ShouldNotBeNull().AsObject();
        var requestBody = uploadOperation["requestBody"].ShouldNotBeNull().AsObject();
        var content = requestBody["content"].ShouldNotBeNull().AsObject();
        var multipartSchema = content["multipart/form-data"].ShouldNotBeNull().AsObject()["schema"].ShouldNotBeNull().AsObject();
        var requiredFields = multipartSchema["required"].ShouldNotBeNull().AsArray()
            .Select(item => item.ShouldNotBeNull().GetValue<string>())
            .ToArray();

        // Act
        var mediaTypes = content.Select(item => item.Key).ToArray();

        // Assert
        mediaTypes.ShouldContain("multipart/form-data");
        mediaTypes.ShouldNotContain("application/x-www-form-urlencoded");
        requiredFields.ShouldContain("file");
        requiredFields.ShouldContain("altText");
    }
}
