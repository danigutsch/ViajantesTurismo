using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>Guards the hypermedia schema for Catalog resource responses.</summary>
[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
[Trait(PandaBear.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
public sealed class CatalogHypermediaOpenApiTests
{
    [Theory]
    [InlineData("public-catalog", "TourSummaryDto")]
    [InlineData("public-catalog", "TourDetailsDto")]
    [InlineData("public-catalog", "PublicContentVariantDto")]
    [InlineData("catalog", "CatalogTourDto")]
    [InlineData("catalog", "PublicContentDto")]
    [InlineData("catalog", "CatalogMediaImageDto")]
    [InlineData("v1", "TourSummaryDto")]
    [InlineData("v1", "CatalogTourDto")]
    public void Catalog_resource_schemas_declare_shared_links(string boundary, string resource)
    {
        // Arrange
        var document = CatalogOpenApiSnapshots.CreateSnapshotSet().GetCanonicalSnapshot(boundary).AsObject();
        var schemas = document["components"].ShouldNotBeNull().AsObject()["schemas"].ShouldNotBeNull().AsObject();

        // Act
        var resourceProperties = schemas[resource].ShouldNotBeNull().AsObject()["properties"].ShouldNotBeNull().AsObject();
        var links = resourceProperties["links"].ShouldNotBeNull().AsObject();
        var link = schemas["HypermediaLink"].ShouldNotBeNull().AsObject()["properties"].ShouldNotBeNull().AsObject();
        var linkPropertyNames = link.Select(property => property.Key).Order(StringComparer.Ordinal).ToArray();

        // Assert
        links["items"].ShouldNotBeNull().AsObject()["$ref"].ShouldNotBeNull().GetValue<string>().ShouldBe("#/components/schemas/HypermediaLink");
        linkPropertyNames.ShouldBe(["href", "method", "rel"]);
        foreach (var property in link)
        {
            property.Value.ShouldNotBeNull().AsObject()["type"].ShouldNotBeNull().GetValue<string>().ShouldBe("string");
        }
    }
}
