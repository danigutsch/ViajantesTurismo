using TestTraits = ViajantesTurismo.Branding.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Branding.ApiService;

namespace ViajantesTurismo.Branding.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingApiEndpointTests
{
    private const string PublicSettingsPath = $"/api/v1/{BrandingRoutes.PublicSettingsPath}";
    private const string ManagementSettingsPath = $"/api/v1/{BrandingRoutes.ManagementSettingsPath}";

    [Fact]
    public void Branding_api_marker_exposes_entry_assembly()
    {
        // Arrange
        var marker = new BrandingApiHostEntryPoint();

        // Act
        var entryPointAssembly = typeof(BrandingApiHostEntryPoint).Assembly;
        var markerAssembly = marker.Assembly;

        // Assert
        entryPointAssembly.ShouldBe(BrandingApiMarker.Assembly);
        markerAssembly.ShouldBe(entryPointAssembly);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_health_endpoint_returns_success(string path)
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Public_branding_endpoint_returns_defaults_when_no_settings_are_saved()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        var settings = await response.Content.ReadFromJsonAsync<BrandingSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull();
        cacheControl.Public.ShouldBeTrue();
        settings.ShouldNotBeNull();
        settings.BrandName.ShouldBe("Viajantes Turismo");
        settings.PrimaryColor.ShouldBe("#0F766E");
        settings.AccentColor.ShouldBe("#F97316");
        settings.BackgroundColor.ShouldBe("#FFFBF5");
        settings.TextColor.ShouldBe("#1F2937");
        settings.HeadingFontFamily.ShouldBe("Georgia");
        settings.BodyFontFamily.ShouldBe("system-ui");
        settings.LogoUri.ShouldBeNull();
    }

    [Fact]
    public async Task Branding_management_endpoint_saves_and_public_endpoint_reads_settings()
    {
        // Arrange
        var store = new TestBrandingSettingsStore();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var request = new BrandingSettingsDto
        {
            BrandName = "Viajantes",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = "https://cdn.example.test/logo.svg"
        };

        // Act
        using var writeResponse = await client.PutAsJsonAsync(
            new Uri(ManagementSettingsPath, UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var readResponse = await client.GetAsync(new Uri(PublicSettingsPath, UriKind.Relative), TestContext.Current.CancellationToken);
        var settings = await readResponse.Content.ReadFromJsonAsync<BrandingSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        writeResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var writeCacheControl = writeResponse.Headers.CacheControl.ShouldNotBeNull();
        writeCacheControl.NoStore.ShouldBeTrue();
        readResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        settings.ShouldNotBeNull();
        settings.BrandName.ShouldBe("Viajantes");
        settings.PrimaryColor.ShouldBe("#112233");
        settings.LogoUri.ShouldBe("https://cdn.example.test/logo.svg");
    }

    [Fact]
    public async Task Branding_management_endpoint_rejects_unsafe_values()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new BrandingSettingsDto
        {
            BrandName = "Viajantes Turismo",
            PrimaryColor = "red; background:url(javascript:alert(1))",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Papyrus",
            BodyFontFamily = "Verdana",
            LogoUri = "javascript:alert(1)"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri(ManagementSettingsPath, UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.PrimaryColor));
        problem.Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.HeadingFontFamily));
        problem.Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.LogoUri));
    }
}
