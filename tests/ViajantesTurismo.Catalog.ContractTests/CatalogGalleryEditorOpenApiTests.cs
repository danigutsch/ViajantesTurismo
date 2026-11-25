using ViajantesTurismo.Catalog.ContractTests.Infrastructure;

namespace ViajantesTurismo.Catalog.ContractTests;

/// <summary>Verifies the private Catalog gallery editor contract.</summary>
public sealed class CatalogGalleryEditorOpenApiTests
{
    [Fact]
    [Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.DriftGuardCategory)]
    [Trait(SharedKernel.Testing.TestTraitNames.SurfaceName, TestTraits.OpenApiSurface)]
    public void Editor_placement_uses_the_private_management_contract_and_returns_an_outcome_link()
    {
        // Arrange
        const string path = "/api/v1/catalog/tours/{id}/gallery-editor";
        const string optionsPath = path + "/options";
        const string initializePath = path + "/initialize";
        var snapshots = CatalogOpenApiSnapshots.CreateSnapshotSet();
        var management = snapshots.GetCanonicalSnapshot("catalog");
        var managementPaths = management["paths"].ShouldNotBeNull().AsObject();
        var publicPaths = snapshots.GetCanonicalSnapshot("public-catalog")["paths"].ShouldNotBeNull().AsObject();

        // Act
        var operations = managementPaths[path].ShouldNotBeNull().AsObject();
        var readResponses = operations["get"].ShouldNotBeNull()["responses"].ShouldNotBeNull().AsObject();
        var writeResponses = operations["put"].ShouldNotBeNull()["responses"].ShouldNotBeNull().AsObject();
        var acceptedHeaders = writeResponses["202"].ShouldNotBeNull()["headers"].ShouldNotBeNull().AsObject();
        var schemas = management["components"].ShouldNotBeNull()["schemas"].ShouldNotBeNull().AsObject();
        var optionsResponses = managementPaths[optionsPath].ShouldNotBeNull()["post"].ShouldNotBeNull()["responses"].ShouldNotBeNull().AsObject();
        var initializeResponses = managementPaths[initializePath].ShouldNotBeNull()["post"].ShouldNotBeNull()["responses"].ShouldNotBeNull().AsObject();
        var editorFields = schemas["CatalogGalleryEditorStateDto"].ShouldNotBeNull()["properties"].ShouldNotBeNull().AsObject();
        var required = schemas["QueueCatalogGalleryPlacementRequest"].ShouldNotBeNull()["required"].ShouldNotBeNull().AsArray()
            .Select(property => property.ShouldNotBeNull().GetValue<string>());

        // Assert
        operations.Select(operation => operation.Key).Order(StringComparer.Ordinal).ToArray().ShouldBe(["get", "put"]);
        publicPaths.ContainsKey(path).ShouldBeFalse();
        publicPaths.ContainsKey(optionsPath).ShouldBeFalse();
        publicPaths.ContainsKey(initializePath).ShouldBeFalse();
        optionsResponses.ContainsKey("200").ShouldBeTrue();
        optionsResponses.ContainsKey("400").ShouldBeTrue();
        optionsResponses.ContainsKey("404").ShouldBeTrue();
        initializeResponses.ContainsKey("204").ShouldBeTrue();
        initializeResponses.ContainsKey("403").ShouldBeTrue();
        initializeResponses["202"].ShouldNotBeNull()["headers"].ShouldNotBeNull().AsObject().ContainsKey("Location").ShouldBeTrue();
        editorFields.ContainsKey("canEditGallery").ShouldBeTrue();
        editorFields.ContainsKey("maximumPhotoCount").ShouldBeTrue();
        readResponses.ContainsKey("200").ShouldBeTrue();
        readResponses.ContainsKey("400").ShouldBeTrue();
        readResponses.ContainsKey("404").ShouldBeTrue();
        acceptedHeaders.ContainsKey("Location").ShouldBeTrue();
        writeResponses.ContainsKey("400").ShouldBeTrue();
        writeResponses.ContainsKey("403").ShouldBeTrue();
        writeResponses.ContainsKey("404").ShouldBeTrue();
        writeResponses.ContainsKey("409").ShouldBeTrue();
        required.Order(StringComparer.Ordinal).ToArray().ShouldBe(["acceptedPlacementSequence", "photoIds", "streamVersion"]);
    }
}
