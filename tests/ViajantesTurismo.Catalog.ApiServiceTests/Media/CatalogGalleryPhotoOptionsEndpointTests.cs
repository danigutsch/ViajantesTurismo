using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>Verifies the bounded management photo-option endpoint.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class CatalogGalleryPhotoOptionsEndpointTests
{
    [Fact]
    public async Task Catalog_readers_can_hydrate_only_selected_tour_photos()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        Guid[] includedPhotoIds = [scenario.SecondPhotoId, Guid.CreateVersion7()];
        var payload = new
        {
            Limit = 24,
            IncludedPhotoIds = includedPhotoIds
        };

        // Act
        using var response = await client.PostAsJsonAsync(new Uri($"{scenario.EditorPath}/options", UriKind.Relative), payload, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty("total").GetInt32().ShouldBe(1);
        var result = body.GetProperty("items").EnumerateArray().ShouldHaveSingleItem();
        result.GetProperty("id").GetGuid().ShouldBe(scenario.SecondPhotoId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(CatalogGalleryPhotoOptionsRequest.MaxPageSize + 1)]
    public async Task Photo_options_require_a_limit_within_one_picker_page(int limit)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        var request = new CatalogGalleryPhotoOptionsRequest(Query: null, Limit: limit);

        // Act
        using var response = await client.PostAsJsonAsync(new Uri($"{scenario.EditorPath}/options", UriKind.Relative), request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Photo_options_reject_searches_longer_than_the_picker_bound()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        var request = new CatalogGalleryPhotoOptionsRequest(new string('x', CatalogGalleryPhotoOptionsRequest.MaxQueryLength + 1));

        // Act
        using var response = await client.PostAsJsonAsync(new Uri($"{scenario.EditorPath}/options", UriKind.Relative), request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Photo_options_reject_empty_IDs_and_selections_larger_than_the_album_bound(bool containsEmptyId)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        Guid[] included = containsEmptyId
            ? [Guid.Empty]
            : [.. Enumerable.Range(0, CatalogDomainLimits.MaxGalleryPhotos + 1).Select(_ => Guid.CreateVersion7())];
        var request = new CatalogGalleryPhotoOptionsRequest(Query: null, IncludedPhotoIds: included);

        // Act
        using var response = await client.PostAsJsonAsync(new Uri($"{scenario.EditorPath}/options", UriKind.Relative), request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Guest", HttpStatusCode.Forbidden)]
    [InlineData("Operator", HttpStatusCode.NotFound)]
    public async Task Photo_options_require_catalog_read_permission_before_disclosing_a_missing_tour(string? role, HttpStatusCode status)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CatalogApiTestHost.CreateAnonymous();
        using var client = host.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        var endpoint = new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/gallery-editor/options", UriKind.Relative);
        var request = new CatalogGalleryPhotoOptionsRequest(Query: null);

        // Act
        using var response = await client.PostAsJsonAsync(endpoint, request, ct);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Fact]
    public async Task Photo_options_reject_an_empty_tour_identifier()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CatalogApiTestHost.Create();
        using var client = host.CreateClient();
        var endpoint = new Uri($"/api/v1/catalog/tours/{Guid.Empty}/gallery-editor/options", UriKind.Relative);
        var request = new CatalogGalleryPhotoOptionsRequest(Query: null);

        // Act
        using var response = await client.PostAsJsonAsync(endpoint, request, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
