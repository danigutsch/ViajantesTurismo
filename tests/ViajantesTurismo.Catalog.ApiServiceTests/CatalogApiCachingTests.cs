using System.Net.Http.Json;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicTheme;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiCachingTests
{
    [Theory]
    [InlineData("/api/v1/public/catalog/tours")]
    [InlineData("/api/v1/public/catalog/tours/camino-norte")]
    [InlineData("/api/v1/public/catalog/content/home.hero?culture=en-US")]
    [InlineData("/api/v1/public/catalog/theme")]
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
        cacheControl.Public.ShouldBeTrue();
        cacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(60));
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
            new Uri("/api/v1/catalog/public-theme", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
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
        using var firstResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var firstTour = await firstResponse.Content.ReadFromJsonAsync<CatalogTourDto>(cancellationToken);
        _ = await tourStore.UpdatePresentation(
            tourId,
            CatalogApiCachingTestData.CreatePresentationUpdate("Unpublished store-only change", "camino-norte"),
            cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var cachedTour = await cachedResponse.Content.ReadFromJsonAsync<CatalogTourDto>(cancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            CatalogApiCachingTestData.CreatePresentationRequest("Invalidated tour", "camino-norte"),
            cancellationToken);
        using var refreshedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
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
        using var firstResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var firstContent = await firstResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Store-only content"), cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var cachedContent = await cachedResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            CatalogApiCachingTestData.CreateContentRequest("Invalidated content"),
            cancellationToken);
        using var refreshedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
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
    public async Task Public_theme_update_invalidates_cached_public_theme_reads()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var themeStore = new TestPublicThemeSettingsStore();
        await themeStore.SaveTheme(
            PublicThemeSettings.Create("#112233", "#445566", "#FFFFFF", "#000000", "Inter", "Verdana").Value,
            cancellationToken);
        await using var factory = CatalogApiTestHost.Create(themeStore);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/theme", UriKind.Relative), cancellationToken);
        var firstTheme = await firstResponse.Content.ReadFromJsonAsync<PublicThemeSettingsDto>(cancellationToken);
        await themeStore.SaveTheme(
            PublicThemeSettings.Create("#334455", "#445566", "#FFFFFF", "#000000", "Inter", "Verdana").Value,
            cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/theme", UriKind.Relative), cancellationToken);
        var cachedTheme = await cachedResponse.Content.ReadFromJsonAsync<PublicThemeSettingsDto>(cancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            new Uri("/api/v1/catalog/public-theme", UriKind.Relative),
            new PublicThemeSettingsDto
            {
                PrimaryColor = "#556677",
                AccentColor = "#445566",
                BackgroundColor = "#FFFFFF",
                TextColor = "#000000",
                HeadingFontFamily = "Inter",
                BodyFontFamily = "Verdana"
            },
            cancellationToken);
        using var refreshedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/theme", UriKind.Relative), cancellationToken);
        var refreshedTheme = await refreshedResponse.Content.ReadFromJsonAsync<PublicThemeSettingsDto>(cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstTheme.ShouldNotBeNull();
        firstTheme.PrimaryColor.ShouldBe("#112233");
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedTheme.ShouldNotBeNull();
        cachedTheme.PrimaryColor.ShouldBe("#112233");
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedTheme.ShouldNotBeNull();
        refreshedTheme.PrimaryColor.ShouldBe("#556677");
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
        using var firstResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=ZZ&language=EN", UriKind.Relative), cancellationToken);
        var firstContent = await firstResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Store-only content"), cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var cachedContent = await cachedResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstContent.ShouldNotBeNull();
        firstContent.Title.ShouldBe("Original content");
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedContent.ShouldNotBeNull();
        cachedContent.Title.ShouldBe("Original content");
    }

    [Fact]
    public async Task Public_content_cache_preserves_invalid_culture_validation()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Original content"), cancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=ZZ", UriKind.Relative), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
    }

    [Fact]
    public async Task Public_content_empty_culture_uses_default_language()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Default content"), cancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=", UriKind.Relative), cancellationToken);
        var content = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotBeNull();
        content.Title.ShouldBe("Default content");
    }

    [Fact]
    public async Task Blank_public_content_keys_are_not_cacheable()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/public/catalog/content/%20?culture=en-US", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
    }

    [Theory]
    [InlineData("/api/v1/public/catalog/tours/missing-tour")]
    [InlineData("/api/v1/public/catalog/content/missing-content?culture=en-US")]
    public async Task Missing_public_catalog_reads_are_not_cacheable(string path)
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Published content"), cancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
    }
}
