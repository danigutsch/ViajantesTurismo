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
        var responses = uploadOperation["responses"].ShouldNotBeNull().AsObject();
        var multipartSchema = content["multipart/form-data"].ShouldNotBeNull().AsObject()["schema"].ShouldNotBeNull().AsObject();
        var properties = multipartSchema["properties"].ShouldNotBeNull().AsObject();
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
        properties.ContainsKey("file").ShouldBeTrue();
        properties.ContainsKey("altText").ShouldBeTrue();
        properties.ContainsKey("caption").ShouldBeTrue();
        properties.ContainsKey("attribution").ShouldBeTrue();
        properties.ContainsKey("copyright").ShouldBeTrue();
        responses.ContainsKey("201").ShouldBeTrue();
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Catalog_mutations_document_validation_problem_responses()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("catalog").AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        (string Path, string Method, string SuccessStatus)[] mutationOperations =
        [
            ("/api/v1/catalog/tours/{id}/presentation", "put", "200"),
            ("/api/v1/catalog/tours/{id}/publish", "post", "204"),
            ("/api/v1/catalog/tours/{id}/unpublish", "post", "204"),
            ("/api/v1/catalog/tours/{id}/images", "post", "201"),
            ("/api/v1/catalog/media/images/{id}/accessibility-draft", "post", "200"),
            ("/api/v1/catalog/media/images/{id}/accessibility-review", "put", "200"),
            ("/api/v1/catalog/public-content/{key}", "put", "200")
        ];

        // Act
        var validationResponses = mutationOperations.Select(operation =>
        {
            var path = paths[operation.Path].ShouldNotBeNull().AsObject();
            var endpoint = path[operation.Method].ShouldNotBeNull().AsObject();
            return (operation.SuccessStatus, Responses: endpoint["responses"].ShouldNotBeNull().AsObject());
        }).ToArray();

        // Assert
        foreach (var (successStatus, responses) in validationResponses)
        {
            responses.ContainsKey(successStatus).ShouldBeTrue();
            var validationResponse = responses["400"].ShouldNotBeNull().AsObject();
            validationResponse["description"].ShouldNotBeNull().GetValue<string>().ShouldBe("Bad Request");
            var content = validationResponse["content"].ShouldNotBeNull().AsObject();
            var problem = content["application/problem+json"].ShouldNotBeNull().AsObject();
            var schema = problem["schema"].ShouldNotBeNull().AsObject();
            schema["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/HttpValidationProblemDetails");
        }
    }
}
