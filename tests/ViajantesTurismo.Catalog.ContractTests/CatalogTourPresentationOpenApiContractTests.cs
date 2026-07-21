using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>
/// Verifies the public and management HTTP contracts for Catalog tour presentation.
/// </summary>
public sealed class CatalogTourPresentationOpenApiContractTests
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Public_tour_contracts_separate_summary_and_details_from_management_fields()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("public-catalog").AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var schemas = document["components"].ShouldNotBeNull().AsObject()["schemas"].ShouldNotBeNull().AsObject();
        var listSchema = paths["/api/v1/public/catalog/tours"].ShouldNotBeNull().AsObject()
            ["get"].ShouldNotBeNull().AsObject()
            ["responses"].ShouldNotBeNull().AsObject()
            ["200"].ShouldNotBeNull().AsObject()
            ["content"].ShouldNotBeNull().AsObject()
            ["application/json"].ShouldNotBeNull().AsObject()
            ["schema"].ShouldNotBeNull().AsObject();
        var detailsSchema = paths["/api/v1/public/catalog/tours/{slug}"].ShouldNotBeNull().AsObject()
            ["get"].ShouldNotBeNull().AsObject()
            ["responses"].ShouldNotBeNull().AsObject()
            ["200"].ShouldNotBeNull().AsObject()
            ["content"].ShouldNotBeNull().AsObject()
            ["application/json"].ShouldNotBeNull().AsObject()
            ["schema"].ShouldNotBeNull().AsObject();

        // Act
        var summaryReference = listSchema["items"].ShouldNotBeNull().AsObject()["$ref"].ShouldNotBeNull().GetValue<string>();
        var detailsReference = detailsSchema["$ref"].ShouldNotBeNull().GetValue<string>();
        var summaryProperties = schemas["TourSummaryDto"].ShouldNotBeNull().AsObject()["properties"].ShouldNotBeNull().AsObject();
        var detailsProperties = schemas["TourDetailsDto"].ShouldNotBeNull().AsObject()["properties"].ShouldNotBeNull().AsObject();

        // Assert
        summaryReference.ShouldBe("#/components/schemas/TourSummaryDto");
        detailsReference.ShouldBe("#/components/schemas/TourDetailsDto");
        summaryProperties.ContainsKey("summary").ShouldBeTrue();
        detailsProperties.ContainsKey("description").ShouldBeTrue();
        detailsProperties.ContainsKey("itinerary").ShouldBeTrue();
        detailsProperties.ContainsKey("seoTitle").ShouldBeTrue();
        detailsProperties.ContainsKey("seoDescription").ShouldBeTrue();
        foreach (var managementProperty in new[] { "id", "adminTourId", "identifier", "isPublished", "version" })
        {
            summaryProperties.ContainsKey(managementProperty).ShouldBeFalse();
            detailsProperties.ContainsKey(managementProperty).ShouldBeFalse();
        }
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Management_tour_mutations_document_not_found_and_conflict_responses()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("catalog").AsObject();
        var paths = document["paths"].ShouldNotBeNull().AsObject();
        var mutationPaths = new[]
        {
            "/api/v1/catalog/tours/{id}/presentation",
            "/api/v1/catalog/tours/{id}/publish",
            "/api/v1/catalog/tours/{id}/unpublish"
        };

        // Act
        var responses = mutationPaths
            .Select(path => paths[path].ShouldNotBeNull().AsObject()
                .Single().Value.ShouldNotBeNull().AsObject()
                ["responses"].ShouldNotBeNull().AsObject())
            .ToArray();

        // Assert
        foreach (var response in responses)
        {
            response.ContainsKey("202").ShouldBeTrue();
            response.ContainsKey("404").ShouldBeTrue();
            response.ContainsKey("409").ShouldBeTrue();
            response["202"].ShouldNotBeNull().AsObject()
                ["headers"].ShouldNotBeNull().AsObject()
                ["Location"].ShouldNotBeNull();
        }
    }

    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Management_tour_contract_requires_a_positive_version()
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("catalog").AsObject();
        var schema = document["components"].ShouldNotBeNull().AsObject()
            ["schemas"].ShouldNotBeNull().AsObject()
            ["CatalogTourDto"].ShouldNotBeNull().AsObject();

        // Act
        var required = schema["required"].ShouldNotBeNull().AsArray()
            .Select(item => item.ShouldNotBeNull().GetValue<string>())
            .ToArray();
        var version = schema["properties"].ShouldNotBeNull().AsObject()
            ["version"].ShouldNotBeNull().AsObject();

        // Assert
        required.ShouldContain("version");
        version["minimum"].ShouldNotBeNull().GetValue<long>().ShouldBe(1);
    }

    [Theory]
    [InlineData("CatalogTourPublicationRequest")]
    [InlineData("UpsertCatalogTourPresentationRequest")]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Management_tour_mutation_requests_require_a_positive_expected_version(string schemaName)
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot("catalog").AsObject();
        var schema = document["components"].ShouldNotBeNull().AsObject()
            ["schemas"].ShouldNotBeNull().AsObject()
            [schemaName].ShouldNotBeNull().AsObject();

        // Act
        var required = schema["required"].ShouldNotBeNull().AsArray()
            .Select(item => item.ShouldNotBeNull().GetValue<string>())
            .ToArray();
        var expectedVersion = schema["properties"].ShouldNotBeNull().AsObject()
            ["expectedVersion"].ShouldNotBeNull().AsObject();

        // Assert
        required.ShouldContain("expectedVersion");
        expectedVersion["minimum"].ShouldNotBeNull().GetValue<long>().ShouldBe(1);
    }
}
