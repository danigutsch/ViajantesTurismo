using System.Net.Http.Json;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiCachingTests
{
    [Theory]
    [InlineData("/api/v1/public/catalog/tours")]
    [InlineData("/api/v1/public/catalog/tours/camino-norte")]
    [InlineData("/api/v1/public/catalog/content/home.hero?culture=en-US")]
    [InlineData("/API/V1/PUBLIC/CATALOG/TOURS")]
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
        client.DefaultRequestHeaders.Authorization = null;

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), cancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        cacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(60));
        cacheControl.Extensions.Select(extension => extension.Name).ShouldNotContain("stale-while-revalidate");
        response.Headers.Contains("Pragma").ShouldBeFalse();
        response.Headers.NonValidated.Contains("Expires").ShouldBeFalse();
    }

    [Fact]
    public async Task Management_catalog_writes_are_not_cacheable()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        var request = CatalogApiCachingTestData.CreateContentRequest("No-store content");
        CatalogApiTestHost.ConfigureAuthenticatedClient(client);

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
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

    [Theory]
    [InlineData(null, HttpStatusCode.BadRequest)]
    [InlineData("{}", HttpStatusCode.UnsupportedMediaType)]
    public async Task Management_binding_failures_are_not_cacheable(string? body, HttpStatusCode expectedStatusCode)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client);
        using var request = new HttpRequestMessage(
            HttpMethod.Put,
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative));
        if (body is not null)
        {
            request.Content = new StringContent(body);
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(expectedStatusCode);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
    }

    [Fact]
    public async Task Catalog_tour_publication_cycle_invalidates_cached_public_tour_reads()
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
        client.DefaultRequestHeaders.Authorization = null;

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var firstTour = await firstResponse.Content.ReadFromJsonAsync<TourDetailsDto>(cancellationToken);
        _ = await tourStore.UpdatePresentation(
            tourId,
            CatalogApiCachingTestData.CreatePresentationUpdate("Unpublished store-only change", "camino-norte"),
            streamVersion: 4,
            position: 2,
            DateTimeOffset.UtcNow,
            cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var cachedTour = await cachedResponse.Content.ReadFromJsonAsync<TourDetailsDto>(cancellationToken);
        CatalogApiTestHost.ConfigureAuthenticatedClient(client);
        using var unpublishResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/unpublish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 3 },
            cancellationToken);
        using var updateResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            CatalogApiCachingTestData.CreatePresentationRequest("Invalidated tour", "camino-norte") with { ExpectedVersion = 4 },
            cancellationToken);
        using var publishResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/publish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 5 },
            cancellationToken);
        using var refreshedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative), cancellationToken);
        var refreshedTour = await refreshedResponse.Content.ReadFromJsonAsync<TourDetailsDto>(cancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstTour.ShouldNotBeNull();
        firstTour.Title.ShouldBe("Original tour");
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedTour.ShouldNotBeNull();
        cachedTour.Title.ShouldBe("Original tour");
        unpublishResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        refreshedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        refreshedTour.ShouldNotBeNull();
        refreshedTour.Title.ShouldBe("Invalidated tour");
    }

    [Fact]
    public async Task Post_commit_cache_eviction_does_not_capture_request_cancellation()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            CatalogApiCachingTestData.CreatePublishedTour(tourId, "Published tour", "published-tour"),
            TestContext.Current.CancellationToken);
        var cacheStore = new RecordingOutputCacheStore();
        await using var baseFactory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore>(cacheStore))));
        using var client = factory.CreateClient();
        using var requestCancellation = new CancellationTokenSource();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/unpublish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 3 },
            requestCancellation.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        cacheStore.EvictionObserved.ShouldBeTrue();
        cacheStore.EvictionCancellationToken.ShouldBe(CancellationToken.None);
    }

    [Fact]
    public async Task Unpublish_returns_accepted_when_projection_is_pending_after_the_event_commits()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        var tour = CatalogApiCachingTestData.CreatePublishedTour(tourId, "Published tour", "published-tour");
        await tourStore.UpsertDraft(tour, TestContext.Current.CancellationToken);
        tourStore.FailNextPublicationProjection = true;
        var eventStore = new TestEventStore();
        eventStore.SeedTour(tour);
        var cacheStore = new RecordingOutputCacheStore
        {
            EvictionException = new InvalidOperationException("Simulated cache eviction failure.")
        };
        await using var baseFactory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), eventStore);
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore>(cacheStore))));
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/unpublish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 3 },
            TestContext.Current.CancellationToken);
        var persisted = await eventStore.Load(
            CatalogTourStreamIds.FromAdminTourId(tour.AdminTourId),
            afterRevision: null,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.OriginalString.ShouldBe($"/api/v1/catalog/tours/{tourId}");
        persisted.Count.ShouldBe(4);
        var unpublishEnvelope = persisted.OrderBy(envelope => envelope.Revision.Value).Last();
        unpublishEnvelope.Data.ShouldBeOfType<CatalogTourUnpublished>();
        cacheStore.EvictionObserved.ShouldBeTrue();
        var staleTour = await tourStore.GetTour(tourId, TestContext.Current.CancellationToken);
        staleTour.ShouldNotBeNull();
        staleTour.IsPublished.ShouldBeTrue();

        var projection = new CatalogTourReadModelProjection(tourStore);
        await projection.Apply(unpublishEnvelope, TestContext.Current.CancellationToken);
        var projectedTour = await tourStore.GetTour(tourId, TestContext.Current.CancellationToken);
        projectedTour.ShouldNotBeNull();
        projectedTour.IsPublished.ShouldBeFalse();
    }

    [Fact]
    public async Task Post_commit_public_content_cache_eviction_does_not_capture_request_cancellation()
    {
        // Arrange
        var cacheStore = new RecordingOutputCacheStore();
        await using var baseFactory = CatalogApiTestHost.Create(
            new TestCatalogTourReadModelStore(),
            new TestPublicContentStore());
        await using var factory = baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore>(cacheStore))));
        using var client = factory.CreateClient();
        using var requestCancellation = new CancellationTokenSource();

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            CatalogApiCachingTestData.CreateContentRequest("Published content"),
            requestCancellation.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        cacheStore.EvictionObserved.ShouldBeTrue();
        cacheStore.EvictionCancellationToken.ShouldBe(CancellationToken.None);
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
        client.DefaultRequestHeaders.Authorization = null;

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var firstContent = await firstResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Store-only content"), cancellationToken);
        using var cachedResponse = await client.GetAsync(new Uri("/api/v1/public/catalog/content/home.hero?culture=en-US", UriKind.Relative), cancellationToken);
        var cachedContent = await cachedResponse.Content.ReadFromJsonAsync<PublicContentVariantDto>(cancellationToken);
        CatalogApiTestHost.ConfigureAuthenticatedClient(client);
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
    public async Task Public_content_cache_uses_canonical_culture_for_language_alias()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var contentStore = new TestPublicContentStore();
        await contentStore.SaveContent(CatalogApiCachingTestData.CreatePublishedContent("Original content"), cancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), contentStore);
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = null;

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
        client.DefaultRequestHeaders.Authorization = null;

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
        client.DefaultRequestHeaders.Authorization = null;

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
        client.DefaultRequestHeaders.Authorization = null;

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
        client.DefaultRequestHeaders.Authorization = null;

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
