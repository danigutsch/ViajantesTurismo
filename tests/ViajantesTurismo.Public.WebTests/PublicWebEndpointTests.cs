using Microsoft.AspNetCore.Mvc.Testing;
using System.Xml.Linq;
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        content.ShouldContain("<html lang=\"en\">", StringComparison.Ordinal);
        content.ShouldContain("Viajantes Turismo", StringComparison.Ordinal);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.ShouldContain("New tours will be published soon.", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h3><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h3>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<html lang=\"pt-BR\">", StringComparison.Ordinal);
        content.ShouldContain("<title>Cicloturismo - Viajantes Turismo</title>", StringComparison.Ordinal);
        content.ShouldContain("<h1 id=\"home-hero-title\">Cicloturismo pelo mundo!</h1>", StringComparison.Ordinal);
        content.ShouldContain("Pedale com cultura", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<html lang=\"pt-BR\">", StringComparison.Ordinal);
        content.ShouldContain("<h1 id=\"home-hero-title\">Cicloturismo pelo mundo!</h1>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<html lang=\"en-US\">", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_renders_public_branding_css_variables_and_brand_name()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            LogoUri = "https://cdn.example/logo.svg",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("--vt-color-primary: #112233;", StringComparison.Ordinal);
        content.ShouldContain("--vt-font-heading: Inter;", StringComparison.Ordinal);
        content.ShouldContain("font-family: var(--vt-font-body);", StringComparison.Ordinal);
        content.ShouldContain("class=\"public-hero\"", StringComparison.Ordinal);
        content.ShouldContain("class=\"public-shell-nav\"", StringComparison.Ordinal);
        content.ShouldContain("Camino Riders", StringComparison.Ordinal);
        content.ShouldContain("https://cdn.example/logo.svg", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_renders_root_relative_branding_logo_uri()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            LogoUri = "/images/logo.svg",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("/images/logo.svg", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/images/\u0001logo.svg")]
    [InlineData("https://cdn.example/\u0001logo.svg")]
    [InlineData("/images\\logo.svg")]
    [InlineData("//cdn.example/logo.svg")]
    [InlineData("https:///logo.svg")]
    [InlineData("https://user:pass@cdn.example/logo.svg")]
    public async Task Root_omits_unsafe_branding_logo_uri(string candidate)
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            LogoUri = candidate,
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.Contains("<img class=\"public-shell-logo\"", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task Root_canonicalizes_safe_branding_font_casing()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(new BrandingSettingsDto
        {
            BrandName = "Camino Riders",
            LogoUri = null,
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "inter",
            BodyFontFamily = "verdana"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("--vt-font-heading: Inter;", StringComparison.Ordinal);
        content.ShouldContain("--vt-font-body: Verdana;", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_default_branding_when_branding_load_fails()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient
        {
            FailRequests = true
        };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("--vt-color-primary: #0F766E;", StringComparison.Ordinal);
        content.ShouldContain("--vt-font-body: system-ui;", StringComparison.Ordinal);
        content.ShouldContain("Viajantes Turismo", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_uses_default_branding_when_branding_response_is_empty()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient
        {
            ReturnEmptyResponse = true
        };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("--vt-color-primary: #0F766E;", StringComparison.Ordinal);
        content.ShouldContain("--vt-font-body: system-ui;", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("unsupported")]
    public async Task Root_uses_default_branding_when_branding_response_cannot_be_deserialized(string responseFailure)
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient
        {
            ReturnMalformedResponse = string.Equals(responseFailure, "malformed", StringComparison.Ordinal),
            ReturnUnsupportedResponse = string.Equals(responseFailure, "unsupported", StringComparison.Ordinal)
        };

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("--vt-color-primary: #0F766E;", StringComparison.Ordinal);
        content.ShouldContain("--vt-font-body: system-ui;", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Root_rejects_unsafe_branding_values_and_uses_safe_fallbacks()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(new BrandingSettingsDto
        {
            BrandName = "Unsafe Brand",
            LogoUri = "http://cdn.example/logo.svg",
            PrimaryColor = "url(javascript:alert(1))",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "bad;font",
            BodyFontFamily = "Verdana"
        });

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("--vt-color-primary: #0F766E;", StringComparison.Ordinal);
        content.ShouldContain("--vt-font-heading: Georgia;", StringComparison.Ordinal);
        content.Contains("http://cdn.example/logo.svg", StringComparison.Ordinal).ShouldBeFalse();
        content.ShouldNotContain("javascript:alert(1)", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.Contains("Wrong section", StringComparison.Ordinal).ShouldBeFalse();
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle safely", StringComparison.Ordinal);
        content.ShouldContain("Camino Norte", StringComparison.Ordinal);
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
        Action act = () => catalogApi.AddContent(" ", content);
        var exception = act.ShouldThrow<ArgumentException>();

        // Assert
        exception.ParamName.ShouldBe("culture");
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
        content.ShouldNotBeNull();
        content.Title.ShouldBe("Newline key");
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h1 id=\"home-hero-title\">Cycle safely</h1>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Cycle tourism around the world!", StringComparison.Ordinal);
        content.ShouldContain("<h3><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h3>", StringComparison.Ordinal);
        content.Contains("Tours could not be loaded", StringComparison.Ordinal).ShouldBeFalse();
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
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        content.ShouldContain("Tours could not be loaded right now. Try again later.", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        content.ShouldContain($"<h1>{expectedHeading}</h1>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h2><a href=\"/group-bike-tours/camino-norte\">Camino Norte</a></h2>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        content.ShouldContain("Tours could not be loaded right now. Try again later.", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h1>Camino Norte</h1>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        content.ShouldContain("<h1>Tour unavailable</h1>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<h1>Tour not found</h1>", StringComparison.Ordinal);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
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
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Robots_txt_allows_public_crawling()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory();
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
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
        body.ShouldBe("User-agent: *\nAllow: /\nSitemap: https://localhost:7003/sitemap.xml");
    }

    [Fact]
    public async Task Robots_txt_uses_sitemap_canonical_origin_from_configuration()
    {
        // Arrange
        await using var sourceFactory = PublicWebEndpointTestsHelpers.CreateFactory();
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
    public async Task Sitemap_xml_includes_only_canonical_public_pages()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        var updatedAt = new DateTimeOffset(2026, 7, 11, 9, 30, 0, TimeSpan.Zero);
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("camino-norte", "Camino Norte") with
        {
            UpdatedAt = updatedAt
        });
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("missing-date", "Missing date") with
        {
            UpdatedAt = default
        });
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("invalid/tour", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("invalid\\tour", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("invalid?tour", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("invalid#tour", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("\u0001invalid", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour(".", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour("..", "Invalid tour"));
        catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour(" ", "Invalid tour"));

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://testserver")
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

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/xml");
        locations.ShouldBe([
            "https://localhost:7003/",
            "https://localhost:7003/group-bike-tours",
            "https://localhost:7003/gallery",
            "https://localhost:7003/group-bike-tours/camino-norte",
            "https://localhost:7003/group-bike-tours/missing-date"
        ]);
        lastModified.ShouldBe(["2026-07-11T09:30:00Z"]);
    }

    [Fact]
    public async Task Sitemap_xml_limits_tours_to_protocol_maximum()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient();
        for (var index = 0; index < 49_998; index++)
        {
            catalogApi.AddTour(PublicWebEndpointTestsHelpers.CreateTour($"tour-{index}", "Tour"));
        }

        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/sitemap.xml", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var document = XDocument.Parse(body);
        var sitemapNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var locations = document.Descendants(sitemapNamespace + "loc").Select(element => element.Value).ToArray();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        locations.Length.ShouldBe(50_000);
        locations[^1].ShouldBe("https://localhost:7003/group-bike-tours/tour-49996");
        locations.ShouldNotContain("https://localhost:7003/group-bike-tours/tour-49997");
    }

    [Fact]
    public async Task Sitemap_xml_returns_service_unavailable_when_catalog_fails()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { FailListRequests = true };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/sitemap.xml", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Sitemap_xml_returns_non_cacheable_service_unavailable_when_catalog_cancels_upstream()
    {
        // Arrange
        var catalogApi = new FakePublicCatalogApiClient { ThrowOperationCanceledExceptionOnListRequests = true };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(catalogApi);
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
    public async Task Production_root_returns_public_landing_page()
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(
            environment: "Production",
            canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_default_health_endpoint_returns_safe_status_text(string path)
    {
        // Arrange
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(
            environment: "Production",
            canonicalOrigin: "https://public.example.test");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        body.ShouldBe("Healthy");
    }

    [Fact]
    public void Production_host_fails_startup_when_sitemap_canonical_origin_is_missing()
    {
        // Arrange
        // Act
        var exception = PublicWebEndpointTestsHelpers.GetProductionSitemapValidationException();

        // Assert
        exception.Message.ShouldContain(
            "PublicWeb:Sitemap:CanonicalOrigin must be provided.",
            StringComparison.Ordinal);
    }

    [Fact]
    public void Production_host_fails_startup_when_sitemap_canonical_origin_is_invalid()
    {
        // Arrange
        // Act
        var exception = PublicWebEndpointTestsHelpers.GetProductionSitemapValidationException(
            "https://public.example.test/sitemap.xml");

        // Assert
        exception.Message.ShouldContain(
            "PublicWeb:Sitemap:CanonicalOrigin must be an absolute HTTP or HTTPS origin without a path, query, fragment, or userinfo.",
            StringComparison.Ordinal);
    }

}
