using Microsoft.AspNetCore.Mvc.Testing;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebCultureTests
{
    [Theory]
    [InlineData(null, "/en/")]
    [InlineData("pt-BR,pt;q=0.9,en;q=0.5", "/pt/")]
    [InlineData("fr-FR", "/en/")]
    public async Task Root_redirects_to_the_visitors_language(string? acceptLanguage, string expectedLocation)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/", UriKind.Relative));
        if (acceptLanguage is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(expectedLocation);
    }

    [Theory]
    [InlineData("/?culture=pt-BR", "/pt/")]
    [InlineData("/?language=pt", "/pt/")]
    [InlineData("/group-bike-tours?culture=EN-us", "/en/group-bike-tours")]
    [InlineData("/group-bike-tours/camino-norte?language=pt-BR", "/pt/group-bike-tours/camino-norte")]
    [InlineData("/gallery?culture=zz&language=en", "/en/gallery")]
    public async Task Former_culture_query_addresses_move_permanently_to_the_culture_path(string path, string expectedLocation)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(expectedLocation);
    }

    [Theory]
    [InlineData("/pt-BR/gallery", "/pt/gallery")]
    [InlineData("/en-US/", "/en/")]
    [InlineData("/pt-br", "/pt/")]
    [InlineData("/EN/group-bike-tours/camino-norte?page=2", "/en/group-bike-tours/camino-norte?page=2")]
    public async Task Regional_or_differently_cased_culture_segments_move_permanently_to_the_short_culture(
        string path,
        string expectedLocation)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MovedPermanently);
        response.Headers.Location.ShouldNotBeNull().OriginalString.ShouldBe(expectedLocation);
    }

    [Fact]
    public async Task Culture_path_requests_the_content_language_from_the_catalog()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("pt-BR", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.PortugueseBrazil,
            Title = "Cicloturismo pelo mundo!",
            Body = "Pedale com cultura, saúde e diversão."
        });
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Cycle safely",
            Body = "Guided tours for everyone."
        });
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        var portuguese = await client.GetStringAsync(new Uri("/pt/", UriKind.Relative), TestContext.Current.CancellationToken);
        var english = await client.GetStringAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        portuguese.ShouldContain("<html lang=\"pt-BR\">", StringComparison.Ordinal);
        portuguese.ShouldContain("Cicloturismo pelo mundo!", StringComparison.Ordinal);
        english.ShouldContain("<html lang=\"en-US\">", StringComparison.Ordinal);
        english.ShouldContain("Cycle safely", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Pages_link_their_canonical_address_and_language_versions()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi, canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();

        // Act
        var content = await client.GetStringAsync(
            new Uri("/pt/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        content.ShouldContain("<link rel=\"canonical\" href=\"https://public.example.test/pt/group-bike-tours/camino-norte\"", StringComparison.Ordinal);
        content.ShouldContain("<link rel=\"alternate\" hreflang=\"en-US\" href=\"https://public.example.test/en/group-bike-tours/camino-norte\"", StringComparison.Ordinal);
        content.ShouldContain("<link rel=\"alternate\" hreflang=\"pt-BR\" href=\"https://public.example.test/pt/group-bike-tours/camino-norte\"", StringComparison.Ordinal);
        content.ShouldContain("<link rel=\"alternate\" hreflang=\"x-default\" href=\"https://public.example.test/group-bike-tours/camino-norte\"", StringComparison.Ordinal);
        content.ShouldContain("<meta property=\"og:locale\" content=\"pt_BR\"", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Language_switcher_and_navigation_stay_on_the_page_in_each_language()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        var content = await client.GetStringAsync(new Uri("/pt/gallery", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        content.ShouldContain("<nav class=\"site-language-switcher\" aria-label=\"Language\">", StringComparison.Ordinal);
        content.ShouldContain("<a href=\"/en/gallery\" hreflang=\"en-US\" lang=\"en-US\">English</a>", StringComparison.Ordinal);
        content.ShouldContain("<a href=\"/pt/gallery\" hreflang=\"pt-BR\" lang=\"pt-BR\" aria-current=\"true\">Portugu&#xEA;s</a>", StringComparison.Ordinal);
        content.ShouldContain("<a href=\"/pt/group-bike-tours\">Bike Tours</a>", StringComparison.Ordinal);
        content.ShouldContain("<a href=\"/pt/gallery\" aria-current=\"page\">Gallery</a>", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/robots.txt")]
    [InlineData("/sitemap.xml")]
    [InlineData("/theme.css")]
    public async Task Files_and_health_endpoints_are_not_redirected_to_a_culture(string path)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
