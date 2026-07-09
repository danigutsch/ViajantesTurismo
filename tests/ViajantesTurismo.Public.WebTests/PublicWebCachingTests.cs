using Microsoft.AspNetCore.Mvc.Testing;
using SharedKernel.Testing.Assertions;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class PublicWebCachingTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/group-bike-tours")]
    [InlineData("/group-bike-tours/camino-norte")]
    [InlineData("/gallery")]
    public async Task Public_ssr_routes_emit_cache_metadata(string path)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.ToString().ShouldContain("public", StringComparison.Ordinal);
        cacheControl.ToString().ShouldContain("max-age=60", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_list_uses_output_cache_for_published_content()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/group-bike-tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var firstContent = await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        catalogApi.FailListRequests = true;
        using var cachedResponse = await client.GetAsync(new Uri("/group-bike-tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var cachedContent = await cachedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstContent.ShouldContain("Camino Norte", StringComparison.Ordinal);
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedContent.ShouldContain("Camino Norte", StringComparison.Ordinal);
        cachedContent.ShouldNotContain("Tours could not be loaded right now.");
    }

    [Fact]
    public async Task Public_web_cache_uses_canonical_culture_for_language_alias()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Original hero",
            Body = "Original body"
        });
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var firstResponse = await client.GetAsync(new Uri("/?culture=EN&language=pt-BR", UriKind.Relative), TestContext.Current.CancellationToken);
        var firstContent = await firstResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Store-only hero",
            Body = "Store-only body"
        });
        using var cachedResponse = await client.GetAsync(new Uri("/?culture=en-US", UriKind.Relative), TestContext.Current.CancellationToken);
        var cachedContent = await cachedResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        firstContent.ShouldContain("Original hero", StringComparison.Ordinal);
        cachedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        cachedContent.ShouldContain("Original hero", StringComparison.Ordinal);
        cachedContent.ShouldNotContain("Store-only hero");
    }

    [Fact]
    public async Task Public_ssr_load_failures_are_not_cacheable()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/group-bike-tours", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.ToString().ShouldContain("no-store", StringComparison.Ordinal);
        content.ShouldContain("Tours could not be loaded right now.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Error_endpoint_is_not_cacheable()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri("/Error", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.ToString().ShouldContain("no-store", StringComparison.Ordinal);
    }
}
