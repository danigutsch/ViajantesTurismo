using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>
/// Verifies the public multipart contract for Catalog tour image uploads.
/// </summary>
public sealed class CatalogOpenApiUploadContractTests
{
    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
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
        var parameters = uploadOperation["parameters"].ShouldNotBeNull().AsArray()
            .Select(parameter => parameter.ShouldNotBeNull().AsObject())
            .ToArray();
        var multipartSchema = content["multipart/form-data"].ShouldNotBeNull().AsObject()["schema"].ShouldNotBeNull().AsObject();
        var properties = multipartSchema["properties"].ShouldNotBeNull().AsObject();
        var requiredFields = multipartSchema["required"].ShouldNotBeNull().AsArray()
            .Select(item => item.ShouldNotBeNull().GetValue<string>())
            .ToArray();

        // Act
        var mediaTypes = content.Select(item => item.Key).ToArray();
        var pathParameters = parameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "path")
            .ToArray();
        var queryParameters = parameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "query")
            .ToArray();
        var routeParameter = pathParameters.ShouldHaveSingleItem();

        // Assert
        requestBody["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        mediaTypes.ShouldHaveSingleItem().ShouldBe("multipart/form-data");
        routeParameter["name"].ShouldNotBeNull().GetValue<string>().ShouldBe("id");
        routeParameter["in"].ShouldNotBeNull().GetValue<string>().ShouldBe("path");
        routeParameter["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        queryParameters.ShouldBeEmpty();
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
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Public_content_uses_required_route_identity_and_optional_localization_query_parameters()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("public-catalog").AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var contentPath = paths["/api/v1/public/catalog/content/{key}"].ShouldNotBeNull().AsObject();
        var operation = contentPath["get"].ShouldNotBeNull().AsObject();
        var parameters = operation["parameters"].ShouldNotBeNull().AsArray()
            .Select(parameter => parameter.ShouldNotBeNull().AsObject())
            .ToArray();

        // Act
        var keyParameter = parameters.ShouldHaveSingleItem(parameter => parameter["name"]?.GetValue<string>() == "key");
        var localizationParameters = parameters
            .Where(parameter => parameter["in"]?.GetValue<string>() == "query")
            .ToArray();
        var localizationParameterNames = localizationParameters
            .Select(parameter => parameter["name"].ShouldNotBeNull().GetValue<string>())
            .ToArray();

        // Assert
        keyParameter["in"].ShouldNotBeNull().GetValue<string>().ShouldBe("path");
        keyParameter["required"].ShouldNotBeNull().GetValue<bool>().ShouldBeTrue();
        localizationParameterNames.Length.ShouldBe(2);
        localizationParameterNames.ShouldContain("language");
        localizationParameterNames.ShouldContain("culture");
        foreach (var parameter in localizationParameters)
        {
            (parameter["required"]?.GetValue<bool>() ?? false).ShouldBeFalse();
        }
    }

    [Fact]
    [Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Catalog_mutations_document_validation_problem_responses()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("catalog").AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        (string Path, string Method, string SuccessStatus, string[] ExpectedStatuses)[] mutationOperations =
        [
            ("/api/v1/catalog/tours/{id}/presentation", "put", "200", ["200", "202", "400", "401", "403", "404", "409"]),
            ("/api/v1/catalog/tours/{id}/publish", "post", "204", ["202", "204", "400", "401", "403", "404", "409"]),
            ("/api/v1/catalog/tours/{id}/unpublish", "post", "204", ["202", "204", "400", "401", "403", "404", "409"]),
            ("/api/v1/catalog/tours/{id}/images", "post", "201", ["201", "400", "401", "403"]),
            ("/api/v1/catalog/media/images/{id}/accessibility-draft", "post", "200", ["200", "400", "401", "403"]),
            ("/api/v1/catalog/media/images/{id}/accessibility-review", "put", "200", ["200", "400", "401", "403"]),
            ("/api/v1/catalog/public-content/{key}", "put", "200", ["200", "400", "401", "403"])
        ];

        // Act
        var validationResponses = mutationOperations.Select(operation =>
        {
            var path = paths[operation.Path].ShouldNotBeNull().AsObject();
            var endpoint = path[operation.Method].ShouldNotBeNull().AsObject();
            return (operation.SuccessStatus, operation.ExpectedStatuses, Responses: endpoint["responses"].ShouldNotBeNull().AsObject());
        }).ToArray();

        // Assert
        foreach (var (successStatus, expectedStatuses, responses) in validationResponses)
        {
            responses.ContainsKey(successStatus).ShouldBeTrue();
            var responseStatuses = responses.Select(response => response.Key).Order(StringComparer.Ordinal).ToArray();
            responseStatuses.ShouldBe(expectedStatuses);
            var validationResponse = responses["400"].ShouldNotBeNull().AsObject();
            validationResponse["description"].ShouldNotBeNull().GetValue<string>().ShouldBe("Bad Request");
            var content = validationResponse["content"].ShouldNotBeNull().AsObject();
            var problem = content["application/problem+json"].ShouldNotBeNull().AsObject();
            var schema = problem["schema"].ShouldNotBeNull().AsObject();
            schema["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/HttpValidationProblemDetails");
        }
    }
}
