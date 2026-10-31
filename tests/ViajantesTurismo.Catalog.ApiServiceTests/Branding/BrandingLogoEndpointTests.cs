using ImageMagick;
using PandaBear.MalwareScanning;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Branding.Contracts.Http;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Branding;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class BrandingLogoEndpointTests
{
    private const string PngType = "image/png";

    [Fact]
    public async Task Uploading_a_png_stores_a_webp_and_points_the_settings_at_its_generated_url()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var png = BrandingTestImages.Create(MagickFormat.Png, 64, 32);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, png, PngType, "*");
        var body = await response.Content.ReadFromJsonAsync<BrandingManagementSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = store.Logos.ShouldHaveSingleItem().Value;
        stored.ContentType.ShouldBe("image/webp");
        stored.Content.Length.ShouldBeLessThanOrEqualTo(40 * 1024);
        body.ShouldNotBeNull().LogoUri.ShouldNotBeNull().OriginalString.ShouldBe($"/api/v1/public/branding/logo/{stored.Sha256}");
    }

    /// <summary>Allowed alpha drift (0-255) after re-encoding: lossy WebP keeps alpha lossless, so only rounding is expected.</summary>
    private const double AlphaTolerance = 3;

    [Fact]
    public async Task A_fully_transparent_png_stays_fully_transparent()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var png = BrandingTestImages.CreateTransparentPng(64, 32);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, png, PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = store.Logos.ShouldHaveSingleItem().Value.Content.ToArray();
        BrandingTestImages.HasAlpha(stored).ShouldBeTrue();
        BrandingTestImages.AlphaAt(stored, 0, 0).ShouldBeLessThanOrEqualTo(AlphaTolerance);
        BrandingTestImages.AlphaAt(stored, 32, 16).ShouldBeLessThanOrEqualTo(AlphaTolerance);
    }

    [Fact]
    public async Task A_soft_edged_png_keeps_its_transparent_surround_and_partial_alpha()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var png = BrandingTestImages.CreateSoftEdgedLogoPng(256, 128);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, png, PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = store.Logos.ShouldHaveSingleItem().Value.Content.ToArray();
        BrandingTestImages.HasAlpha(stored).ShouldBeTrue();
        BrandingTestImages.AlphaAt(stored, 0, 0).ShouldBeLessThanOrEqualTo(AlphaTolerance);
        BrandingTestImages.AlphaAt(stored, 128, 64).ShouldBeGreaterThanOrEqualTo(255 - AlphaTolerance);
        var edgeBefore = BrandingTestImages.AlphaAt(png, 192, 64);
        var edgeAfter = BrandingTestImages.AlphaAt(stored, 192, 64);
        edgeBefore.ShouldBeGreaterThan(AlphaTolerance);
        edgeBefore.ShouldBeLessThan(255 - AlphaTolerance);
        Math.Abs(edgeAfter - edgeBefore).ShouldBeLessThanOrEqualTo(AlphaTolerance);
        BrandingTestImages.MaxAlphaDifference(png, stored).ShouldBeLessThanOrEqualTo(AlphaTolerance);
    }

    [Fact]
    public async Task A_large_gradient_logo_with_alpha_fits_the_budget_at_512_pixels()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var png = BrandingTestImages.CreateGradientLogoPng(1024, 512);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, png, PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = store.Logos.ShouldHaveSingleItem().Value;
        BrandingTestImages.HasAlpha(stored.Content.ToArray()).ShouldBeTrue();
        BrandingTestImages.WidthOf(stored.Content.ToArray()).ShouldBe(512u);
    }

    [Fact]
    public async Task A_jpeg_upload_without_alpha_is_stored_as_an_opaque_webp()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var jpeg = BrandingTestImages.Create(MagickFormat.Jpeg, 64, 32);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, jpeg, "image/jpeg", "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = store.Logos.ShouldHaveSingleItem().Value;
        stored.ContentType.ShouldBe("image/webp");
        BrandingTestImages.AlphaAt(stored.Content.ToArray(), 10, 10).ShouldBeGreaterThanOrEqualTo(255 - AlphaTolerance);
    }

    [Fact]
    public async Task An_animated_webp_upload_is_stored_as_a_single_frame()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var apng = BrandingTestImages.CreateAnimatedWebP(32, 32);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, apng, "image/webp", "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = store.Logos.ShouldHaveSingleItem().Value.Content.ToArray();
        BrandingTestImages.FrameCount(stored).ShouldBe(1);
    }

    [Fact]
    public async Task Uploading_the_same_image_twice_keeps_one_content_addressed_logo()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var png = BrandingTestImages.Create(MagickFormat.Png, 64, 32);
        using var first = await BrandingLogoRequests.PutLogo(client, png, PngType, "*");

        // Act
        using var second = await BrandingLogoRequests.PutLogo(client, png, PngType, "*");

        // Assert
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        second.StatusCode.ShouldBe(HttpStatusCode.OK);
        store.Logos.Count.ShouldBe(1);
    }

    [Fact]
    public async Task The_public_logo_is_served_immutable_with_a_strong_etag_and_nosniff()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        using var upload = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 64, 32), PngType, "*");
        var sha256 = store.Logos.Keys.ShouldHaveSingleItem();

        // Act
        using var response = await BrandingLogoRequests.GetLogo(client, sha256);
        var bytes = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("image/webp");
        var cacheControl = response.Headers.CacheControl.ShouldNotBeNull().ToString();
        cacheControl.ShouldContain("immutable", StringComparison.Ordinal);
        cacheControl.ShouldContain("max-age=31536000", StringComparison.Ordinal);
        response.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe($"\"{sha256}\"");
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        bytes.SequenceEqual(store.Logos[sha256].Content.Span).ShouldBeTrue();
    }

    [Fact]
    public async Task A_matching_if_none_match_returns_not_modified()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        using var upload = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 64, 32), PngType, "*");
        var sha256 = store.Logos.Keys.ShouldHaveSingleItem();

        // Act
        using var response = await BrandingLogoRequests.GetLogo(client, sha256, $"\"{sha256}\"");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
    }

    [Theory]
    [InlineData("0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("not-a-hash")]
    [InlineData("ABCDEF0000000000000000000000000000000000000000000000000000000000")]
    public async Task An_unknown_or_malformed_logo_address_is_not_found(string address)
    {
        // Arrange
        await using var factory = BrandingApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.GetLogo(client, address);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task An_upload_without_if_match_requires_a_precondition()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(TestBrandingSettingsStore.Seeded());
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 8, 8), PngType);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task An_upload_with_a_stale_if_match_fails_the_precondition()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(TestBrandingSettingsStore.Seeded());
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 8, 8), PngType, "\"stale\"");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task An_svg_upload_is_refused()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>"u8.ToArray();

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, svg, "image/svg+xml", "*");

        // Assert
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.UnsupportedMediaType);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_upload_whose_bytes_do_not_match_the_declared_type_is_refused()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var jpeg = BrandingTestImages.Create(MagickFormat.Jpeg, 16, 16);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, jpeg, PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_corrupt_image_with_a_valid_signature_is_refused()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        byte[] corrupt = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4, 5, 6, 7, 8];

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, corrupt, PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_image_over_the_dimension_bound_is_refused()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var wide = BrandingTestImages.Create(MagickFormat.Png, 4096, 8);

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, wide, PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_body_over_one_mebibyte_is_refused()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var body = new byte[(1024 * 1024) + 1];
        body[0] = 0xFF;
        body[1] = 0xD8;
        body[2] = 0xFF;

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, body, "image/jpeg", "*");

        // Assert
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_scan_positive_upload_is_refused()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var host = BrandingApiTestHost.Create(store);
        await using var factory = BrandingApiTestHost.WithMalwareScanner(host, new StubMalwareScanner(MalwareScanStatus.Rejected));
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 16, 16), PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task An_unavailable_scanner_returns_service_unavailable()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var host = BrandingApiTestHost.Create(store);
        await using var factory = BrandingApiTestHost.WithMalwareScanner(host, new StubMalwareScanner(MalwareScanStatus.Passed, new InvalidOperationException("down")));
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 16, 16), PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        store.Logos.ShouldBeEmpty();
    }

    [Fact]
    public async Task Settings_put_rejects_a_logo_uri_that_is_not_the_stored_generated_url()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(TestBrandingSettingsStore.Seeded());
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
            LogoUri = "/api/v1/public/branding/logo/0000000000000000000000000000000000000000000000000000000000000000"
        };

        // Act
        using var response = await BrandingManagementRequests.PutSettings(client, request, "*");
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        problem.ShouldNotBeNull().Errors.Keys.ShouldContain(nameof(BrandingSettingsDto.LogoUri));
    }

    [Fact]
    public async Task Settings_put_keeps_the_uploaded_logo_when_the_generated_url_is_sent_back_and_clears_it_when_omitted()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        using var upload = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 16, 16), PngType, "*");
        var uploaded = await upload.Content.ReadFromJsonAsync<BrandingManagementSettingsDto>(TestContext.Current.CancellationToken);
        var request = new BrandingSettingsDto
        {
            BrandName = "Viajantes",
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana",
            LogoUri = uploaded.ShouldNotBeNull().LogoUri?.OriginalString
        };

        // Act
        using var kept = await BrandingManagementRequests.PutSettings(client, request, "*");
        request.LogoUri = null;
        using var cleared = await BrandingManagementRequests.PutSettings(client, request, "*");
        var clearedBody = await cleared.Content.ReadFromJsonAsync<BrandingManagementSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        kept.StatusCode.ShouldBe(HttpStatusCode.OK);
        cleared.StatusCode.ShouldBe(HttpStatusCode.OK);
        clearedBody.ShouldNotBeNull().LogoUri.ShouldBeNull();
    }

    [Fact]
    public async Task Deleting_the_logo_clears_the_logo_uri_and_returns_the_management_settings()
    {
        // Arrange
        var store = TestBrandingSettingsStore.Seeded();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();
        using var upload = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 16, 16), PngType, "*");

        // Act
        using var response = await BrandingLogoRequests.DeleteLogo(client, "*");
        var body = await response.Content.ReadFromJsonAsync<BrandingManagementSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.ShouldNotBeNull().LogoUri.ShouldBeNull();
        store.Settings.ShouldNotBeNull().LogoUri.ShouldBeNull();
    }

    [Fact]
    public async Task Deleting_the_logo_without_if_match_requires_a_precondition()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(TestBrandingSettingsStore.Seeded());
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.DeleteLogo(client);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionRequired);
    }

    [Fact]
    public async Task Deleting_the_logo_with_a_stale_if_match_fails_the_precondition()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(TestBrandingSettingsStore.Seeded());
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.DeleteLogo(client, "\"stale\"");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task Deleting_the_logo_before_revisions_start_returns_service_unavailable()
    {
        // Arrange
        await using var factory = BrandingApiTestHost.Create(new TestBrandingSettingsStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.DeleteLogo(client, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task The_first_ever_upload_before_a_settings_row_exists_returns_service_unavailable()
    {
        // Arrange
        var store = new TestBrandingSettingsStore();
        await using var factory = BrandingApiTestHost.Create(store);
        using var client = factory.CreateClient();

        // Act
        using var response = await BrandingLogoRequests.PutLogo(client, BrandingTestImages.Create(MagickFormat.Png, 16, 16), PngType, "*");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        store.Logos.ShouldBeEmpty();
    }
}
