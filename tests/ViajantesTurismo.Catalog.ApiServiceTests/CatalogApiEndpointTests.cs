using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Http;
using SharedKernel.AI;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.EndpointCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.TestServerHost)]
public sealed class CatalogApiEndpointTests
{
    [Fact]
    public void Catalog_api_marker_exposes_entry_assembly()
    {
        // Arrange
        var marker = new CatalogApiHostEntryPoint();

        // Act
        var entryPointAssembly = typeof(CatalogApiHostEntryPoint).Assembly;
        var markerAssembly = marker.Assembly;

        // Assert
        TestAssert.Same(CatalogApiMarker.Assembly, entryPointAssembly);
        TestAssert.Same(entryPointAssembly, markerAssembly);
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
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/alive")]
    public async Task Production_default_health_endpoint_returns_safe_status_text(string path)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create("Production");
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
    public async Task Robots_txt_disallows_catalog_api_crawling()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/robots.txt", UriKind.Relative), TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/plain");
        response.Content.Headers.ContentType?.CharSet.ShouldBe("utf-8");
        body.ShouldBe("User-agent: *\nDisallow: /");
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(saved);
        TestAssert.Equal("HOME.HERO", saved.Key);
        TestAssert.Contains(saved.Variants, variant => variant.Language == PublicContentLanguageDto.PtBr && variant.RequiresHumanReview);
        TestAssert.Equal("ReviewRequired", saved.PublicationState);
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicContentVariantDto.Title), problem.Errors.Keys);
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains("Variants", problem.Errors.Keys);
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(UpsertPublicContentRequest.Variants), problem.Errors.Keys);
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(UpsertPublicContentRequest.Variants), problem.Errors.Keys);
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains("Variants", problem.Errors.Keys);
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
        TestAssert.True(enUs.IsSuccess);
        TestAssert.True(ptBr.IsSuccess);
        var content = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        TestAssert.True(content.IsSuccess);
        var publish = content.Value.Publish();
        TestAssert.True(publish.IsSuccess);
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = TestAssert.NotNull(variant);
        TestAssert.Equal(PublicContentLanguageDto.PtBr, variant.Language);
        TestAssert.Equal("Bem-vindo", variant.Title);
        TestAssert.False(variant.RequiresHumanReview);
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
        TestAssert.True(enUs.IsSuccess);
        TestAssert.True(ptBr.IsSuccess);
        var content = EditablePublicContent.Create("home/hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        TestAssert.True(content.IsSuccess);
        var publish = content.Value.Publish();
        TestAssert.True(publish.IsSuccess);
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/content/home/hero?culture=en-US", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = TestAssert.NotNull(variant);
        TestAssert.Equal("Welcome", variant.Title);
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
            new Uri("/api/v1/catalog/public-content/home.hero", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, writeResponse.StatusCode);
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
        _ = TestAssert.NotNull(variant);
        TestAssert.Equal(PublicContentLanguageDto.PtBr, variant.Language);
        TestAssert.Equal("Bem-vindo", variant.Title);
        TestAssert.False(variant.RequiresHumanReview);
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
            new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(UpsertCatalogTourPresentationRequest.Title), problem.Errors.Keys);
        TestAssert.Contains(nameof(UpsertCatalogTourPresentationRequest.Slug), problem.Errors.Keys);
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var tourResponse = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);
        TestAssert.Equal(HttpStatusCode.OK, tourResponse.StatusCode);
        var tour = await tourResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(tour);
        var image = TestAssert.ExactlyOne(tour.Images);
        TestAssert.Equal("https://cdn.example/one-640.jpg", image.Uri.ToString());
        TestAssert.True(image.IsCover);
        TestAssert.Equal([320, 640], image.ResponsiveVariants.Select(variant => variant.Width));
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicMediaImageDto.Checksum), problem.Errors.Keys);
        TestAssert.Contains(nameof(PublicMediaImageDto.ContentType), problem.Errors.Keys);
        TestAssert.Contains(nameof(PublicMediaImageDto.ResponsiveVariants), problem.Errors.Keys);
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicMediaImageDto.SourceUri), problem.Errors.Keys);
        TestAssert.Contains(nameof(PublicMediaImageDto.ResponsiveVariants), problem.Errors.Keys);
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.ResponsiveVariants));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_null_responsive_variant_uris()
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
              "responsiveVariants": [{ "uri": null, "width": 640, "height": 427, "contentType": "image/jpeg", "fileSizeBytes": 1024 }],
              "tags": ["camino"],
              "tourLinks": [{ "catalogTourId": "{{Guid.CreateVersion7()}}", "displayOrder": 1, "isCover": true }],
              "altText": "First image"
            }
            """,
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await client.PutAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicMediaImageDto.ResponsiveVariants), problem.Errors.Keys);
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicMediaImageDto.Tags), problem.Errors.Keys);
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicMediaImageDto.TourLinks), problem.Errors.Keys);
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(problem);
        TestAssert.Contains(nameof(PublicMediaImageDto.TourLinks), problem.Errors.Keys);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_null_accessibility_texts()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image",
            AccessibilityTexts = null!
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_duplicate_accessibility_text_languages()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image",
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, AltText = "First image", IsAiGenerated = false, RequiresHumanReview = false },
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, AltText = "Second image", IsAiGenerated = false, RequiresHumanReview = false }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_inconsistent_accessibility_text_review_state()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image",
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, AltText = "Draft image", IsAiGenerated = true, RequiresHumanReview = false }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_ai_decorative_accessibility_text()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = string.Empty,
            IsDecorative = true,
            RequiresHumanReview = true,
            IsAiGenerated = true,
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, IsDecorative = true, IsAiGenerated = true, RequiresHumanReview = true }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_default_accessibility_text_mismatch()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image",
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, AltText = "Second image", IsAiGenerated = false, RequiresHumanReview = false }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_default_accessibility_caption_mismatch()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image",
            Caption = "Top-level caption",
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, AltText = "First image", Caption = "Localized caption", IsAiGenerated = false, RequiresHumanReview = false }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_default_accessibility_state_mismatch()
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
            ProcessingStatus = MediaImageProcessingStatusDto.Pending,
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = Guid.CreateVersion7(), DisplayOrder = 1, IsCover = true }
            ],
            AltText = string.Empty,
            IsDecorative = true,
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, IsDecorative = false, IsAiGenerated = false, RequiresHumanReview = false }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.AccessibilityTexts));
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_accepts_manual_draft_accessibility_text_requiring_review()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-DRAFT",
                "Draft Tour",
                "draft-tour",
                false,
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
                new MediaImageResponsiveVariantDto { Uri = new Uri("https://cdn.example/640.jpg"), Width = 640, Height = 427, ContentType = "image/jpeg", FileSizeBytes = 1000 }
            ],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = tourId, DisplayOrder = 1, IsCover = true }
            ],
            AltText = "Editor draft image",
            RequiresHumanReview = true,
            AccessibilityTexts =
            [
                new PublicMediaAccessibilityTextDto { Language = PublicContentLanguageDto.EnUs, AltText = "Editor draft image", IsAiGenerated = false, RequiresHumanReview = true }
            ]
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var image = await response.Content.ReadFromJsonAsync<PublicMediaImageDto>(TestContext.Current.CancellationToken);
        image.ShouldNotBeNull();
        image.RequiresHumanReview.ShouldBeTrue();
        image.IsAiGenerated.ShouldBeFalse();
        image.AccessibilityTexts.ShouldHaveSingleItem().RequiresHumanReview.ShouldBeTrue();
    }

    [Fact]
    public async Task Catalog_media_image_accessibility_draft_endpoint_stores_ai_draft()
    {
        // Arrange
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Generated beach alt", "Generated caption"));
        var image = PublicMediaImageTestFactory.CreateReadyImage(Guid.CreateVersion7(), "draft-source.jpg", "draft-640.jpg", "sha256:abc", "Reviewed alt", 0, true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1, 2, 3]), "image/jpeg", 3, "sha256:abc"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(mediaStore, objectStore, generator);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-draft", UriKind.Relative),
            new PublicMediaImageAccessibilityDraftRequest { Language = PublicContentLanguageDto.EnUs, Context = "Hero image" },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PublicMediaImageDto>(TestContext.Current.CancellationToken);
        updated.ShouldNotBeNull();
        updated.AltText.ShouldBe("Generated beach alt");
        updated.Caption.ShouldBe("Generated caption");
        updated.RequiresHumanReview.ShouldBeTrue();
        updated.IsAiGenerated.ShouldBeTrue();
        generator.Request.ShouldNotBeNull();
        generator.Request.Context.ShouldBe("Hero image");
    }

    [Fact]
    public async Task Catalog_media_image_accessibility_draft_endpoint_rejects_null_body()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync<PublicMediaImageAccessibilityDraftRequest?>(
            new Uri($"/api/v1/catalog/media/images/{imageId}/accessibility-draft", UriKind.Relative),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Catalog_media_image_accessibility_draft_endpoint_rejects_missing_language()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}/accessibility-draft", UriKind.Relative),
            new PublicMediaImageAccessibilityDraftRequest { Language = PublicContentLanguageDto.None },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageAccessibilityDraftRequest.Language));
    }

    [Fact]
    public async Task Catalog_media_image_accessibility_draft_endpoint_rejects_partial_location()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}/accessibility-draft", UriKind.Relative),
            new PublicMediaImageAccessibilityDraftRequest { Language = PublicContentLanguageDto.EnUs, Latitude = -23.55m },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageAccessibilityDraftRequest.Latitude));
    }

    [Fact]
    public async Task Catalog_media_image_accessibility_draft_endpoint_returns_not_found_when_source_object_is_missing()
    {
        // Arrange
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Generated beach alt", null));
        var image = PublicMediaImageTestFactory.CreateReadyImage(Guid.CreateVersion7(), "draft-source.jpg", "draft-640.jpg", "sha256:abc", "Reviewed alt", 0, true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(mediaStore, objectStore, generator);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-draft", UriKind.Relative),
            new PublicMediaImageAccessibilityDraftRequest { Language = PublicContentLanguageDto.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        generator.Request.ShouldBeNull();
    }

    [Fact]
    public async Task Catalog_media_image_accessibility_draft_endpoint_returns_service_unavailable_when_ai_fails()
    {
        // Arrange
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var generator = new StubImageTextGenerator(new ImageTextGenerationResult("Generated beach alt", null));
        generator.Throw(new ImageTextGenerationException("Proxy failed."));
        var image = PublicMediaImageTestFactory.CreateReadyImage(Guid.CreateVersion7(), "draft-source.jpg", "draft-640.jpg", "sha256:abc", "Reviewed alt", 0, true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest(image.SourceObjectKey, new MemoryStream([1]), "image/jpeg", 1, "sha256:abc"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(mediaStore, objectStore, generator);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-draft", UriKind.Relative),
            new PublicMediaImageAccessibilityDraftRequest { Language = PublicContentLanguageDto.EnUs },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
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
            PublicMediaImageTestFactory.CreateReadyImage(
                publishedTourId,
                "source.jpg",
                "published-640.jpg",
                "sha256:abc",
                "Published image",
                1,
                true),
            TestContext.Current.CancellationToken);
        await mediaStore.Upsert(
            PublicMediaImageTestFactory.CreateReadyImage(
                draftTourId,
                "draft-source.jpg",
                "draft-640.jpg",
                "sha256:def",
                "Draft image",
                1,
                true),
            TestContext.Current.CancellationToken);
        await mediaStore.Upsert(
            PublicMediaImageTestFactory.CreateFailedImage(publishedTourId),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/public/catalog/tours", UriKind.Relative), TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tours = await response.Content.ReadFromJsonAsync<CatalogTourDto[]>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(tours);
        var tour = TestAssert.ExactlyOne(tours);
        TestAssert.Equal("camino-norte", tour.Slug);
        var image = TestAssert.ExactlyOne(tour.Images);
        TestAssert.Equal("https://cdn.example/published-640.jpg", image.Uri.ToString());
        TestAssert.Equal("Published image", image.AltText);
    }

    [Fact]
    public async Task Public_tour_endpoint_hides_images_with_ai_draft_accessibility_text()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Published Tour",
                "published-tour",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "draft-source.jpg",
            "draft-640.jpg",
            "sha256:draft",
            "Draft image",
            displayOrder: 0,
            isCover: true);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "AI draft image", null).IsSuccess.ShouldBeTrue();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri("/api/v1/public/catalog/tours/published-tour", UriKind.Relative), TestContext.Current.CancellationToken);
        var tour = await response.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        tour.ShouldNotBeNull();
        tour.Images.ShouldBeEmpty();
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
            PublicMediaImageTestFactory.CreateReadyImage(
                tourId,
                "gallery-source.jpg",
                "gallery-640.jpg",
                "sha256:abc",
                "Gallery image",
                0,
                false),
            TestContext.Current.CancellationToken);
        await mediaStore.Upsert(
            PublicMediaImageTestFactory.CreateReadyImage(
                tourId,
                "cover-source.jpg",
                "cover-640.jpg",
                "sha256:def",
                "Cover image",
                10,
                true),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tour = await response.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(tour);
        TestAssert.Collection(
            tour.Images,
            image =>
            {
                TestAssert.True(image.IsCover);
                TestAssert.Equal("https://cdn.example/cover-640.jpg", image.Uri.ToString());
            },
            image =>
            {
                TestAssert.False(image.IsCover);
                TestAssert.Equal("https://cdn.example/gallery-640.jpg", image.Uri.ToString());
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
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        using var publicTourResponse = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var managementImagesResponse = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        TestAssert.Equal(HttpStatusCode.OK, upsertResponse.StatusCode);
        var publicTour = await publicTourResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(publicTour);
        TestAssert.Empty(publicTour.Images);
        var managementImages = await managementImagesResponse.Content.ReadFromJsonAsync<CatalogTourImageDto[]>(TestContext.Current.CancellationToken);
        _ = TestAssert.NotNull(managementImages);
        TestAssert.ExactlyOne(managementImages);
    }

    [Fact]
    public async Task Catalog_media_image_endpoint_rejects_ready_images_without_processed_variants()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
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
            ResponsiveVariants = [],
            Tags = ["camino"],
            TourLinks =
            [
                new MediaImageTourLinkDto { CatalogTourId = tourId, DisplayOrder = 1, IsCover = true }
            ],
            AltText = "First image"
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        problem.ShouldNotBeNull();
        problem.Errors.Keys.ShouldContain(nameof(PublicMediaImageDto.ResponsiveVariants));
    }
}
