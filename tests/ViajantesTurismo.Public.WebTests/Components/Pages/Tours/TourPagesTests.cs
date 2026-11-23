using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Components.Pages.Tours;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class TourPagesTests
{
    [Fact]
    public async Task Public_tour_index_returns_a_bounded_window_and_has_a_crawlable_localized_next_page()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        for (var number = 1; number <= 803; number++)
        {
            var slug = FormattableString.Invariant($"page-ride-{number:D3}");
            var title = FormattableString.Invariant($"Page Ride {number:D3}");
            catalogApi.AddTour(PublicTourTestData.CreateDetails(slug, title));
        }

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi, canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var first = await client.GetAsync(new Uri("/pt/group-bike-tours", UriKind.Relative), ct);
        var firstContent = await first.Content.ReadAsStringAsync(ct);
        using var next = await client.GetAsync(new Uri("/pt/group-bike-tours?page=2", UriKind.Relative), ct);
        var nextContent = await next.Content.ReadAsStringAsync(ct);

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        next.StatusCode.ShouldBe(HttpStatusCode.OK);
        var firstHeadings = firstContent.Split("<h2 class=\"site-card__title\">", StringSplitOptions.None).Length - 1;
        var nextHeadings = nextContent.Split("<h2 class=\"site-card__title\">", StringSplitOptions.None).Length - 1;
        firstHeadings.ShouldBe(12);
        nextHeadings.ShouldBe(12);
        firstContent.ShouldContain("Page Ride 012", StringComparison.Ordinal);
        firstContent.ShouldNotContain("Page Ride 013", StringComparison.Ordinal);
        firstContent.ShouldContain("href=\"/pt/group-bike-tours?page=2\"", StringComparison.Ordinal);
        nextContent.ShouldContain("Page Ride 013", StringComparison.Ordinal);
        nextContent.ShouldNotContain("Page Ride 012", StringComparison.Ordinal);
        nextContent.ShouldContain("rel=\"canonical\" href=\"https://public.example.test/pt/group-bike-tours?page=2\"", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(firstContent);
        PrerenderedComponentState.ShouldKeepServerStateBelow(nextContent);
    }

    [Fact]
    public async Task Public_tour_detail_displays_a_cover_and_one_window_of_a_large_photo_collection()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        var photos = Enumerable.Range(0, 803)
            .Select(index => PublicTourTestData.CreateImage(FormattableString.Invariant($"Road photo {index:D3}")) with
            {
                IsCover = index == 0,
                SortOrder = index
            })
            .ToArray();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("large-legacy-tour", "Large legacy tour") with
        {
            Images = photos
        });
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi, canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.GetAsync(new Uri("/en/group-bike-tours/large-legacy-tour", UriKind.Relative), ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        using var next = await client.GetAsync(new Uri("/en/group-bike-tours/large-legacy-tour?page=2", UriKind.Relative), ct);
        var nextContent = await next.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var photoElements = content.Split("alt=\"Road photo ", StringSplitOptions.None).Length - 1;
        photoElements.ShouldBe(7);
        content.ShouldContain("alt=\"Road photo 000\"", StringComparison.Ordinal);
        content.ShouldContain("alt=\"Road photo 006\"", StringComparison.Ordinal);
        content.ShouldNotContain("alt=\"Road photo 007\"", StringComparison.Ordinal);
        content.ShouldContain("href=\"/en/group-bike-tours/large-legacy-tour?page=2\"", StringComparison.Ordinal);
        next.StatusCode.ShouldBe(HttpStatusCode.OK);
        nextContent.ShouldContain("alt=\"Road photo 000\"", StringComparison.Ordinal);
        nextContent.ShouldContain("alt=\"Road photo 007\"", StringComparison.Ordinal);
        nextContent.ShouldNotContain("alt=\"Road photo 006\"", StringComparison.Ordinal);
        nextContent.ShouldContain("rel=\"canonical\" href=\"https://public.example.test/en/group-bike-tours/large-legacy-tour?page=2\"", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(content);
        PrerenderedComponentState.ShouldKeepServerStateBelow(nextContent);
    }

    [Theory]
    [InlineData("/en/group-bike-tours?page=0")]
    [InlineData("/pt/group-bike-tours?page=99")]
    [InlineData("/en/group-bike-tours/camino-norte?page=invalid")]
    public async Task Invalid_public_tour_pages_return_not_found_without_redirecting(string path)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.Location.ShouldBeNull();
    }


    [Fact]
    public async Task Public_tour_list_returns_published_tours_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h2 class=\"site-card__title\"><a href=\"/en/group-bike-tours/camino-norte\">Camino Norte</a></h2>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_list_returns_unavailable_message_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        content.ShouldContain("Tours could not be loaded right now. Try again later.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_tour_content_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h1 class=\"site-hero__title\">Camino Norte</h1>", StringComparison.Ordinal);
        content.ShouldContain("Discover Camino Norte by bicycle.", StringComparison.Ordinal);
        content.ShouldContain("A detailed description of Camino Norte.", StringComparison.Ordinal);
        content.ShouldContain("Day one: explore Camino Norte.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_use_configured_canonical_origin_for_social_metadata()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        var image = PublicTourTestData.CreateImage("Cyclists on the Camino");
        var tour = PublicTourTestData.CreateDetails("camino-norte", "Camino Norte") with
        {
            SeoTitle = "Camino Norte cycling tour",
            SeoDescription = "Cycle the Camino Norte with Viajantes Turismo.",
            Images = [image]
        };
        catalogApi.AddTour(tour);

        await using var factory = PublicWebTestHost.CreateFactory(
            catalogApi,
            canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/en/group-bike-tours/camino-norte", UriKind.Relative));
        request.Headers.Host = "attacker.example.test";

        // Act
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<title>Camino Norte cycling tour</title>", StringComparison.Ordinal);
        content.ShouldContain("name=\"description\" content=\"Cycle the Camino Norte with Viajantes Turismo.\"", StringComparison.Ordinal);
        content.ShouldContain("rel=\"canonical\" href=\"https://public.example.test/en/group-bike-tours/camino-norte\"", StringComparison.Ordinal);
        content.ShouldContain("property=\"og:title\" content=\"Camino Norte cycling tour\"", StringComparison.Ordinal);
        content.ShouldContain("property=\"og:url\" content=\"https://public.example.test/en/group-bike-tours/camino-norte\"", StringComparison.Ordinal);
        content.ShouldContain($"property=\"og:image\" content=\"https://public.example.test/catalog/media/{image.Id}/640/jpg\"", StringComparison.Ordinal);
        content.ShouldNotContain("attacker.example.test", StringComparison.Ordinal);
        var coverAltOccurrences = content.Split("alt=\"Cyclists on the Camino\"", StringSplitOptions.None).Length - 1;
        coverAltOccurrences.ShouldBe(1);
    }

    [Fact]
    public async Task Public_tour_details_encode_presentation_and_metadata_as_plain_text()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("safe-tour", "Safe Tour") with
        {
            Summary = "<script>alert('summary')</script>",
            Description = "<img src=x onerror=alert('description')>",
            SeoTitle = "Tour title\"><script>alert('title')</script>",
            SeoDescription = "Description\"><script>alert('metadata')</script>"
        });
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours/safe-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("&lt;script&gt;", StringComparison.Ordinal);
        content.ShouldContain("&lt;img src=x", StringComparison.Ordinal);
        content.ShouldNotContain("<script>alert", StringComparison.OrdinalIgnoreCase);
        content.ShouldNotContain("<img src=x", StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Public_tour_details_returns_unavailable_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailDetailsRequests = true };

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        content.ShouldContain("<h1 class=\"site-section__title\">Tour unavailable</h1>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_service_unavailable_without_caching_when_upstream_is_cancelled()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { ThrowOperationCanceledExceptionOnDetailsRequests = true };
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        response.Headers.CacheControl.ShouldNotBeNull();
        response.Headers.CacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
    }

    [Fact]
    public async Task Public_tour_details_returns_not_found_when_tour_is_not_published()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/en/group-bike-tours/missing-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl.ShouldNotBeNull();
        response.Headers.CacheControl.NoStore.ShouldBeTrue();
        response.Headers.GetValues("Pragma").ShouldHaveSingleItem().ShouldBe("no-cache");
        content.ShouldContain("<h1 class=\"site-section__title\">Page not found</h1>", StringComparison.Ordinal);
    }
}
