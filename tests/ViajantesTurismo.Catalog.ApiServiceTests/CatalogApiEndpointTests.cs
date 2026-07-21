using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SharedKernel.AI;
using SharedKernel.EventSourcing;
using TestTraits = ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure.TestTraits;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.Tours;

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
        (entryPointAssembly).ShouldBeSameAs(CatalogApiMarker.Assembly);
        (markerAssembly).ShouldBeSameAs(entryPointAssembly);
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
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
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
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
        var saved = await response.Content.ReadFromJsonAsync<PublicContentDto>(TestContext.Current.CancellationToken);
        _ = (saved).ShouldNotBeNull();
        (saved.Key).ShouldBe("HOME.HERO");
        (saved.Variants).ShouldContain(variant => variant.Language == PublicContentLanguageDto.PtBr && variant.RequiresHumanReview);
        (saved.PublicationState).ShouldBe("ReviewRequired");
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
        (response.StatusCode).ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = (problem).ShouldNotBeNull();
        (problem.Errors.Keys).ShouldContain(nameof(PublicContentVariantDto.Title));
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
        (response.StatusCode).ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = (problem).ShouldNotBeNull();
        (problem.Errors.Keys).ShouldContain("Variants");
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
        (response.StatusCode).ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = (problem).ShouldNotBeNull();
        (problem.Errors.Keys).ShouldContain(nameof(UpsertPublicContentRequest.Variants));
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
        (response.StatusCode).ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = (problem).ShouldNotBeNull();
        (problem.Errors.Keys).ShouldContain(nameof(UpsertPublicContentRequest.Variants));
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
        (response.StatusCode).ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = (problem).ShouldNotBeNull();
        (problem.Errors.Keys).ShouldContain("Variants");
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
        (enUs.IsSuccess).ShouldBeTrue();
        (ptBr.IsSuccess).ShouldBeTrue();
        var content = EditablePublicContent.Create("home.hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        (content.IsSuccess).ShouldBeTrue();
        var publish = content.Value.Publish();
        (publish.IsSuccess).ShouldBeTrue();
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/content/home.hero?culture=pt-BR", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
        _ = (variant).ShouldNotBeNull();
        (variant.Language).ShouldBe(PublicContentLanguageDto.PtBr);
        (variant.Title).ShouldBe("Bem-vindo");
        (variant.RequiresHumanReview).ShouldBeFalse();
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
        (enUs.IsSuccess).ShouldBeTrue();
        (ptBr.IsSuccess).ShouldBeTrue();
        var content = EditablePublicContent.Create("home/hero", PublicContentLanguage.EnUs, [enUs.Value, ptBr.Value]);
        (content.IsSuccess).ShouldBeTrue();
        var publish = content.Value.Publish();
        (publish.IsSuccess).ShouldBeTrue();
        await publicContentStore.SaveContent(content.Value, TestContext.Current.CancellationToken);

        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), publicContentStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/content/home/hero?culture=en-US", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var variant = await response.Content.ReadFromJsonAsync<PublicContentVariantDto>(TestContext.Current.CancellationToken);

        // Assert
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
        _ = (variant).ShouldNotBeNull();
        (variant.Title).ShouldBe("Welcome");
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
        (writeResponse.StatusCode).ShouldBe(HttpStatusCode.OK);
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
        _ = (variant).ShouldNotBeNull();
        (variant.Language).ShouldBe(PublicContentLanguageDto.PtBr);
        (variant.Title).ShouldBe("Bem-vindo");
        (variant.RequiresHumanReview).ShouldBeFalse();
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
            ExpectedVersion = 1
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        (response.StatusCode).ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(TestContext.Current.CancellationToken);
        _ = (problem).ShouldNotBeNull();
        (problem.Errors.Keys).ShouldContain(nameof(UpsertCatalogTourPresentationRequest.Title));
        (problem.Errors.Keys).ShouldContain(nameof(UpsertCatalogTourPresentationRequest.Slug));
    }

    [Fact]
    public async Task Catalog_media_object_key_upsert_endpoint_is_not_available()
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{imageId}", UriKind.Relative),
            new { },
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0, "jpg")]
    [InlineData(640, "svg")]
    public async Task Public_catalog_media_endpoint_rejects_invalid_renditions_before_loading_media(int width, string format)
    {
        // Arrange
        var imageStore = new TestPublicMediaImageStore();
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), imageStore, new TestMediaObjectStore());
        using var client = factory.CreateClient();
        var imageId = Guid.CreateVersion7();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{imageId}/{width}/{format}", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        imageStore.GetImageCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task Catalog_tour_image_upload_binds_multipart_form_values()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-UPLOAD",
                "Upload Tour",
                "upload-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAD0lEQVR4nGNgSDsDRXAWAEMEBy263W6BAAAAAElFTkSuQmCC"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "tour.png");
        content.Add(new StringContent("Sunset over the mountains"), "altText");
        content.Add(new StringContent("Evening arrival"), "caption");
        content.Add(new StringContent("Viajantes Turismo"), "attribution");
        content.Add(new StringContent("Copyright 2026"), "copyright");

        // Act
        using var response = await client.PostAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var image = await response.Content.ReadFromJsonAsync<CatalogMediaImageDto>(TestContext.Current.CancellationToken);
        image.ShouldNotBeNull();
        image.AltText.ShouldBe("Sunset over the mountains");
        image.Caption.ShouldBe("Evening arrival");
        var storedImage = await mediaStore.GetImage(image.Id, TestContext.Current.CancellationToken);
        storedImage.ShouldNotBeNull();
        storedImage.Attribution.ShouldBe("Viajantes Turismo");
        storedImage.Copyright.ShouldBe("Copyright 2026");
    }

    [Fact]
    public async Task Catalog_tour_image_upload_rejects_missing_alt_text()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-UPLOAD",
                "Upload Tour",
                "upload-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), new TestPublicMediaImageStore());
        using var client = factory.CreateClient();
        using var content = new MultipartFormDataContent();
        using var file = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAD0lEQVR4nGNgSDsDRXAWAEMEBy263W6BAAAAAElFTkSuQmCC"));
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", "tour.png");

        // Act
        using var response = await client.PostAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative),
            content,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Catalog_tour_images_return_opaque_management_preview_urls()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-PREVIEW",
                "Preview Tour",
                "preview-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "preview-640.jpg",
            "sha256:preview",
            "Preview image",
            displayOrder: 0,
            isCover: true);
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        using (document)
        {
            var returnedImage = document.RootElement.EnumerateArray().Single();
            var variant = returnedImage.GetProperty("responsiveVariants").EnumerateArray().Single();

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            returnedImage.GetProperty("id").GetGuid().ShouldBe(image.Id);
            returnedImage.TryGetProperty("sourceObjectKey", out _).ShouldBeFalse();
            variant.TryGetProperty("objectKey", out _).ShouldBeFalse();
            variant.TryGetProperty("uri", out _).ShouldBeFalse();
            variant.GetProperty("width").GetInt32().ShouldBe(640);
        }
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
        var updated = await response.Content.ReadFromJsonAsync<CatalogMediaImageDto>(TestContext.Current.CancellationToken);
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
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
        var tours = await response.Content.ReadFromJsonAsync<TourSummaryDto[]>(TestContext.Current.CancellationToken);
        _ = (tours).ShouldNotBeNull();
        var tour = (tours).ShouldHaveSingleItem();
        (tour.Slug).ShouldBe("camino-norte");
        var image = (tour.Images).ShouldHaveSingleItem();
        (image.Id).ShouldNotBe(Guid.Empty);
        var rendition = image.ResponsiveVariants.ShouldHaveSingleItem();
        rendition.Width.ShouldBe(640);
        rendition.ContentType.ShouldBe("image/jpeg");
        (image.AltText).ShouldBe("Published image");
    }

    [Fact]
    public async Task Public_tour_list_returns_presentation_summary_without_management_fields()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-PUBLIC",
                "Public Tour",
                "public-tour",
                true,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        var tour = document.RootElement.EnumerateArray().Single();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        tour.GetProperty("title").GetString().ShouldBe("Public Tour");
        tour.TryGetProperty("summary", out _).ShouldBeTrue();
        tour.TryGetProperty("id", out _).ShouldBeFalse();
        tour.TryGetProperty("adminTourId", out _).ShouldBeFalse();
        tour.TryGetProperty("identifier", out _).ShouldBeFalse();
        tour.TryGetProperty("isPublished", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task Catalog_tour_publish_endpoint_requires_an_explicit_transition()
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
        using var presentationResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Draft Tour",
                Slug = "draft-tour",
                Summary = "A publishable draft tour.",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);

        // Act
        using var response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/publish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 2 },
            TestContext.Current.CancellationToken);

        // Assert
        presentationResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Catalog_tour_presentation_rejects_edits_until_a_published_tour_is_unpublished()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-PUBLISHED-EDIT",
                "Published Edit Tour",
                "published-edit-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        using var presentationResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Published Edit Tour",
                Slug = "published-edit-tour",
                Summary = "Ready to publish.",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);
        using var publishResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/publish", UriKind.Relative),
            new CatalogTourPublicationRequest { ExpectedVersion = 2 },
            TestContext.Current.CancellationToken);

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Unapproved Live Edit",
                Slug = "unapproved-live-edit",
                Summary = string.Empty,
                ExpectedVersion = 3
            },
            TestContext.Current.CancellationToken);

        // Assert
        presentationResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        publishResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Catalog_tour_presentation_does_not_commit_when_response_image_enrichment_fails()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-IMAGE-FAILURE",
                "Image Failure Tour",
                "image-failure-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var imageStore = new TestPublicMediaImageStore
        {
            ListByTourException = new InvalidOperationException("Simulated image-store failure.")
        };
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), imageStore);
        using var client = factory.CreateClient();
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = "Updated Tour",
            Slug = "updated-tour",
            Summary = "Updated summary.",
            ExpectedVersion = 1
        };

        // Act
        using var failedResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);
        imageStore.ListByTourException = null;
        using var retryResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        failedResponse.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        retryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Catalog_tour_presentation_normalizes_a_customer_facing_slug()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-SLUG",
                "Slug Tour",
                "slug-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = "Slug Tour",
            Slug = "São Paulo Tour",
            ExpectedVersion = 1
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tour = await response.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        tour.ShouldNotBeNull();
        tour.Slug.ShouldBe("sao-paulo-tour");
    }

    [Fact]
    public async Task Catalog_tour_presentation_rejects_a_stale_expected_version()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-CONCURRENCY",
                "Concurrency Tour",
                "concurrency-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = "Concurrency Tour",
            Slug = "concurrency-tour",
            Summary = "A concurrency-tested tour.",
            ExpectedVersion = 1
        };
        using var firstResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Act
        using var staleResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        staleResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Catalog_tour_presentation_rejects_a_duplicate_slug_without_advancing_the_stream()
    {
        // Arrange
        var firstTourId = Guid.CreateVersion7();
        var secondTourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                firstTourId,
                Guid.CreateVersion7(),
                "TOUR-FIRST",
                "First Tour",
                "tour-first",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                secondTourId,
                Guid.CreateVersion7(),
                "TOUR-SECOND",
                "Second Tour",
                "tour-second",
                false,
                2,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        var firstRequest = new UpsertCatalogTourPresentationRequest
        {
            Title = "First Tour",
            Slug = "shared-tour",
            Summary = "First tour summary.",
            ExpectedVersion = 1
        };
        var duplicateRequest = new UpsertCatalogTourPresentationRequest
        {
            Title = "Second Tour",
            Slug = "shared-tour",
            Summary = "Second tour summary.",
            ExpectedVersion = 1
        };
        using var firstResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{firstTourId}/presentation", UriKind.Relative),
            firstRequest,
            TestContext.Current.CancellationToken);

        // Act
        using var duplicateResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{secondTourId}/presentation", UriKind.Relative),
            duplicateRequest,
            TestContext.Current.CancellationToken);
        using var retryResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{secondTourId}/presentation", UriKind.Relative),
            duplicateRequest with { Slug = "second-tour" },
            TestContext.Current.CancellationToken);

        // Assert
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        duplicateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        retryResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Catalog_tour_presentation_rejects_a_slug_claimed_by_an_unprojected_event()
    {
        // Arrange
        var ownerTourId = Guid.CreateVersion7();
        var ownerAdminTourId = Guid.CreateVersion7();
        var targetTourId = Guid.CreateVersion7();
        var targetAdminTourId = Guid.CreateVersion7();
        var owner = new CatalogTourDraftReadModel(
            ownerTourId,
            ownerAdminTourId,
            "TOUR-OWNER",
            "Owner Tour",
            "owner-tour",
            false,
            1,
            DateTimeOffset.UtcNow);
        var target = new CatalogTourDraftReadModel(
            targetTourId,
            targetAdminTourId,
            "TOUR-TARGET",
            "Target Tour",
            "target-tour",
            false,
            2,
            DateTimeOffset.UtcNow);
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(owner, TestContext.Current.CancellationToken);
        await tourStore.UpsertDraft(target, TestContext.Current.CancellationToken);
        var eventStore = new TestEventStore();
        eventStore.SeedTour(owner);
        eventStore.SeedTour(target);
        _ = await eventStore.Append(
            CatalogTourStreamIds.FromAdminTourId(ownerAdminTourId),
            ExpectedStreamRevision.From(StreamRevision.From(1)),
            [new CatalogTourPresentationChanged(
                ownerTourId,
                "Owner Tour",
                "reserved-tour",
                "Reserved before projection.",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty)],
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), eventStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{targetTourId}/presentation", UriKind.Relative),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Target Tour",
                Slug = "reserved-tour",
                Summary = "Must not claim the reserved slug.",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);
        var targetEvents = await eventStore.Load(
            CatalogTourStreamIds.FromAdminTourId(targetAdminTourId),
            afterRevision: null,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        targetEvents.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("public/tour")]
    [InlineData("public?tour")]
    public async Task Catalog_tour_presentation_rejects_unsafe_slugs(string slug)
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-SLUG",
                "Slug Tour",
                "slug-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore());
        using var client = factory.CreateClient();
        var request = new UpsertCatalogTourPresentationRequest
        {
            Title = "Slug Tour",
            Slug = slug,
            ExpectedVersion = 1
        };

        // Act
        using var response = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/presentation", UriKind.Relative),
            request,
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
        var tour = await response.Content.ReadFromJsonAsync<TourDetailsDto>(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        tour.ShouldNotBeNull();
        tour.Images.ShouldBeEmpty();
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_streams_ready_reviewed_image_for_a_published_tour()
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
        var objectStore = new TestMediaObjectStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "published-640.jpg",
            "sha256:published",
            "Published image",
            displayOrder: 0,
            isCover: true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var expectedContent = "published-media"u8.ToArray();
        await objectStore.Put(
            new MediaObjectWriteRequest("published-640.jpg", new MemoryStream(expectedContent), "image/jpeg", expectedContent.Length, "sha256:published"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        content.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_rejects_a_stored_content_type_that_differs_from_the_rendition()
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
        var objectStore = new TestMediaObjectStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "published-640.jpg",
            "sha256:published",
            "Published image",
            displayOrder: 0,
            isCover: true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("published-640.jpg", new MemoryStream("not-an-image"u8.ToArray()), "text/html", 12, "sha256:published"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_returns_not_found_when_the_object_disappears_before_opening()
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
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "published-640.jpg",
            "sha256:published",
            "Published image",
            displayOrder: 0,
            isCover: true);
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var objectStore = new TestMediaObjectStore { ThrowFileNotFoundOnOpenRead = true };
        await objectStore.Put(
            new MediaObjectWriteRequest("published-640.jpg", new MemoryStream("published-media"u8.ToArray()), "image/jpeg", 15, "sha256:published"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl?.NoStore.ShouldBeTrue();
    }

    [Fact]
    public async Task Management_media_preview_streams_the_selected_rendition_with_nosniff()
    {
        // Arrange
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            Guid.CreateVersion7(),
            "source.jpg",
            "management-640.jpg",
            "sha256:management",
            "Management image",
            displayOrder: 0,
            isCover: true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var expectedContent = "management-media"u8.ToArray();
        await objectStore.Put(
            new MediaObjectWriteRequest("management-640.jpg", new MemoryStream(expectedContent), "image/jpeg", expectedContent.Length, "sha256:management"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/preview/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var content = await response.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        content.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task Management_media_preview_selects_the_exact_width_and_format_rendition()
    {
        // Arrange
        var imageId = Guid.CreateVersion7();
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var imageResult = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = imageId,
                SourceObjectKey = "management-source.jpg",
                Checksum = "sha256:management",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1200, 800),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = "Management image"
            },
            [
                new MediaImageResponsiveVariant("management-640.avif", 640, 427, "image/avif", 640),
                new MediaImageResponsiveVariant("management-640.jpg", 640, 427, "image/jpeg", 640)
            ],
            ["management"],
            [new MediaImageTourLink(Guid.CreateVersion7(), 0, true)]);
        imageResult.IsSuccess.ShouldBeTrue();
        var image = imageResult.Value;
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var avifContent = "management-avif"u8.ToArray();
        var jpegContent = "management-jpeg"u8.ToArray();
        await objectStore.Put(
            new MediaObjectWriteRequest("management-640.avif", new MemoryStream(avifContent), "image/avif", avifContent.Length, "sha256:avif"),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("management-640.jpg", new MemoryStream(jpegContent), "image/jpeg", jpegContent.Length, "sha256:jpeg"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var avifResponse = await client.GetAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/preview/640/avif", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var avifResponseContent = await avifResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        using var jpegResponse = await client.GetAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/preview/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var jpegResponseContent = await jpegResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        avifResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        avifResponse.Content.Headers.ContentType?.MediaType.ShouldBe("image/avif");
        avifResponseContent.ShouldBe(avifContent);
        jpegResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        jpegResponse.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
        jpegResponseContent.ShouldBe(jpegContent);
    }

    [Fact]
    public async Task Management_media_preview_rejects_a_stored_content_type_that_differs_from_the_selected_rendition()
    {
        // Arrange
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            Guid.CreateVersion7(),
            "source.jpg",
            "management-640.jpg",
            "sha256:management",
            "Management image",
            displayOrder: 0,
            isCover: true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("management-640.jpg", new MemoryStream("not-an-image"u8.ToArray()), "text/html", 12, "sha256:management"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(new TestCatalogTourReadModelStore(), mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/preview/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_returns_not_found_without_storage_object()
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
            "source.jpg",
            "missing-640.jpg",
            "sha256:missing",
            "Published image",
            displayOrder: 0,
            isCover: true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, new TestMediaObjectStore());
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_hides_images_linked_only_to_unpublished_tours()
    {
        // Arrange
        var tourId = Guid.CreateVersion7();
        var tourStore = new TestCatalogTourReadModelStore();
        await tourStore.UpsertDraft(
            new CatalogTourDraftReadModel(
                tourId,
                Guid.CreateVersion7(),
                "TOUR-2026",
                "Draft Tour",
                "draft-tour",
                false,
                1,
                DateTimeOffset.UtcNow),
            TestContext.Current.CancellationToken);
        var mediaStore = new TestPublicMediaImageStore();
        var objectStore = new TestMediaObjectStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "draft-640.jpg",
            "sha256:draft",
            "Draft image",
            displayOrder: 0,
            isCover: true);
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("draft-640.jpg", new MemoryStream("draft-media"u8.ToArray()), "image/jpeg", 11, "sha256:draft"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_hides_images_with_unreviewed_accessibility_text()
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
        var objectStore = new TestMediaObjectStore();
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "draft-640.jpg",
            "sha256:draft",
            "Draft image",
            displayOrder: 0,
            isCover: true);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "AI draft image", null).IsSuccess.ShouldBeTrue();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("draft-640.jpg", new MemoryStream("draft-media"u8.ToArray()), "image/jpeg", 11, "sha256:draft"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Headers.CacheControl?.NoStore.ShouldBe(true);
    }

    [Fact]
    public async Task Catalog_media_accessibility_review_promotes_an_ai_draft_to_public_delivery()
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
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "review-640.jpg",
            "sha256:review",
            "Initial image",
            displayOrder: 0,
            isCover: true);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "AI draft", null).IsSuccess.ShouldBeTrue();
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var objectStore = new TestMediaObjectStore();
        var expectedContent = "reviewed-media"u8.ToArray();
        await objectStore.Put(
            new MediaObjectWriteRequest("review-640.jpg", new MemoryStream(expectedContent), "image/jpeg", expectedContent.Length, "sha256:review"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var beforeReview = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var reviewResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-review", UriKind.Relative),
            new PublicMediaImageAccessibilityReviewRequest
            {
                Language = PublicContentLanguageDto.EnUs,
                AltText = "Cyclists riding through a mountain pass.",
                IsDecorative = false
            },
            TestContext.Current.CancellationToken);
        using var afterReview = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var deliveredContent = await afterReview.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        beforeReview.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        reviewResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        afterReview.StatusCode.ShouldBe(HttpStatusCode.OK);
        deliveredContent.ShouldBe(expectedContent);
    }

    [Fact]
    public async Task Catalog_media_accessibility_review_rejects_empty_non_decorative_alt_text_without_publication()
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
        var image = PublicMediaImageTestFactory.CreateReadyImage(
            tourId,
            "source.jpg",
            "review-640.jpg",
            "sha256:review",
            "Initial image",
            displayOrder: 0,
            isCover: true);
        image.SetAiDraftAccessibilityText(PublicContentLanguage.EnUs, "AI draft", null).IsSuccess.ShouldBeTrue();
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var objectStore = new TestMediaObjectStore();
        await objectStore.Put(
            new MediaObjectWriteRequest("review-640.jpg", new MemoryStream("reviewed-media"u8.ToArray()), "image/jpeg", 14, "sha256:review"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var reviewResponse = await client.PutAsJsonAsync(
            new Uri($"/api/v1/catalog/media/images/{image.Id}/accessibility-review", UriKind.Relative),
            new PublicMediaImageAccessibilityReviewRequest
            {
                Language = PublicContentLanguageDto.EnUs,
                AltText = string.Empty,
                IsDecorative = false
            },
            TestContext.Current.CancellationToken);
        using var publicResponse = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var persistedImage = await mediaStore.GetImage(image.Id, TestContext.Current.CancellationToken);

        // Assert
        reviewResponse.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        publicResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        persistedImage.ShouldNotBeNull();
        persistedImage.RequiresHumanReview.ShouldBeTrue();
    }

    [Fact]
    public async Task Public_catalog_media_endpoint_selects_the_exact_id_width_and_format_rendition()
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
        var imageResult = PublicMediaImage.Create(
            new PublicMediaImageMetadata
            {
                Id = Guid.CreateVersion7(),
                SourceObjectKey = "source.jpg",
                Checksum = "sha256:variants",
                ContentType = "image/jpeg",
                FileSizeBytes = 2048,
                Dimensions = new MediaImageDimensions(1280, 853),
                ProcessingStatus = MediaImageProcessingStatus.Ready,
                AltText = "Published image"
            },
            [
                new MediaImageResponsiveVariant("variant-640.avif", 640, 427, "image/avif", 640),
                new MediaImageResponsiveVariant("variant-640.jpg", 640, 427, "image/jpeg", 640)
            ],
            ["catalog"],
            [new MediaImageTourLink(tourId, 0, true)]);
        imageResult.IsSuccess.ShouldBeTrue();
        var image = imageResult.Value;
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(image, TestContext.Current.CancellationToken);
        var objectStore = new TestMediaObjectStore();
        var avifContent = "640-avif"u8.ToArray();
        var smallContent = "640-media"u8.ToArray();
        await objectStore.Put(
            new MediaObjectWriteRequest("variant-640.avif", new MemoryStream(avifContent), "image/avif", avifContent.Length, "sha256:640-avif"),
            TestContext.Current.CancellationToken);
        await objectStore.Put(
            new MediaObjectWriteRequest("variant-640.jpg", new MemoryStream(smallContent), "image/jpeg", smallContent.Length, "sha256:640"),
            TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, mediaStore, objectStore);
        using var client = factory.CreateClient();

        // Act
        using var avifResponse = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/avif", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var avifResponseContent = await avifResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);
        using var jpegResponse = await client.GetAsync(
            new Uri($"/api/v1/public/catalog/media/{image.Id}/640/jpg", UriKind.Relative),
            TestContext.Current.CancellationToken);
        var jpegResponseContent = await jpegResponse.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken);

        // Assert
        avifResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        avifResponse.Content.Headers.ContentType?.MediaType.ShouldBe("image/avif");
        avifResponseContent.ShouldBe(avifContent);
        jpegResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        jpegResponse.Content.Headers.ContentType?.MediaType.ShouldBe("image/jpeg");
        jpegResponseContent.ShouldBe(smallContent);
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
        (response.StatusCode).ShouldBe(HttpStatusCode.OK);
        var tour = await response.Content.ReadFromJsonAsync<TourDetailsDto>(TestContext.Current.CancellationToken);
        _ = (tour).ShouldNotBeNull();
        (tour.Images).ShouldMatchCollection(image =>
            {
                (image.IsCover).ShouldBeTrue();
                (image.Id).ShouldNotBe(Guid.Empty);
                image.ResponsiveVariants.ShouldHaveSingleItem().Width.ShouldBe(640);
            }, image =>
            {
                (image.IsCover).ShouldBeFalse();
                (image.Id).ShouldNotBe(Guid.Empty);
                image.ResponsiveVariants.ShouldHaveSingleItem().Width.ShouldBe(640);
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
        var mediaStore = new TestPublicMediaImageStore();
        await mediaStore.Upsert(PublicMediaImageTestFactory.CreateFailedImage(tourId), TestContext.Current.CancellationToken);
        await using var factory = CatalogApiTestHost.Create(tourStore, new TestPublicContentStore(), mediaStore);
        using var client = factory.CreateClient();

        // Act
        using var publicTourResponse = await client.GetAsync(
            new Uri("/api/v1/public/catalog/tours/camino-norte", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var managementTourResponse = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}", UriKind.Relative),
            TestContext.Current.CancellationToken);
        using var managementImagesResponse = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        var publicTour = await publicTourResponse.Content.ReadFromJsonAsync<TourDetailsDto>(TestContext.Current.CancellationToken);
        _ = (publicTour).ShouldNotBeNull();
        (publicTour.Images).ShouldBeEmpty();
        var managementTour = await managementTourResponse.Content.ReadFromJsonAsync<CatalogTourDto>(TestContext.Current.CancellationToken);
        _ = (managementTour).ShouldNotBeNull();
        (managementTour.Images).ShouldBeEmpty();
        var managementImages = await managementImagesResponse.Content.ReadFromJsonAsync<CatalogMediaImageDto[]>(TestContext.Current.CancellationToken);
        _ = (managementImages).ShouldNotBeNull();
        (managementImages).ShouldHaveSingleItem();
    }

}
