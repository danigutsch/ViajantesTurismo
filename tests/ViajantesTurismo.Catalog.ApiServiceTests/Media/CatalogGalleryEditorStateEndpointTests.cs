using System.Text.Json;
using PandaBear.Testing;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>Verifies the private editor snapshot contract and request boundaries.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class CatalogGalleryEditorStateEndpointTests
{
    [Fact]
    public async Task A_catalog_reader_can_load_private_editor_revisions_without_caching()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(
            tourId, Guid.CreateVersion7(), "andes", "Andes", "andes", false, 1, TimeProvider.System.GetUtcNow()), ct);
        await using var factory = CatalogApiTestHost.Create(tours, new TestPublicContentStore());
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/catalog/tours/{tourId}/gallery-editor", UriKind.Relative), ct);
        using var missing = await client.GetAsync(new Uri($"/api/v1/catalog/tours/{tourId}/gallery-editor?placementSequence=1", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty("streamVersion").GetInt64().ShouldBe(1);
        body.GetProperty("projectedStreamVersion").GetInt64().ShouldBe(1);
        body.GetProperty("acceptedPlacementSequence").GetInt64().ShouldBe(0);
        body.GetProperty("resolvedPlacementSequence").GetInt64().ShouldBe(0);
        body.GetProperty("canEditGallery").GetBoolean().ShouldBeTrue();
        body.GetProperty("maximumPhotoCount").GetInt32().ShouldBe(500);
        body.GetProperty("photoIds").ValueKind.ShouldBe(JsonValueKind.Null);
        body.GetProperty("outcome").ValueKind.ShouldBe(JsonValueKind.Null);
        missing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Published_editor_reports_current_gallery_permission_for_each_role()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: true, ct);
        await using var factory = scenario.CreateHost();
        using var administrator = factory.CreateClient();
        using var tourOperator = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(tourOperator, "Operator");

        // Act
        var asOperator = await tourOperator.GetFromJsonAsync<JsonElement>(scenario.EditorPath, ct);
        var asAdministrator = await administrator.GetFromJsonAsync<JsonElement>(scenario.EditorPath, ct);

        // Assert
        asOperator.GetProperty("canEditGallery").GetBoolean().ShouldBeFalse();
        asAdministrator.GetProperty("canEditGallery").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task Editor_state_requires_catalog_authentication()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        var route = new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/gallery-editor", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Editor_state_rejects_nonpositive_outcome_sequences()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var route = new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/gallery-editor?placementSequence=0", UriKind.Relative);

        // Act
        using var response = await client.GetAsync(route, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
