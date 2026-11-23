using System.Text.Json;
using PandaBear.Testing;

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
}
