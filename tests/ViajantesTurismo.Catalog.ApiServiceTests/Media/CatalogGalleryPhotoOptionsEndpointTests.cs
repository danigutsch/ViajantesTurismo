using System.Text.Json;
using PandaBear.Testing;

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

    [Fact]
    public async Task Photo_options_reject_limits_above_one_picker_page()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryWriteScenario.Create(published: false, ct);
        await using var host = scenario.CreateHost();
        using var client = host.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"{scenario.EditorPath}/options", UriKind.Relative), new
            {
                Limit = 25
            }, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
