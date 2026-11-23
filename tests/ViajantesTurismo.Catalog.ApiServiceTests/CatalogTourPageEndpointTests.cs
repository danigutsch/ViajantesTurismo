using System.Text.Json;
using ViajantesTurismo.Catalog.Application.Tours;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogTourPageEndpointTests
{
    [Fact]
    public async Task Large_catalog_returns_one_ordered_page_of_lightweight_rows()
    {
        // Arrange
        const int tourCount = 800;
        var ct = TestContext.Current.CancellationToken;
        var tourStore = new TestCatalogTourReadModelStore();
        for (var index = 0; index < tourCount; index++)
        {
            await tourStore.UpsertDraft(new CatalogTourDraftReadModel(
                Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}"),
                Guid.CreateVersion7(),
                $"TOUR-{index:D3}",
                "Same title",
                $"tour-{index:D3}",
                false,
                1,
                DateTimeOffset.UnixEpoch)
            {
                Description = new string('x', 2048),
            }, ct);
        }

        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/catalog/tours/page?page=2&sort=title", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(ct);
        using var document = JsonDocument.Parse(body);
        var page = document.RootElement;
        page.GetProperty("total").GetInt32().ShouldBe(tourCount);
        var rows = page.GetProperty("items").EnumerateArray().ToArray();
        rows.Length.ShouldBe(10);
        var identifiers = rows.Select(row => row.GetProperty("identifier").GetString()).ToArray();
        identifiers.ShouldBe([.. Enumerable.Range(10, 10).Select(index => $"TOUR-{index:D3}")]);
        var includesDetail = rows.Any(row => row.TryGetProperty("description", out _) || row.TryGetProperty("images", out _));
        includesDetail.ShouldBeFalse();
    }

    [Fact]
    public async Task Tour_page_requires_catalog_read_access()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/catalog/tours/page", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Unsupported_sort_is_rejected()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/catalog/tours/page?sort=999", UriKind.Relative), ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
