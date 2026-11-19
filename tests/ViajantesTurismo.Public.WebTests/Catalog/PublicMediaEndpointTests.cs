using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Catalog;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class PublicMediaEndpointTests
{
    [Fact]
    public async Task Public_media_returns_not_found_and_no_store_when_catalog_has_no_media()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/catalog/media/6db0b8be-e4e8-4500-a398-b44e7709a640/640/jpg", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBe(true);
    }

    [Fact]
    public async Task Public_media_streams_catalog_content_with_public_cache_storage()
    {
        // Arrange
        using var upstreamResponse = new HttpResponseMessage();
        var catalogApi = new FakePublicCatalogApiClient
        {
            Media = new PublicMediaObjectResponse(
                upstreamResponse,
                new MemoryStream("image"u8.ToArray()),
                "image/jpeg")
        };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/catalog/media/6db0b8be-e4e8-4500-a398-b44e7709a640/640/jpg", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("image/jpeg");
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        cacheControl.NoStore.ShouldBeFalse();
        cacheControl.MaxAge.ShouldBe(PublicWebHttpCache.PublishedContentFreshness);
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        content.ShouldBe("image"u8.ToArray());
        catalogApi.LastMediaId.ShouldBe(Guid.Parse("6db0b8be-e4e8-4500-a398-b44e7709a640"));
        catalogApi.LastMediaWidth.ShouldBe(640);
        catalogApi.LastMediaFormat.ShouldBe("jpg");
    }

    [Fact]
    public async Task Public_media_ignores_range_requests_and_disposes_the_upstream_stream_after_the_response()
    {
        // Arrange
        using var upstreamResponse = new HttpResponseMessage();
        var upstreamStream = new MemoryStream("image"u8.ToArray());
        var catalogApi = new FakePublicCatalogApiClient
        {
            Media = new PublicMediaObjectResponse(upstreamResponse, upstreamStream, "image/jpeg")
        };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/catalog/media/6db0b8be-e4e8-4500-a398-b44e7709a640/640/jpg", UriKind.Relative));
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(1, 2);

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.AcceptRanges.ShouldBeEmpty();
        response.Content.Headers.ContentRange.ShouldBeNull();
        content.ShouldBe("image"u8.ToArray());
        upstreamStream.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task Public_media_releases_the_upstream_response_when_the_browser_disconnects()
    {
        // Arrange
        using var upstreamResponse = new ReleaseTrackingResponse();
        var upstreamStream = new BlockingReadStream("i"u8.ToArray(), []);
        var catalogApi = new FakePublicCatalogApiClient
        {
            Media = new PublicMediaObjectResponse(upstreamResponse, upstreamStream, "image/jpeg")
        };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();
        var response = await client.GetAsync(
            new Uri("/catalog/media/6db0b8be-e4e8-4500-a398-b44e7709a640/640/jpg", UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead,
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken);
        var firstByte = new byte[1];
        var bytesRead = await body.ReadAsync(firstByte, TestContext.Current.CancellationToken);
        bytesRead.ShouldBe(1);

        // Act
        await upstreamStream.Blocked.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        response.Dispose();
        await Task.WhenAll(upstreamStream.Disposed, upstreamResponse.Released).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        upstreamStream.BlockedReadCancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task Public_media_returns_non_cacheable_service_unavailable_when_catalog_cancels_upstream()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { ThrowOperationCanceledExceptionOnMediaRequests = true };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/catalog/media/6db0b8be-e4e8-4500-a398-b44e7709a640/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Public_media_returns_non_cacheable_service_unavailable_when_catalog_HTTP_request_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailMediaRequests = true };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/catalog/media/6db0b8be-e4e8-4500-a398-b44e7709a640/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
    }
}
