using TestTraits = ViajantesTurismo.Public.WebTests.Infrastructure.TestTraits;

namespace ViajantesTurismo.Public.WebTests;

[Trait(TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(TestTraitNames.HostName, TestTraits.TestServerHost)]
[TestScope(TestScope.Integration)]
public sealed class PublicWebThemeTests
{
    [Fact]
    public async Task Pages_link_the_site_and_theme_stylesheets_without_inline_styles_and_render_the_brand()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(PublicWebThemeTestsHelpers.CreateBranding(
            logoUri: "/api/v1/public/branding/logo/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("<link rel=\"stylesheet\" href=\"_content/PandaBear.SharedKernel.Blazor.Site/site.", StringComparison.Ordinal);
        content.ShouldContain("<link rel=\"stylesheet\" href=\"/theme.css\" />", StringComparison.Ordinal);
        content.ShouldNotContain("<style", StringComparison.OrdinalIgnoreCase);
        content.ShouldContain("<a href=\"#main-content\" class=\"site-skip-link\">Skip to content</a>", StringComparison.Ordinal);
        content.ShouldContain("<main id=\"main-content\" tabindex=\"-1\">", StringComparison.Ordinal);
        content.ShouldContain("<nav class=\"site-nav\" aria-label=\"Main navigation\">", StringComparison.Ordinal);
        content.ShouldContain("Camino Riders", StringComparison.Ordinal);
        content.ShouldContain(
            "<img class=\"public-brand__logo\" src=\"/api/v1/public/branding/logo/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\" alt />",
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Theme_stylesheet_overrides_the_site_tokens_with_the_brand_colours_and_fonts()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(PublicWebThemeTestsHelpers.CreateBranding());
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/theme.css", UriKind.Relative), TestContext.Current.CancellationToken);
        var stylesheet = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("text/css");
        stylesheet.ShouldContain("--site-color-primary: light-dark(#112233, color-mix(in srgb, #112233 55%, white));", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-color-accent: light-dark(#445566, color-mix(in srgb, #445566 55%, white));", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-color-surface: light-dark(#FFFFFF, #111827);", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-color-text: light-dark(#000000, #f3f4f6);", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-font-heading: Inter;", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-font-body: Verdana;", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/images/\u0001logo.svg")]
    [InlineData("https://cdn.example/logo.svg")]
    [InlineData("http://cdn.example/logo.svg")]
    [InlineData("https://cdn.example/\u0001logo.svg")]
    [InlineData("/images\\logo.svg")]
    [InlineData("//cdn.example/logo.svg")]
    [InlineData("https:///logo.svg")]
    [InlineData("https://user:pass@cdn.example/logo.svg")]
    public async Task Pages_omit_an_unsafe_branding_logo(string candidate)
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(PublicWebThemeTestsHelpers.CreateBranding(logoUri: candidate));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldNotContain("public-brand__logo", StringComparison.Ordinal);
        content.ShouldContain("<span class=\"public-brand__mark\" aria-hidden=\"true\">CR</span>", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Theme_stylesheet_canonicalizes_allowed_font_casing()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(PublicWebThemeTestsHelpers.CreateBranding(headingFont: "inter", bodyFont: "verdana"));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/theme.css", UriKind.Relative), TestContext.Current.CancellationToken);
        var stylesheet = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        stylesheet.ShouldContain("--site-font-heading: Inter;", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-font-body: Verdana;", StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("failed")]
    [InlineData("empty")]
    [InlineData("malformed")]
    [InlineData("unsupported")]
    public async Task Theme_stylesheet_and_pages_use_the_default_branding_when_branding_cannot_be_read(string failure)
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient
        {
            FailRequests = string.Equals(failure, "failed", StringComparison.Ordinal),
            ReturnEmptyResponse = string.Equals(failure, "empty", StringComparison.Ordinal),
            ReturnMalformedResponse = string.Equals(failure, "malformed", StringComparison.Ordinal),
            ReturnUnsupportedResponse = string.Equals(failure, "unsupported", StringComparison.Ordinal)
        };
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var stylesheetResponse = await client.GetAsync(new Uri("/theme.css", UriKind.Relative), TestContext.Current.CancellationToken);
        var stylesheet = await stylesheetResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var pageResponse = await client.GetAsync(new Uri("/en/", UriKind.Relative), TestContext.Current.CancellationToken);
        var page = await pageResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        stylesheetResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        stylesheet.ShouldContain("--site-color-primary: light-dark(#0F766E,", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-font-heading: Georgia;", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-font-body: system-ui;", StringComparison.Ordinal);
        pageResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        page.ShouldContain("Viajantes Turismo", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Theme_stylesheet_rejects_unsafe_branding_values_and_uses_safe_fallbacks()
    {
        // Arrange
        var brandingApi = new FakeBrandingApiClient();
        brandingApi.SetBranding(PublicWebThemeTestsHelpers.CreateBranding(
            primaryColor: "url(javascript:alert(1))",
            headingFont: "bad;font"));
        await using var factory = PublicWebEndpointTestsHelpers.CreateFactory(brandingApiClient: brandingApi);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/theme.css", UriKind.Relative), TestContext.Current.CancellationToken);
        var stylesheet = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        stylesheet.ShouldContain("--site-color-primary: light-dark(#0F766E,", StringComparison.Ordinal);
        stylesheet.ShouldContain("--site-font-heading: Georgia;", StringComparison.Ordinal);
        stylesheet.ShouldNotContain("javascript", StringComparison.Ordinal);
        stylesheet.ShouldNotContain("bad;font", StringComparison.Ordinal);
    }
}
