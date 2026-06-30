using Microsoft.AspNetCore.Mvc.Testing;
using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class PublicWebEndpointTests
{
    [Fact]
    public async Task Root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<html lang=\"en\">", content, StringComparison.Ordinal);
        Assert.Contains("Viajantes Turismo", content, StringComparison.Ordinal);
        Assert.Contains("Cycle tourism around the world!", content, StringComparison.Ordinal);
        Assert.Contains("New tours will be published soon.", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_returns_published_tours_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h3><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h3>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_renders_public_content_for_requested_culture()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("pt-BR", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.PtBr,
            Title = "Cicloturismo pelo mundo!",
            Body = "Pedale com cultura, saúde e diversão.",
            SeoTitle = "Cicloturismo - Viajantes Turismo"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/?culture=pt-BR", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"pt-BR\">", content, StringComparison.Ordinal);
        Assert.Contains("<title>Cicloturismo - Viajantes Turismo</title>", content, StringComparison.Ordinal);
        Assert.Contains("<h1>Cicloturismo pelo mundo!</h1>", content, StringComparison.Ordinal);
        Assert.Contains("Pedale com cultura", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_renders_public_content_for_requested_language()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("pt-BR", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.PtBr,
            Title = "Cicloturismo pelo mundo!",
            Body = "Pedale com cultura, saúde e diversão.",
            SeoTitle = "Cicloturismo - Viajantes Turismo"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/?language=pt-BR", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"pt-BR\">", content, StringComparison.Ordinal);
        Assert.Contains("<h1>Cicloturismo pelo mundo!</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_requested_english_language_metadata()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/?culture=en-US", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<html lang=\"en-US\">", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_renders_public_theme_css_variables()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.SetTheme(new PublicThemeSettingsDto
        {
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("--vt-color-primary: #112233;", content, StringComparison.Ordinal);
        Assert.Contains("--vt-font-heading: Inter;", content, StringComparison.Ordinal);
        Assert.Contains("font-family: var(--vt-font-body);", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_default_theme_when_theme_load_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient
        {
            FailThemeRequests = true
        };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("--vt-color-primary: #0F766E;", content, StringComparison.Ordinal);
        Assert.Contains("--vt-font-body: system-ui;", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_default_theme_when_theme_response_is_empty()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient
        {
            ReturnEmptyThemeResponse = true
        };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("--vt-color-primary: #0F766E;", content, StringComparison.Ordinal);
        Assert.Contains("--vt-font-body: system-ui;", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("unsupported")]
    public async Task Root_uses_default_theme_when_theme_response_cannot_be_deserialized(string responseFailure)
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient
        {
            ReturnMalformedThemeResponse = string.Equals(responseFailure, "malformed", StringComparison.Ordinal),
            ReturnUnsupportedThemeResponse = string.Equals(responseFailure, "unsupported", StringComparison.Ordinal)
        };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("--vt-color-primary: #0F766E;", content, StringComparison.Ordinal);
        Assert.Contains("--vt-font-body: system-ui;", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_content_key_when_loading_public_content()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("other.section", "en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Wrong section",
            Body = "This content belongs elsewhere.",
            SeoTitle = "Wrong section - Viajantes Turismo"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cycle tourism around the world!", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Wrong section", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_loads_public_content_and_tours_concurrently()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient
        {
            ContentDelay = TimeSpan.FromSeconds(2),
            ContentStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously),
            ListStarted = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            SeoTitle = "Cycle safely - Viajantes Turismo"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        var responseTask = client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        await catalogApi.ContentStarted.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await catalogApi.ListStarted.Task.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        using var response = await responseTask;
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cycle safely", content, StringComparison.Ordinal);
        Assert.Contains("Camino Norte", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Fake_public_catalog_content_requires_a_culture()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        var content = new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            SeoTitle = "Cycle safely - Viajantes Turismo"
        };

        // Act
        var exception = Assert.Throws<ArgumentException>(() => catalogApi.AddContent(" ", content));

        // Assert
        Assert.Equal("culture", exception.ParamName);
    }

    [Fact]
    public async Task Fake_public_catalog_content_keeps_newline_keys_distinct()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("home\nhero", "en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Newline key",
            Body = "Key-specific content.",
            SeoTitle = "Newline key - Viajantes Turismo"
        });
        catalogApi.AddContent("home", "hero\nen-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Other key",
            Body = "Other content.",
            SeoTitle = "Other key - Viajantes Turismo"
        });

        // Act
        var content = await catalogApi.GetPublicContent(
            "home\nhero",
            "en-US",
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(content);
        Assert.Equal("Newline key", content.Title);
    }

    [Fact]
    public async Task Root_ignores_unsupported_culture_query_and_uses_default_content()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddContent("en-US", new PublicContentVariantDto
        {
            Language = PublicContentLanguageDto.EnUs,
            Title = "Cycle safely",
            Body = "Guided tours for everyone.",
            SeoTitle = "Cycle safely - Viajantes Turismo"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/?culture=fr-FR", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Cycle safely</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_still_renders_tours_when_public_content_load_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailContentRequests = true };
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Cycle tourism around the world!", content, StringComparison.Ordinal);
        Assert.Contains("<h3><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h3>", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Tours could not be loaded", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_returns_unavailable_message_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tours could not be loaded right now. Try again later.", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/group-bike-tours", "Group Bike Tours")]
    [InlineData("/gallery", "Gallery")]
    public async Task Public_ssr_routes_return_expected_content(string path, string expectedHeading)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains($"<h1>{expectedHeading}</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_list_returns_published_tours_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h2><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h2>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_list_returns_unavailable_message_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Tours could not be loaded right now. Try again later.", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_tour_content_when_catalog_loads()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Camino Norte</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_unavailable_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailDetailsRequests = true };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Tour unavailable</h1>", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Public_tour_details_returns_not_found_when_tour_is_not_published()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/group-bike-tours/missing-tour", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("<h1>Tour not found</h1>", content, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_health_endpoint_returns_success(string path)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Error_endpoint_returns_problem_response()
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
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Production_root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(environment: "Production");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_default_health_endpoint_is_not_exposed(string path)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(environment: "Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

}
