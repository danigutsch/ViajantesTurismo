using PandaBear.Testing;
using ViajantesTurismo.Catalog.ApiService;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class PublicCatalogProxyRateLimitTests
{
    [Theory]
    [InlineData(false, null)]
    [InlineData(false, "203.0.113.20")]
    [InlineData(false, "203.0.113.20, 198.51.100.5")]
    [InlineData(true, null)]
    public async Task One_visitors_catalog_allowance_is_shared_across_frontends_without_blocking_other_visitors(
        bool mappedAddress, string? forgedForwardedFor)
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        using var attacker = proxy.CreateVisitor(IPAddress.Parse("203.0.113.10"));
        var otherRepresentation = mappedAddress
            ? IPAddress.Parse("203.0.113.10").MapToIPv6()
            : IPAddress.Parse("203.0.113.10");
        using var attackerReplica = proxy.CreateVisitor(otherRepresentation, frontendReplica: 1);
        using var freshAttackerReplica = proxy.CreateVisitor(otherRepresentation, frontendReplica: 2);
        using var visitor = proxy.CreateVisitor(IPAddress.Parse("203.0.113.20"));
        using var initialResponse = await visitor.GetAsync(
            new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
        initialResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);

        // Act
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit; requestNumber++)
        {
            var frontend = requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit / 2 ? attacker : attackerReplica;
            using var response = await frontend.GetAsync(
                new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
        }

        using var extraRequest = new HttpRequestMessage(HttpMethod.Get,
            new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative));
        if (forgedForwardedFor is not null)
        {
            extraRequest.Headers.Add("X-Forwarded-For", forgedForwardedFor);
        }

        using var limitedResponse = await freshAttackerReplica.SendAsync(extraRequest, TestContext.Current.CancellationToken);
        using var unaffectedResponse = await visitor.GetAsync(
            new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        unaffectedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Backend_quota_responses_are_not_retried_or_counted_as_shared_circuit_breaker_failures()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope(sensitiveCircuitBreaker: true);
        var attackerAddress = IPAddress.Parse("203.0.113.10");
        using var attacker = proxy.CreateVisitor(attackerAddress);
        using var attackerReplica = proxy.CreateVisitor(attackerAddress, frontendReplica: 1);
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit; requestNumber++)
        {
            var frontend = requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit / 2 ? attacker : attackerReplica;
            using var response = await frontend.GetAsync(
                new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
        }

        using var freshAttacker = proxy.CreateVisitor(attackerAddress, frontendReplica: 2);
        using var visitor = proxy.CreateVisitor(IPAddress.Parse("203.0.113.20"), frontendReplica: 2);
        var beforeRequests = proxy.CatalogRequestCount;

        // Act
        for (var requestNumber = 0; requestNumber < 3; requestNumber++)
        {
            using var response = await freshAttacker.GetAsync(
                new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        }
        var requestsAfterRejections = proxy.CatalogRequestCount;
        var backendAttempts = requestsAfterRejections - beforeRequests;
        using var unaffectedResponse = await visitor.GetAsync(
            new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        backendAttempts.ShouldBe(3);
        unaffectedResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Exhausting_media_requests_does_not_block_another_visitors_pages_or_sitemap()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        await proxy.SeedPublishedGallery(1, TestContext.Current.CancellationToken);
        using var attacker = proxy.CreateVisitor(IPAddress.Parse("203.0.113.10"));
        using var attackerReplica = proxy.CreateVisitor(IPAddress.Parse("203.0.113.10"), frontendReplica: 1);
        using var visitor = proxy.CreateVisitor(IPAddress.Parse("203.0.113.20"));
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit; requestNumber++)
        {
            var frontend = requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit / 2 ? attacker : attackerReplica;
            using var response = await frontend.GetAsync(
                new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
        }

        // Act
        foreach (var path in new[] { "/", "/group-bike-tours", "/group-bike-tours/quota-gallery", "/gallery", "/sitemap.xml" })
        {
            using var response = await visitor.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task A_legitimate_gallery_can_render_and_stream_more_than_sixty_images()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        var images = await proxy.SeedPublishedGallery(70, TestContext.Current.CancellationToken);
        using var visitor = proxy.CreateVisitor(IPAddress.Parse("203.0.113.20"));

        // Act
        using var gallery = await visitor.GetAsync(new Uri("/gallery", UriKind.Relative), TestContext.Current.CancellationToken);
        var html = await gallery.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        gallery.StatusCode.ShouldBe(HttpStatusCode.OK);
        html.ShouldContain("A full gallery", StringComparison.Ordinal);
        foreach (var imageId in images)
        {
            using var response = await visitor.GetAsync(
                new Uri($"/catalog/media/{imageId}/640/jpg", UriKind.Relative), TestContext.Current.CancellationToken);
            var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("image/jpeg");
            content.ShouldBe("gallery-media"u8.ToArray());
        }
    }

    [Fact]
    public async Task Frontend_quota_rejections_are_not_cached_and_leave_health_and_robots_available()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        using var visitor = proxy.CreateVisitor(IPAddress.Parse("203.0.113.10"));
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit / 2; requestNumber++)
        {
            using var response = await visitor.GetAsync(
                new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
        }

        // Act
        using var limitedResponse = await visitor.GetAsync(
            new Uri($"/catalog/media/{Guid.NewGuid()}/320/webp", UriKind.Relative), TestContext.Current.CancellationToken);
        using var healthResponse = await visitor.GetAsync(new Uri("/alive", UriKind.Relative), TestContext.Current.CancellationToken);
        using var robotsResponse = await visitor.GetAsync(new Uri("/robots.txt", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        limitedResponse.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        var retryDelay = limitedResponse.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldNotBeNull();
        retryDelay.ShouldBeGreaterThan(TimeSpan.Zero);
        healthResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        robotsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Warm_catalog_reads_still_consume_only_the_callers_allowance()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        using var caller = proxy.CreateDirectCatalogCaller(IPAddress.Parse("203.0.113.10"));
        using var otherCaller = proxy.CreateDirectCatalogCaller(IPAddress.Parse("203.0.113.20"));
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit; requestNumber++)
        {
            using var response = await caller.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // Act
        using var limitedResponse = await caller.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);
        using var otherResponse = await otherCaller.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Untrusted_direct_catalog_callers_cannot_mint_allowances_with_forwarded_headers()
    {
        // Arrange
        await using var proxy = new PublicCatalogProxyTestScope();
        using var caller = proxy.CreateDirectCatalogCaller(IPAddress.Parse("203.0.113.10"));
        using var otherCaller = proxy.CreateDirectCatalogCaller(IPAddress.Parse("203.0.113.20"));
        for (var requestNumber = 0; requestNumber < CatalogSecurityBaseline.PublicReadPermitLimit; requestNumber++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/public/catalog/tours", UriKind.Relative));
            request.Headers.Add("X-Forwarded-For", $"198.51.100.{requestNumber}");
            using var response = await caller.SendAsync(request, TestContext.Current.CancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }

        // Act
        using var finalRequest = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/v1/public/catalog/tours", UriKind.Relative));
        finalRequest.Headers.Add("X-Forwarded-For", "2001:db8::1, 198.51.100.250");
        using var limitedResponse = await caller.SendAsync(finalRequest, TestContext.Current.CancellationToken);
        using var otherResponse = await otherCaller.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        limitedResponse.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        otherResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
