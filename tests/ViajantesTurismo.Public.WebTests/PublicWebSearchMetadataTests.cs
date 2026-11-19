using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebSearchMetadataTests
{
    [Theory]
    [InlineData("/en/", "en-US", "Cycle tourism around the world!", "Travel differently. ViajantesTurismo is culture, health, and fun.")]
    [InlineData("/pt/", "pt-BR", "Cicloturismo pelo mundo!", "Viaje de um jeito diferente. ViajantesTurismo é cultura, saúde e diversão.")]
    [InlineData("/en/group-bike-tours", "en-US", "Group Bike Tours - Viajantes Turismo", "Explore our published group cycling tours.")]
    [InlineData("/pt/group-bike-tours", "pt-BR", "Viagens de bicicleta em grupo - Viajantes Turismo", "Conheça nossas viagens de bicicleta em grupo.")]
    [InlineData("/en/gallery", "en-US", "Gallery - Viajantes Turismo", "Moments from Viajantes Turismo cycle tours.")]
    [InlineData("/pt/gallery", "pt-BR", "Galeria - Viajantes Turismo", "Momentos das viagens de bicicleta da Viajantes Turismo.")]
    public async Task Public_pages_publish_localized_search_metadata(string path, string language, string title, string description)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();

        // Act
        var response = await client.GetStringAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var html = WebUtility.HtmlDecode(response);

        // Assert
        html.ShouldContain($"<html lang=\"{language}\">", StringComparison.Ordinal);
        html.ShouldContain($"<title>{title}</title>", StringComparison.Ordinal);
        html.ShouldContain($"<meta name=\"description\" content=\"{description}\"", StringComparison.Ordinal);
        html.ShouldContain($"<meta property=\"og:description\" content=\"{description}\"", StringComparison.Ordinal);
        html.ShouldContain($"<link rel=\"canonical\" href=\"https://public.example.test{path}\"", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("A tailored search description.", "A tailored search description.")]
    [InlineData(" ", "Guided tours for everyone.")]
    public async Task Home_search_description_prefers_the_published_variant_metadata(string metaDescription, string expectedDescription)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            MetaDescription = metaDescription
        });
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        var html = await client.GetStringAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        html.ShouldContain($"<meta name=\"description\" content=\"{expectedDescription}\"", StringComparison.Ordinal);
        html.ShouldContain("Guided tours for everyone.</p>", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/pt/group-bike-tours/missing-tour", false, HttpStatusCode.NotFound, "Página não encontrada")]
    [InlineData("/pt/group-bike-tours/camino-norte", true, HttpStatusCode.ServiceUnavailable, "Viagem indisponível")]
    public async Task Unavailable_public_pages_keep_localized_error_titles_without_published_metadata(
        string path,
        bool failDetails,
        HttpStatusCode status,
        string errorTitle)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailDetailsRequests = failDetails };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var html = WebUtility.HtmlDecode(content);

        // Assert
        response.StatusCode.ShouldBe(status);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        content.ShouldNotContain("rel=\"canonical\"", StringComparison.Ordinal);
        content.ShouldNotContain("property=\"og:url\"", StringComparison.Ordinal);
        html.ShouldContain($"<title>{errorTitle} - Viajantes Turismo</title>", StringComparison.Ordinal);
        html.ShouldContain($">{errorTitle}</h1>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_route_returns_not_found_without_published_metadata()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/pt/invalid-route", UriKind.Relative), TestContext.Current.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        html.ShouldNotContain("rel=\"canonical\"", StringComparison.Ordinal);
        html.ShouldNotContain("property=\"og:url\"", StringComparison.Ordinal);
    }
}
