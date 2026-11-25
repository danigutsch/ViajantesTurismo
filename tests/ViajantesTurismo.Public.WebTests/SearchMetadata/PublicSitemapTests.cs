using Microsoft.AspNetCore.Mvc.Testing;
using System.Xml.Linq;

namespace ViajantesTurismo.Public.WebTests.SearchMetadata;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
[TestScope(TestScope.Integration)]
public sealed class PublicSitemapTests
{
    [Fact]
    public async Task Robots_txt_allows_public_crawling()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://testserver")
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/robots.txt", UriKind.Relative));
        request.Headers.Host = "evil.example.test";

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentType.ShouldNotBeNull().CharSet.ShouldBe("utf-8");
        body.ShouldBe("User-agent: *\nAllow: /\nSitemap: https://localhost:7003/sitemap.xml");
    }

    [Fact]
    public async Task Robots_txt_uses_sitemap_canonical_origin_from_configuration()
    {
        // Arrange
        await using var sourceFactory = PublicWebTestHost.CreateFactory();
        await using var factory = sourceFactory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("PublicWeb:Sitemap:CanonicalOrigin", "https://public.example.test");
        });
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://testserver")
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/robots.txt", UriKind.Relative));
        request.Headers.Host = "evil.example.test";

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldBe("User-agent: *\nAllow: /\nSitemap: https://public.example.test/sitemap.xml");
    }

    [Fact]
    public async Task Sitemap_XML_includes_only_canonical_public_pages()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        var updatedAt = new DateTimeOffset(2026, 7, 11, 9, 30, 0, TimeSpan.Zero);
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte") with
        {
            UpdatedAt = updatedAt
        });
        catalogApi.AddTour(PublicTourTestData.CreateDetails("missing-date", "Missing date") with
        {
            UpdatedAt = default
        });
        catalogApi.AddTour(PublicTourTestData.CreateDetails("invalid/tour", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails("invalid\\tour", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails("invalid?tour", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails("invalid#tour", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails("\u0001invalid", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails(".", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails("..", "Invalid tour"));
        catalogApi.AddTour(PublicTourTestData.CreateDetails(" ", "Invalid tour"));

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://testserver"),
            AllowAutoRedirect = false
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/sitemap.xml", UriKind.Relative));
        request.Headers.Host = "evil.example.test";

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var document = XDocument.Parse(body);
        var sitemapNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var locations = document.Descendants(sitemapNamespace + "loc").Select(element => element.Value).ToArray();
        var lastModified = document.Descendants(sitemapNamespace + "lastmod").Select(element => element.Value).ToArray();
        var tourLinks = document.Descendants(sitemapNamespace + "url")
            .Where(url => url.Element(sitemapNamespace + "loc")?.Value == "https://localhost:7003/pt/group-bike-tours/camino-norte")
            .Elements(XNamespace.Get("http://www.w3.org/1999/xhtml") + "link")
            .ToArray();
        var fallback = tourLinks.Where(link => link.Attribute("hreflang")?.Value == "x-default").ShouldHaveSingleItem();
        var fallbackAddress = new Uri(fallback.Attribute("href").ShouldNotBeNull().Value);
        using var fallbackRequest = new HttpRequestMessage(HttpMethod.Get, new Uri(fallbackAddress.PathAndQuery, UriKind.Relative));
        fallbackRequest.Headers.AcceptLanguage.ParseAdd("pt-BR");
        using var fallbackResponse = await client.SendAsync(fallbackRequest, TestContext.Current.CancellationToken);
        var tourAlternates = tourLinks
            .Select(link => $"{link.Attribute("hreflang")?.Value} {link.Attribute("href")?.Value}")
            .ToArray();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/xml");
        locations.ShouldBe([
            "https://localhost:7003/en/",
            "https://localhost:7003/pt/",
            "https://localhost:7003/en/group-bike-tours",
            "https://localhost:7003/pt/group-bike-tours",
            "https://localhost:7003/en/gallery",
            "https://localhost:7003/pt/gallery",
            "https://localhost:7003/en/group-bike-tours/camino-norte",
            "https://localhost:7003/pt/group-bike-tours/camino-norte",
            "https://localhost:7003/en/group-bike-tours/missing-date",
            "https://localhost:7003/pt/group-bike-tours/missing-date"
        ]);
        lastModified.ShouldBe(["2026-07-11T09:30:00Z", "2026-07-11T09:30:00Z"]);
        tourAlternates.ShouldBe([
            "en-US https://localhost:7003/en/group-bike-tours/camino-norte",
            "pt-BR https://localhost:7003/pt/group-bike-tours/camino-norte",
            "x-default https://localhost:7003/group-bike-tours/camino-norte"
        ]);
        fallbackResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        fallbackResponse.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe("/pt/group-bike-tours/camino-norte");
    }

    [Fact]
    public async Task Sitemap_XML_limits_tours_to_protocol_maximum()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        for (var index = 0; index < 25_000; index++)
        {
            catalogApi.AddTour(PublicTourTestData.CreateDetails($"tour-{index}", "Tour"));
        }

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/sitemap.xml", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var document = XDocument.Parse(body);
        var sitemapNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var locations = document.Descendants(sitemapNamespace + "loc").Select(element => element.Value).ToArray();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        catalogApi.TourPageRequestCount.ShouldBe(0);
        catalogApi.SitemapTourLimits.ShouldBe([24_997]);
        locations.Length.ShouldBe(50_000);
        locations[^1].ShouldBe("https://localhost:7003/pt/group-bike-tours/tour-24996");
        locations.ShouldNotContain("https://localhost:7003/en/group-bike-tours/tour-24997");
    }

    [Fact]
    public async Task Sitemap_XML_returns_service_unavailable_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/sitemap.xml", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Sitemap_XML_returns_non_cacheable_service_unavailable_when_catalog_cancels_upstream()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { ThrowOperationCanceledExceptionOnListRequests = true };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/sitemap.xml", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
    }
}
