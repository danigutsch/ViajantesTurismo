using System.Text.Json;
using AngleSharp.Html.Parser;

namespace ViajantesTurismo.Public.WebTests.SearchMetadata;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebSearchMetadataTests
{
    [Theory]
    [InlineData("en", "Camino Coast", "Cycle past the \"old pier\" </script> & along the coast.")]
    [InlineData("pt", "Caminho da Costa", "Pedale pelo cais da baía & pela costa.")]
    public async Task Published_tour_details_describe_visible_content_and_the_canonical_URL(
        string culture,
        string title,
        string summary)
    {
        // Arrange
        const string origin = "https://public.example.test";
        var tour = PublicTourTestData.CreateDetails("coast", title) with
        {
            Summary = summary,
            SeoTitle = "Catalog search title",
            SeoDescription = "Catalog search description"
        };
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(tour);
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi, canonicalOrigin: origin);
        using var client = factory.CreateClient();
        var path = $"/{culture}/group-bike-tours/coast";
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Host = "alternate.example.test";
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.SendAsync(request, ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        using var document = await new HtmlParser().ParseDocumentAsync(content, ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        document.QuerySelector("h1").ShouldNotBeNull().TextContent.ShouldBe(title);
        document.QuerySelector(".site-hero__subtitle").ShouldNotBeNull().TextContent.ShouldBe(summary);
        var script = document.QuerySelectorAll("script[type='application/ld+json']").ShouldHaveSingleItem();
        using var payload = JsonDocument.Parse(script.TextContent);
        var canonical = document.QuerySelector("link[rel='canonical']").ShouldNotBeNull().GetAttribute("href");
        canonical.ShouldBe(origin + path);
        payload.RootElement.GetProperty("@context").GetString().ShouldBe("https://schema.org");
        payload.RootElement.GetProperty("@type").GetString().ShouldBe("TouristTrip");
        payload.RootElement.GetProperty("name").GetString().ShouldBe(title);
        payload.RootElement.GetProperty("description").GetString().ShouldBe(summary);
        payload.RootElement.GetProperty("url").GetString().ShouldBe(canonical);
        var properties = payload.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).ToArray();
        properties.ShouldBe(["@context", "@type", "description", "name", "url"]);
    }

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
        await using var factory = PublicWebTestHost.CreateFactory(canonicalOrigin: "https://public.example.test");
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
        response.ShouldNotContain("application/ld+json", StringComparison.Ordinal);
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
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
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
        content.ShouldNotContain("application/ld+json", StringComparison.Ordinal);
        html.ShouldContain($"<title>{errorTitle} - Viajantes Turismo</title>", StringComparison.Ordinal);
        html.ShouldContain($">{errorTitle}</h1>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_route_returns_not_found_without_published_metadata()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
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
