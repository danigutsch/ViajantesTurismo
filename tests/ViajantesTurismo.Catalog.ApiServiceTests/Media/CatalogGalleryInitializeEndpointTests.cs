using System.Text.Json;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>Verifies explicit, permission-checked initialization of the authored legacy gallery.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.SecurityCategory)]
public sealed class CatalogGalleryInitializeEndpointTests
{
    [Fact]
    public async Task Draft_initialization_preserves_the_legacy_order_and_cover_when_retried()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        var endpoint = new Uri($"{scenario.EditorPath}/initialize", UriKind.Relative);

        // Act
        using var first = await client.PostAsync(endpoint, content: null, ct);
        using var repeated = await client.PostAsync(endpoint, content: null, ct);
        var state = await client.GetFromJsonAsync<JsonElement>(scenario.EditorPath, ct);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        repeated.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        first.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        state.GetProperty("streamVersion").GetInt64().ShouldBe(2L);
        state.GetProperty("projectedStreamVersion").GetInt64().ShouldBe(2L);
        state.GetProperty("photoIds").EnumerateArray().Select(photo => photo.GetGuid())
            .ShouldBe([scenario.FirstPhotoId, scenario.SecondPhotoId]);
        state.GetProperty("coverId").GetGuid().ShouldBe(scenario.FirstPhotoId);
    }

    [Fact]
    public async Task Published_initialization_requires_publication_permission()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: true, ct);
        await using var host = scenario.CreateHost();
        using var tourOperator = host.CreateClient();
        using var administrator = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(tourOperator, "Operator");
        var endpoint = new Uri($"{scenario.EditorPath}/initialize", UriKind.Relative);

        // Act
        using var forbidden = await tourOperator.PostAsync(endpoint, content: null, ct);
        var afterDenied = await administrator.GetFromJsonAsync<JsonElement>(scenario.EditorPath, ct);
        using var allowed = await administrator.PostAsync(endpoint, content: null, ct);

        // Assert
        forbidden.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        afterDenied.GetProperty("photoIds").ValueKind.ShouldBe(JsonValueKind.Null);
        allowed.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Committed_initialization_returns_the_uncached_editor_until_its_projection_catches_up()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        scenario.FailNextGalleryProjection();
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");
        var endpoint = new Uri($"{scenario.EditorPath}/initialize", UriKind.Relative);

        // Act
        using var committed = await client.PostAsync(endpoint, content: null, ct);

        // Assert
        committed.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        committed.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var location = committed.Headers.Location.ShouldNotBeNull();
        location.IsAbsoluteUri.ShouldBeFalse();
        location.ShouldBe(scenario.EditorPath);

        // Act
        using var pendingResponse = await client.GetAsync(location, ct);
        var pending = await pendingResponse.Content.ReadFromJsonAsync<CatalogGalleryEditorStateDto>(ct);
        using var pendingRetry = await client.PostAsync(endpoint, content: null, ct);

        // Assert
        pendingResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        pendingResponse.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        pending.ShouldNotBeNull().StreamVersion.ShouldBe(2);
        pending.ProjectedStreamVersion.ShouldBe(1);
        pending.PhotoIds.ShouldNotBeNull().ShouldBe([scenario.FirstPhotoId, scenario.SecondPhotoId]);
        pending.CoverId.ShouldBe(scenario.FirstPhotoId);
        pendingRetry.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        pendingRetry.Headers.Location.ShouldBe(scenario.EditorPath);

        // Act
        await scenario.ProjectCommittedGallery(ct);
        using var projectedRetry = await client.PostAsync(endpoint, content: null, ct);
        var projected = await client.GetFromJsonAsync<CatalogGalleryEditorStateDto>(scenario.EditorPath, ct);

        // Assert
        projectedRetry.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        projected.ShouldNotBeNull().StreamVersion.ShouldBe(2);
        projected.ProjectedStreamVersion.ShouldBe(2);
        projected.PhotoIds.ShouldNotBeNull().ShouldBe([scenario.FirstPhotoId, scenario.SecondPhotoId]);
        projected.CoverId.ShouldBe(scenario.FirstPhotoId);
    }

    [Fact]
    public async Task Initialization_distinguishes_an_empty_tour_identifier_from_a_missing_tour()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var host = CatalogApiTestHost.Create();
        using var client = host.CreateClient();
        var empty = new Uri($"/api/v1/catalog/tours/{Guid.Empty}/gallery-editor/initialize", UriKind.Relative);
        var missing = new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/gallery-editor/initialize", UriKind.Relative);

        // Act
        using var emptyResponse = await client.PostAsync(empty, content: null, ct);
        using var missingResponse = await client.PostAsync(missing, content: null, ct);

        // Assert
        emptyResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        missingResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
