using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiEndpointTests
{
    [Fact]
    public void Catalog_api_marker_exposes_entry_assembly()
    {
        // Arrange
        var marker = new CatalogApiEntryPoint();

        // Act
        var entryPointAssembly = typeof(CatalogApiEntryPoint).Assembly;
        var markerAssembly = marker.Assembly;

        // Assert
        Assert.Same(CatalogApiMarker.Assembly, entryPointAssembly);
        Assert.Same(entryPointAssembly, markerAssembly);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Default_health_endpoint_returns_success(string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_default_health_endpoint_is_not_exposed(string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create("Production");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // Act
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Public_content_endpoint_saves_review_required_draft()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco", RequiresHumanReview = true });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal("HOME.HERO", saved.Key);
        Assert.Contains(saved.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr && variant.RequiresHumanReview);
        Assert.Equal("ReviewRequired", saved.PublicationState);
    }

    [Fact]
    public async Task Public_theme_endpoint_returns_defaults_when_no_theme_is_configured()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/public/catalog/theme", UriKind.Relative), TestContext.Current.CancellationToken);
        var theme = await response.Content.ReadFromJsonAsync<PublicThemeSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(theme);
        Assert.Equal("#0F766E", theme.PrimaryColor);
        Assert.Equal("Georgia", theme.HeadingFontFamily);
    }

    [Fact]
    public async Task Public_theme_management_endpoint_saves_supported_theme_settings()
    {
        // Arrange
        var store = new TestPublicThemeSettingsStore();
        await using var factory = CatalogApiTestHost.Create(store);
        using var client = factory.CreateClient();
        var request = new PublicThemeSettingsDto
        {
            PrimaryColor = "#112233",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        using var writeResponse = await client.PutAsJsonAsync(
            new Uri("/catalog/public-theme", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var readResponse = await client.GetAsync(new Uri("/public/catalog/theme", UriKind.Relative), TestContext.Current.CancellationToken);
        var theme = await readResponse.Content.ReadFromJsonAsync<PublicThemeSettingsDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.NotNull(theme);
        Assert.Equal("#112233", theme.PrimaryColor);
        Assert.Equal("Inter", theme.HeadingFontFamily);
    }

    [Fact]
    public async Task Public_theme_management_endpoint_rejects_unsafe_values()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new PublicThemeSettingsDto
        {
            PrimaryColor = "red; background:url(javascript:alert(1))",
            AccentColor = "#445566",
            BackgroundColor = "#FFFFFF",
            TextColor = "#000000",
            HeadingFontFamily = "Inter",
            BodyFontFamily = "Verdana"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-theme", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicThemeSettingsDto.PrimaryColor), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_body_is_invalid()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = string.Empty, Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicContentVariantDto.Title), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_variant_language_is_duplicated()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Variants", problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_variants_is_null()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        using var content = new StringContent(
            """
            { "sourceLanguage": 1, "variants": null }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertPublicContentRequest.Variants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_variant_element_is_null()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        using var content = new StringContent(
            """
            {
              "sourceLanguage": 1,
              "variants": [
                null,
                { "language": 2, "title": "Bem-vindo", "body": "Pedale conosco" }
              ]
            }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertPublicContentRequest.Variants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_endpoint_returns_validation_problem_when_supported_language_is_missing()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains("Variants", problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_content_read_endpoint_returns_requested_approved_variant()
    {
        // Arrange
        var publicContentStore = new TestPublicContentStore();
        var enUs = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            "Welcome",
            "Ride with us",
            "Cycle tours",
            null,
            null,
            requiresHumanReview: false);
        var ptBr = PublicContentVariant.Create(
            PublicContentLanguage.PtBr,
            "Bem-vindo",
            "Pedale conosco",
            "Cicloturismo",
            null,
            null,
            requiresHumanReview: false);
        Assert.True(enUs.IsSuccess);
        Assert.True(ptBr.IsSuccess);
        var content = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        Assert.True(content.IsSuccess);
        var publish = content.Value.Publish();
        Assert.True(publish.IsSuccess);
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(variant);
        Assert.Equal(PublicContentLanguageDto.PtBr, variant.Language);
        Assert.Equal("Bem-vindo", variant.Title);
        Assert.False(variant.RequiresHumanReview);
    }

    [Fact]
    public async Task Public_content_read_endpoint_supports_slashes_in_content_key()
    {
        // Arrange
        var publicContentStore = new TestPublicContentStore();
        var enUs = PublicContentVariant.Create(
            PublicContentLanguage.EnUs,
            "Welcome",
            "Ride with us",
            null,
            null,
            null,
            requiresHumanReview: false);
        var ptBr = PublicContentVariant.Create(
            PublicContentLanguage.PtBr,
            "Bem-vindo",
            "Pedale conosco",
            null,
            null,
            null,
            requiresHumanReview: false);
        Assert.True(enUs.IsSuccess);
        Assert.True(ptBr.IsSuccess);
        var content = EditablePublicContent.Create("home/hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        Assert.True(content.IsSuccess);
        var publish = content.Value.Publish();
        Assert.True(publish.IsSuccess);
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/content/home/hero?culture=en-US", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(variant);
        Assert.Equal("Welcome", variant.Title);
    }

    [Fact]
    public async Task Public_content_write_endpoint_publishes_approved_content_for_public_reads()
    {
        // Arrange
        var publicContentStore = new TestPublicContentStore();
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();
        var request = new UpsertPublicContentRequest
        {
            SourceLanguage = PublicContentLanguageDto.EnUs
        };
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.EnUs, Title = "Welcome", Body = "Ride with us" });
        request.Variants.Add(new PublicContentVariantDto { Language = PublicContentLanguageDto.PtBr, Title = "Bem-vindo", Body = "Pedale conosco" });

        // Act
        using var writeResponse = await client.PutAsJsonAsync(
            new Uri("/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var response = await client.GetAsync(
            new Uri("/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(variant);
        Assert.Equal(PublicContentLanguageDto.PtBr, variant.Language);
        Assert.Equal("Bem-vindo", variant.Title);
        Assert.False(variant.RequiresHumanReview);
    }

    [Fact]
    public async Task Catalog_tour_presentation_endpoint_returns_validation_problem_when_values_are_too_long()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = new string('t', ContractConstants.MaxNameLength + 1),
            Slug = new string('s', ContractConstants.MaxSlugLength + 1),
            IsPublished = true
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/catalog/tours/{Guid.CreateVersion7()}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(UpsertCatalogTourPresentationRequest.Title), problem.Errors.Keys);
        Assert.Contains(nameof(UpsertCatalogTourPresentationRequest.Slug), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_tour_image_endpoints_save_ordered_images_and_include_them_in_tour_dto()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Camino Norte",
                "camino-norte",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        var request = new PublicMediaImageDto
        {
            Id = imageId,
            SourceUri = new Uri("https://cdn.example/source.jpg"),
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensionsDto { Width = 1200, Height = 800 },
            ProcessingStatus = MediaImageProcessingStatusDto.Ready,
            ResponsiveVariants =
            [
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 },
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-320.jpg"), Width = 320, Height = 213, ContentType = "image/jpeg", FileSizeBytes = 512 }
            ],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = tourId, DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image",
            Caption = "Mountain pass"
        };

        // Act
        using var upsertResponse = await client.PutAsJsonAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var tourResponse = await client.GetAsync(
            new Uri($"/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, tourResponse.StatusCode);
        var tour = await tourResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(tour);
        var image = Assert.Single(tour.Images);
        Assert.Equal("https://cdn.example/one-640.jpg", image.Uri.ToString());
        Assert.True(image.IsCover);
        Assert.Equal([320, 640], image.ResponsiveVariants.Select(variant => variant.Width));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_metadata_that_exceeds_contract_limits()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        var tooLongContentType = new string('x', ContractConstants.MaxContentTypeLength + 1);
        var request = new PublicMediaImageDto
        {
            Id = imageId,
            SourceUri = new Uri("https://cdn.example/source.jpg"),
            Checksum = new string('a', ContractConstants.MaxChecksumLength + 1),
            ContentType = tooLongContentType,
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensionsDto { Width = 1200, Height = 800 },
            ProcessingStatus = MediaImageProcessingStatusDto.Ready,
            ResponsiveVariants =
            [
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = tooLongContentType, FileSizeBytes = 0 }
            ],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicMediaImageDto.Checksum), problem.Errors.Keys);
        Assert.Contains(nameof(PublicMediaImageDto.ContentType), problem.Errors.Keys);
        Assert.Contains(nameof(PublicMediaImageDto.ResponsiveVariants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_non_http_image_uris()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        var request = new PublicMediaImageDto
        {
            Id = imageId,
            SourceUri = new Uri("file:///tmp/source.jpg"),
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensionsDto { Width = 1200, Height = 800 },
            ProcessingStatus = MediaImageProcessingStatusDto.Ready,
            ResponsiveVariants =
            [
                new MediaImageResponsiveVariantDto { Uri = new Uri("data:image/gif;base64,R0lGODlhAQABAAAAACw="), Width = 1, Height = 1, ContentType = "image/gif", FileSizeBytes = 35 }
            ],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicMediaImageDto.SourceUri), problem.Errors.Keys);
        Assert.Contains(nameof(PublicMediaImageDto.ResponsiveVariants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_null_responsive_variant_entries()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        using var content = new StringContent(
            $$"""
            {
              "id": "{{imageId}}",
              "sourceUri": "https://cdn.example/source.jpg",
              "checksum": "sha256:abc",
              "contentType": "image/jpeg",
              "fileSizeBytes": 2048,
              "dimensions": { "width": 1200, "height": 800 },
              "processingStatus": 3,
              "responsiveVariants": [null],
              "tags": ["camino"],
              "tourLinks": [{ "catalogTourId": "{{Guid.CreateVersion7()}}", "displayOrder": 1, "isCover": true }],
              "altText": "First image"
            }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicMediaImageDto.ResponsiveVariants), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_tags_that_sanitize_to_blank()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        var request = new PublicMediaImageDto
        {
            Id = imageId,
            SourceUri = new Uri("https://cdn.example/source.jpg"),
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensionsDto { Width = 1200, Height = 800 },
            ProcessingStatus = MediaImageProcessingStatusDto.Ready,
            ResponsiveVariants =
            [
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
            ],
            Tags = ["\u0001"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicMediaImageDto.Tags), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_null_tour_link_entries()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        using var content = new StringContent(
            $$"""
            {
              "id": "{{imageId}}",
              "sourceUri": "https://cdn.example/source.jpg",
              "checksum": "sha256:abc",
              "contentType": "image/jpeg",
              "fileSizeBytes": 2048,
              "dimensions": { "width": 1200, "height": 800 },
              "processingStatus": 3,
              "responsiveVariants": [
                { "uri": "https://cdn.example/one-640.jpg", "width": 640, "height": 427, "contentType": "image/jpeg", "fileSizeBytes": 1024 }
              ],
              "tags": ["camino"],
              "tourLinks": [null],
              "altText": "First image"
            }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicMediaImageDto.TourLinks), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_duplicate_tour_links()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        var tourId = Guid.CreateVersion7();
        var request = new PublicMediaImageDto
        {
            Id = imageId,
            SourceUri = new Uri("https://cdn.example/source.jpg"),
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensionsDto { Width = 1200, Height = 800 },
            ProcessingStatus = MediaImageProcessingStatusDto.Ready,
            ResponsiveVariants =
            [
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
            ],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = tourId, DisplayOrder = 1, IsCover = true },
                new MediaImageTourLinkDto { CatalogTourId = tourId, DisplayOrder = 2, IsCover = false }
            ],
            AltText = "First image"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        Assert.NotNull(problem);
        Assert.Contains(nameof(PublicMediaImageDto.TourLinks), problem.Errors.Keys);
    }

    [Fact]
    public async Task Public_tour_list_returns_only_published_tours_with_ready_public_images()
    {
        // Arrange
        var publishedTourId = Guid.CreateVersion7();
        var draftTourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                publishedTourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Camino Norte",
                "camino-norte",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                draftTourId,
                Guid.CreateVersion7(),
                "TOUR-DRAFT",
                "Draft Tour",
                "draft-tour",
                false,
                2,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(
            new PublicMediaImage(
                new PublicMediaImageMetadata
                {
                    Id = Guid.CreateVersion7(),
                    SourceUri = new Uri("https://private.example/source.jpg"),
                    Checksum = "sha256:abc",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 2048,
                    Dimensions = new MediaImageDimensions(1200, 800),
                    ProcessingStatus = MediaImageProcessingStatus.Ready,
                    AltText = "Published image"
                },
                [new MediaImageResponsiveVariant(new Uri("https://cdn.example/published-640.jpg"), 640, 427, "image/jpeg", 1024)],
                ["camino"],
                [new MediaImageTourLink(publishedTourId, DisplayOrder: 1, IsCover: true)]),
            TestContext.Current.CancellationToken);
        await mediaStore.Upsert(
            new PublicMediaImage(
                new PublicMediaImageMetadata
                {
                    Id = Guid.CreateVersion7(),
                    SourceUri = new Uri("https://private.example/draft-source.jpg"),
                    Checksum = "sha256:def",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 2048,
                    Dimensions = new MediaImageDimensions(1200, 800),
                    ProcessingStatus = MediaImageProcessingStatus.Ready,
                    AltText = "Draft image"
                },
                [new MediaImageResponsiveVariant(new Uri("https://cdn.example/draft-640.jpg"), 640, 427, "image/jpeg", 1024)],
                ["draft"],
                [new MediaImageTourLink(draftTourId, DisplayOrder: 1, IsCover: true)]),
            TestContext.Current.CancellationToken);
        await mediaStore.Upsert(
            new PublicMediaImage(
                new PublicMediaImageMetadata
                {
                    Id = Guid.CreateVersion7(),
                    SourceUri = new Uri("https://private.example/failed-source.jpg"),
                    Checksum = "sha256:ghi",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 2048,
                    Dimensions = new MediaImageDimensions(1200, 800),
                    ProcessingStatus = MediaImageProcessingStatus.Failed,
                    AltText = "Failed image"
                },
                [new MediaImageResponsiveVariant(new Uri("https://cdn.example/failed-640.jpg"), 640, 427, "image/jpeg", 1024)],
                ["failed"],
                [new MediaImageTourLink(publishedTourId, DisplayOrder: 2, IsCover: false)]),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(tours);
        var tour = Assert.Single(tours);
        Assert.Equal("camino-norte", tour.Slug);
        var image = Assert.Single(tour.Images);
        Assert.Equal("https://cdn.example/published-640.jpg", image.Uri.ToString());
        Assert.Equal("Published image", image.AltText);
    }

    [Fact]
    public async Task Public_tour_endpoint_orders_cover_image_before_gallery_images()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Camino Norte",
                "camino-norte",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(
            new PublicMediaImage(
                new PublicMediaImageMetadata
                {
                    Id = Guid.CreateVersion7(),
                    SourceUri = new Uri("https://private.example/gallery-source.jpg"),
                    Checksum = "sha256:abc",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 2048,
                    Dimensions = new MediaImageDimensions(1200, 800),
                    ProcessingStatus = MediaImageProcessingStatus.Ready,
                    AltText = "Gallery image"
                },
                [new MediaImageResponsiveVariant(new Uri("https://cdn.example/gallery-640.jpg"), 640, 427, "image/jpeg", 1024)],
                ["gallery"],
                [new MediaImageTourLink(tourId, DisplayOrder: 0, IsCover: false)]),
            TestContext.Current.CancellationToken);
        await mediaStore.Upsert(
            new PublicMediaImage(
                new PublicMediaImageMetadata
                {
                    Id = Guid.CreateVersion7(),
                    SourceUri = new Uri("https://private.example/cover-source.jpg"),
                    Checksum = "sha256:def",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 2048,
                    Dimensions = new MediaImageDimensions(1200, 800),
                    ProcessingStatus = MediaImageProcessingStatus.Ready,
                    AltText = "Cover image"
                },
                [new MediaImageResponsiveVariant(new Uri("https://cdn.example/cover-640.jpg"), 640, 427, "image/jpeg", 1024)],
                ["cover"],
                [new MediaImageTourLink(tourId, DisplayOrder: 10, IsCover: true)]),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tour = await response.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(tour);
        Assert.Collection(
            tour.Images,
            image =>
            {
                Assert.True(image.IsCover);
                Assert.Equal("https://cdn.example/cover-640.jpg", image.Uri.ToString());
            },
            image =>
            {
                Assert.False(image.IsCover);
                Assert.Equal("https://cdn.example/gallery-640.jpg", image.Uri.ToString());
            });
    }

    [Fact]
    public async Task Public_tour_endpoint_excludes_images_that_are_not_ready()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Camino Norte",
                "camino-norte",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();
        var request = new PublicMediaImageDto
        {
            Id = imageId,
            SourceUri = new Uri("https://cdn.example/source.jpg"),
            Checksum = "sha256:abc",
            ContentType = "image/jpeg",
            FileSizeBytes = 2048,
            Dimensions = new MediaImageDimensionsDto { Width = 1200, Height = 800 },
            ProcessingStatus = MediaImageProcessingStatusDto.Failed,
            ResponsiveVariants =
            [
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/one-640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1024 }
            ],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = tourId, DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image"
        };

        // Act
        using var upsertResponse = await client.PutAsJsonAsync(
            new Uri($"/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var publicTourResponse = await client.GetAsync(
            new Uri("/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var managementImagesResponse = await client.GetAsync(
            new Uri($"/catalog/tours/{tourId}/images", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);
        var publicTour = await publicTourResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(publicTour);
        Assert.Empty(publicTour.Images);
        var managementImages = await managementImagesResponse.Content.ReadFromJsonAsync<CatalogTourImageDto[]>(TestContext.Current.CancellationToken);
        Assert.NotNull(managementImages);
        Assert.Single(managementImages);
    }

    [Fact]
    public async Task Public_tour_endpoint_excludes_ready_images_without_processed_variants()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Camino Norte",
                "camino-norte",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(
            new PublicMediaImage(
                new PublicMediaImageMetadata
                {
                    Id = Guid.CreateVersion7(),
                    SourceUri = new Uri("https://private.example/source.jpg"),
                    Checksum = "sha256:abc",
                    ContentType = "image/jpeg",
                    FileSizeBytes = 2048,
                    Dimensions = new MediaImageDimensions(1200, 800),
                    ProcessingStatus = MediaImageProcessingStatus.Ready,
                    AltText = "First image"
                },
                [],
                ["camino"],
                [new MediaImageTourLink(tourId, DisplayOrder: 1, IsCover: true)]),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var publicTourResponse = await client.GetAsync(
            new Uri("/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        var publicTour = await publicTourResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        Assert.NotNull(publicTour);
        Assert.Empty(publicTour.Images);
    }
}
