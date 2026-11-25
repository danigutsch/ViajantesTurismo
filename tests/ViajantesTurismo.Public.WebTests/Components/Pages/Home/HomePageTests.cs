using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests.Components.Pages.Home;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class HomePageTests
{
    [Theory]
    [InlineData("en", "Browse all tours")]
    [InlineData("pt", "Veja todas as viagens")]
    public async Task Home_shows_a_bounded_preview_and_links_to_the_localized_tour_index(string culture, string linkText)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        for (var number = 1; number <= 803; number++)
        {
            var slug = FormattableString.Invariant($"preview-ride-{number:D3}");
            var title = FormattableString.Invariant($"Preview Ride {number:D3}");
            catalogApi.AddTour(PublicTourTestData.CreateDetails(slug, title));
        }

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        // Act
        using var response = await client.GetAsync(new Uri($"/{culture}/", UriKind.Relative), ct);
        var content = await response.Content.ReadAsStringAsync(ct);
        var indexPath = $"/{culture}/group-bike-tours";
        using var indexResponse = await client.GetAsync(new Uri(indexPath, UriKind.Relative), ct);
        var indexContent = await indexResponse.Content.ReadAsStringAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var headings = content.Split("<h3 class=\"site-card__title\">", StringSplitOptions.None).Length - 1;
        headings.ShouldBe(6);
        content.ShouldContain("Preview Ride 006", StringComparison.Ordinal);
        content.ShouldNotContain("Preview Ride 007", StringComparison.Ordinal);
        content.ShouldContain($"href=\"{indexPath}\">{linkText}", StringComparison.Ordinal);
        PrerenderedComponentState.ShouldKeepServerStateBelow(content);
        indexResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        indexContent.ShouldContain($"href=\"{indexPath}/preview-ride-007\">Preview Ride 007", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/html");
        content.ShouldContain("<html lang=\"en-US\">", StringComparison.Ordinal);
        content.ShouldContain("Viajantes Turismo", StringComparison.Ordinal);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.ShouldContain("New tours will be published soon.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_returns_published_tours_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h3 class=\"site-card__title\"><a href=\"/en/group-bike-tours/camino-norte\">Camino Norte</a></h3>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_renders_public_content_for_requested_culture()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("pt-BR", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.PortugueseBrazil,
            Title = "Cicloturismo pelo mundo!",
            Body = "Pedale com cultura, saúde e diversão.",
            SeoTitle = "Cicloturismo - Viajantes Turismo"
        });

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/pt/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<html lang=\"pt-BR\">", StringComparison.Ordinal);
        content.ShouldContain("<title>Cicloturismo - Viajantes Turismo</title>", StringComparison.Ordinal);
        content.ShouldContain("<h1 class=\"site-hero__title\">Cicloturismo pelo mundo!</h1>", StringComparison.Ordinal);
        content.ShouldContain("Pedale com cultura", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_content_key_when_loading_public_content()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("other.section", "en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Wrong section",
            Body = "This content belongs elsewhere.",
            SeoTitle = "Wrong section - Viajantes Turismo"
        });

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.Contains("Wrong section", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Root_loads_public_content_and_tours_concurrently()
    {
        // Arrange
        var contentStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowResponses = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var catalogApi = new FakePublicCatalogApiClient
        {
            ContentStarted = contentStarted,
            ListStarted = listStarted,
            ListAndContentResponseGate = allowResponses.Task
        };
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            SeoTitle = "Cycle safely - Viajantes Turismo"
        });

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();
        using var requestDeadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        requestDeadline.CancelAfter(TimeSpan.FromSeconds(30));

        // Act
        var responseTask = client.GetAsync(new Uri("/en/", UriKind.Relative), requestDeadline.Token);
        await FakePublicCatalogApiClient.ReleaseResponsesAfter(
            Task.WhenAll(contentStarted.Task, listStarted.Task), allowResponses, requestDeadline.Token);
        using var response = await responseTask;
        var content = await response.Content.ReadAsStringAsync(requestDeadline.Token);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle safely", StringComparison.Ordinal);
        content.ShouldContain("Camino Norte", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_ignores_unsupported_culture_query_and_uses_default_content()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageTags.EnglishUnitedStates,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            SeoTitle = "Cycle safely - Viajantes Turismo"
        });

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/?culture=fr-FR", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h1 class=\"site-hero__title\">Cycle safely</h1>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_still_renders_tours_when_public_content_load_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailContentRequests = true };
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.ShouldContain("<h3 class=\"site-card__title\"><a href=\"/en/group-bike-tours/camino-norte\">Camino Norte</a></h3>", StringComparison.Ordinal);
        content.Contains("Tours could not be loaded", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Root_preserves_default_hero_when_optional_public_content_is_cancelled_upstream()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient
        {
            ThrowOperationCanceledExceptionOnContentRequests = true
        };
        catalogApi.AddTour(PublicTourTestData.CreateDetails("camino-norte", "Camino Norte"));

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.ShouldContain("<h3 class=\"site-card__title\"><a href=\"/en/group-bike-tours/camino-norte\">Camino Norte</a></h3>", StringComparison.Ordinal);
        content.Contains("Tours could not be loaded", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Root_returns_unavailable_message_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };

        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        content.ShouldContain("Tours could not be loaded right now. Try again later.", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Production_root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebTestHost.CreateFactory(
            environment: "Production",
            canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
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
        await using var factory = PublicWebTestHost.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        var html = await client.GetStringAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        html.ShouldContain($"<meta name=\"description\" content=\"{expectedDescription}\"", StringComparison.Ordinal);
        html.ShouldContain("Guided tours for everyone.</p>", StringComparison.Ordinal);
    }
}
