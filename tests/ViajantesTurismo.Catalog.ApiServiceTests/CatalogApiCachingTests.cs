using System.Net.Http.Json;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiCachingTests
{
    [Theory]
    [InlineData("/public/catalog/tours")]
    [InlineData("/public/catalog/tours/camino-norte")]
    [InlineData("/public/catalog/content/home.hero?culture=en-US")]
    [InlineData("/public/catalog/theme")]
    public async Task Public_catalog_reads_emit_cache_metadata(string path)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(Guid.CreateVersion7(), "Camino Norte", "camino-norte"),
            cancellationToken);
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Welcome"), cancellationToken);

        await using var factory = CatalogApiTestHost.Create(tourStore, contentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.ToString().ShouldContain("public", StringComparison.Ordinal);
        cacheControl.ToString().ShouldContain("max-age=60", StringComparison.Ordinal);
        var etag = response.Headers.ETag.ShouldNotBeNull();
        etag.ToString().ShouldContain("W/\"", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Management_catalog_writes_are_not_cacheable()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new PublicThemeSettingsDto
        {
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-theme", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.ToString().ShouldContain("no-store", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Catalog_tour_update_invalidates_cached_public_tour_reads()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(tourId, "Original tour", "camino-norte"),
            cancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var firstTour = await firstResponse.Content.ReadFromJsonAsync<CatalogTourDto>(cancellationToken);
        _ = await tourStore.UpdatePresentation(
            tourId,
            CatalogApiCachingTestData.CreatePresentationUpdate("Unpublished store-only change", "camino-norte"),
            cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var cachedTour = await cachedResponse.Content.ReadFromJsonAsync<CatalogTourDto>(cancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            new Uri($"/catalog/tours/{tourId}/presentation", UriKind.Relative),
            CatalogApiCachingTestData.CreatePresentationRequest("Invalidated tour", "camino-norte"),
            cancellationToken);
        using var refreshedResponse = await client.GetAsync(new Uri("/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var refreshedTour = await refreshedResponse.Content.ReadFromJsonAsync<CatalogTourDto>(cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstTour.ShouldNotBeNull();
        firstTour.Title.ShouldBe("Original tour");
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedTour.ShouldNotBeNull();
        cachedTour.Title.ShouldBe("Original tour");
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedTour.ShouldNotBeNull();
        refreshedTour.Title.ShouldBe("Invalidated tour");
    }

    [Fact]
    public async Task Public_content_update_invalidates_cached_public_content_reads()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Original content"), cancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var firstContent = await firstResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Store-only content"), cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var cachedContent = await cachedResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            CatalogApiCachingTestData.CreateContentRequest("Invalidated content"),
            cancellationToken);
        using var refreshedResponse = await client.GetAsync(new Uri("/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var refreshedContent = await refreshedResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstContent.ShouldNotBeNull();
        firstContent.Title.ShouldBe("Original content");
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedContent.ShouldNotBeNull();
        cachedContent.Title.ShouldBe("Original content");
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedContent.ShouldNotBeNull();
        refreshedContent.Title.ShouldBe("Invalidated content");
    }

    [Fact]
    public async Task Public_content_cache_uses_canonical_culture_for_language_alias()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Original content"), cancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/public/catalog/content/home.hero?language=EN", UriKind.Relative), cancellationToken);
        var firstContent = await firstResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Store-only content"), cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var cachedContent = await cachedResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstContent.ShouldNotBeNull();
        firstContent.Title.ShouldBe("Original content");
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedContent.ShouldNotBeNull();
        cachedContent.Title.ShouldBe("Original content");
    }
}
