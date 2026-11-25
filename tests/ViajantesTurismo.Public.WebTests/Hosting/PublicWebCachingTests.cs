using Microsoft.AspNetCore.Mvc.Testing;

namespace ViajantesTurismo.Public.WebTests.Hosting;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebCachingTests
{
    [Theory]
    [InlineData("/en/")]
    [InlineData("/pt/group-bike-tours")]
    [InlineData("/en/group-bike-tours/camino-norte")]
    [InlineData("/pt/gallery")]
    [InlineData("/sitemap.xml")]
    public async Task Tour_dependent_content_is_not_stored(string path)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Unversioned_theme_stylesheet_retains_public_cache_metadata()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/theme.css", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        cacheControl.MaxAge.ShouldBe(TimeSpan.FromSeconds(60));
        cacheControl.ToString().ShouldNotContain("stale-while-revalidate", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Culture_redirect_is_not_publicly_cacheable()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri("/gallery", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var isPublic = response.Headers.CacheControl?.Public ?? false;
        isPublic.ShouldBeFalse();
        response.Headers.Vary.ShouldContain("Accept-Language");
    }

    [Fact]
    public async Task Public_tour_list_reflects_catalog_updates()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/en/group-bike-tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var firstContent = await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        catalogApi.AddTour(PublicTourTestData.CreateDetails("atlantic-way", "Atlantic Way"));
        using var updatedResponse = await client.GetAsync(new Uri("/en/group-bike-tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var updatedContent = await updatedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstContent.ShouldContain("Camino Norte", StringComparison.Ordinal);
        updatedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        updatedContent.ShouldContain("Camino Norte", StringComparison.Ordinal);
        updatedContent.ShouldContain("Atlantic Way", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_ssr_load_failures_are_not_cacheable()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/group-bike-tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
        content.ShouldContain("Tours could not be loaded right now.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sitemap_XML_failure_is_not_cacheable()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };
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

    [Fact]
    public async Task Error_endpoint_is_not_cacheable()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri("/Error", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        var expires = response.Headers.NonValidated.TryGetValues("Expires", out var values)
            ? values
            : response.Content.Headers.NonValidated["Expires"];
        expires.ShouldHaveSingleItem().ShouldBe("Thu, 01 Jan 1970 00:00:00 GMT");
    }
}
