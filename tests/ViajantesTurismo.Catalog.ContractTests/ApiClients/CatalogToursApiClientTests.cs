using Microsoft.AspNetCore.Http;
using System.Net;
using CatalogToursApiClientTestsHelpers = SharedKernel.Testing.Contracts.ContractHttpClientTestHelper;
using SharedKernel.Testing.Contracts;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.Http;

namespace ViajantesTurismo.Catalog.ContractTests.ApiClients;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, Infrastructure.TestTraits.ContractCategory)]
public sealed class CatalogToursApiClientTests
{
    [Fact]
    public void Progress_overload_preserves_existing_client_implementations()
    {
        // Arrange
        var parameterTypes = new[] { typeof(Guid), typeof(CatalogTourImageUploadRequest), typeof(IProgress<long>), typeof(CancellationToken) };

        // Act
        var overload = typeof(ICatalogToursApiClient).GetMethod(nameof(ICatalogToursApiClient.UploadTourImage), parameterTypes);

        // Assert
        overload.ShouldNotBeNull();
        overload.IsAbstract.ShouldBeFalse();
    }

    [Fact]
    public async Task GetTours_requests_management_catalog_endpoint_and_skips_null_items()
    {
        // Arrange
        var requestPath = string.Empty;
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                [
                  {
                    "id":"11111111-1111-1111-1111-111111111111",
                    "adminTourId":"22222222-2222-2222-2222-222222222222",
                    "identifier":"TOUR-1",
                    "title":"First tour",
                    "slug":"first-tour",
                    "isPublished":false,
                    "version":1,
                    "images":[],
                    "updatedAt":"2026-06-25T10:00:00+00:00"
                  },
                  null,
                  {
                    "id":"33333333-3333-3333-3333-333333333333",
                    "adminTourId":"44444444-4444-4444-4444-444444444444",
                    "identifier":"TOUR-2",
                    "title":"Second tour",
                    "slug":"second-tour",
                    "isPublished":true,
                    "version":2,
                    "images":[],
                    "updatedAt":"2026-06-25T11:00:00+00:00"
                  }
                ]
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTours(TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/catalog/tours");
        var tours = outcome.Value.ShouldNotBeNull();
        tours.Length.ShouldBe(2);
        tours[0].Slug.ShouldBe("first-tour");
        tours[1].Slug.ShouldBe("second-tour");
    }

    [Fact]
    public async Task GetTours_returns_empty_array_when_catalog_returns_only_nulls()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("[null,null]"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTours(TestContext.Current.CancellationToken);

        // Assert
        var tours = outcome.Value.ShouldNotBeNull();
        tours.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdatePresentation_sends_management_catalog_presentation_request()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var requestBody = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"Updated tour",
                  "slug":"updated-tour",
                  "isPublished":true,
                  "version":2,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UpdatePresentation(
            tourId,
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Updated tour",
                Slug = "updated-tour",
                Summary = "Updated summary",
                Description = "Updated description",
                Itinerary = "Updated itinerary",
                SeoTitle = "Updated SEO title",
                SeoDescription = "Updated SEO description",
                ExpectedVersion = 4
            },
            TestContext.Current.CancellationToken);

        // Assert
        var updated = outcome.Value.ShouldNotBeNull();
        requestMethod.ShouldBe(HttpMethods.Put);
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111/presentation");
        requestBody.ShouldContain("\"summary\":\"Updated summary\"", StringComparison.Ordinal);
        requestBody.ShouldContain("\"expectedVersion\":4", StringComparison.Ordinal);
        requestBody.ShouldNotContain("\"isPublished\"", StringComparison.Ordinal);
        updated.IsPublished.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdatePresentation_surfaces_a_pending_projection_as_accepted()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.Accepted));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Accepted tour",
                Slug = "accepted-tour",
                Summary = "Accepted summary",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.UnexpectedStatus);
        outcome.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Publish_sends_explicit_publication_request()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var requestBody = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.Publish(
            tourId,
            new CatalogTourPublicationRequest { ExpectedVersion = 5 },
            TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111/publish");
        requestBody.ShouldContain("\"expectedVersion\":5", StringComparison.Ordinal);
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.Succeeded);
    }

    [Fact]
    public async Task Unpublish_sends_explicit_publication_request()
    {
        // Arrange
        var requestPath = string.Empty;
        var requestMethod = string.Empty;
        var requestBody = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMethod = request.Method.Method;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.Unpublish(
            tourId,
            new CatalogTourPublicationRequest { ExpectedVersion = 6 },
            TestContext.Current.CancellationToken);

        // Assert
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111/unpublish");
        requestBody.ShouldContain("\"expectedVersion\":6", StringComparison.Ordinal);
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.Succeeded);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Publication_surfaces_a_pending_projection_as_accepted(bool publish)
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            new HttpResponseMessage(HttpStatusCode.Accepted));
        var sut = new CatalogToursApiClient(httpClient);
        var request = new CatalogTourPublicationRequest { ExpectedVersion = 3 };

        // Act
        var outcome = publish
            ? await sut.Publish(Guid.CreateVersion7(), request, TestContext.Current.CancellationToken)
            : await sut.Unpublish(Guid.CreateVersion7(), request, TestContext.Current.CancellationToken);

        // Assert
        outcome.StatusCode.ShouldBe(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task GetTour_requests_management_catalog_details_endpoint()
    {
        // Arrange
        var requestPath = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"Catalog tour",
                  "slug":"catalog-tour",
                  "isPublished":false,
                  "version":1,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTour(tourId, TestContext.Current.CancellationToken);

        // Assert
        var tour = outcome.Value.ShouldNotBeNull();
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111");
        tour.Slug.ShouldBe("catalog-tour");
    }

    [Fact]
    public async Task GetTour_rejects_a_management_payload_with_a_missing_version()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"Missing version",
                  "slug":"missing-version",
                  "isPublished":false,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTour(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.MalformedBody);
    }

    [Fact]
    public async Task GetTour_rejects_a_management_payload_with_a_zero_version()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"11111111-1111-1111-1111-111111111111",
                  "adminTourId":"22222222-2222-2222-2222-222222222222",
                  "identifier":"TOUR-1",
                  "title":"Zero version",
                  "slug":"zero-version",
                  "isPublished":false,
                  "version":0,
                  "images":[],
                  "updatedAt":"2026-06-25T10:00:00+00:00"
                }
                """));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTour(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.MalformedBody);
    }

    [Fact]
    public async Task GetTour_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTour(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetTour_returns_malformed_outcome_when_catalog_returns_success_with_null_body()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTour(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePresentation_returns_null_when_catalog_returns_notfound()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Missing",
                Slug = "missing",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task UpdatePresentation_returns_malformed_outcome_when_catalog_returns_success_with_null_body()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => CatalogToursApiClientTestsHelpers.JsonResponse("null"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UpdatePresentation(
            Guid.CreateVersion7(),
            new UpsertCatalogTourPresentationRequest
            {
                Title = "Missing",
                Slug = "missing",
                ExpectedVersion = 1
            },
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.MalformedBody);
    }

    [Fact]
    public async Task GetTourImages_reads_management_media_without_storage_keys()
    {
        // Arrange
        var requestPath = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                [
                  {
                    "id":"33333333-3333-3333-3333-333333333333",
                    "responsiveVariants":[
                      {
                        "width":640,
                        "height":427,
                        "contentType":"image/jpeg",
                        "fileSizeBytes":640
                      }
                    ],
                    "altText":"Cyclist on a mountain pass",
                    "isDecorative":false,
                    "requiresHumanReview":true,
                    "isAiGenerated":true
                  }
                ]
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTourImages(tourId, TestContext.Current.CancellationToken);

        // Assert
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111/images");
        var images = outcome.Value.ShouldNotBeNull();
        var image = images.ShouldHaveSingleItem();
        image.Id.ShouldBe(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        image.ResponsiveVariants.ShouldHaveSingleItem().Width.ShouldBe(640);
    }

    [Fact]
    public async Task GetTourImages_returns_malformed_body_when_the_payload_contains_a_null_image()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
            CatalogToursApiClientTestsHelpers.JsonResponse("[null]"));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.GetTourImages(Guid.CreateVersion7(), TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.MalformedBody);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetMediaPreview_streams_the_catalog_preview_response()
    {
        // Arrange
        var requestPath = string.Empty;
        var imageId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            var responseContent = new ByteArrayContent("preview"u8.ToArray());
            responseContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = responseContent };
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        await using var preview = await sut.GetMediaPreview(imageId, 640, "jpg", TestContext.Current.CancellationToken);
        using var buffer = new MemoryStream();
        await preview.ShouldNotBeNull().Content.CopyToAsync(buffer, TestContext.Current.CancellationToken);
        var content = buffer.ToArray();

        // Assert
        requestPath.ShouldBe("/api/v1/catalog/media/images/33333333-3333-3333-3333-333333333333/preview/640/jpg");
        content.ShouldBe("preview"u8.ToArray());
    }

    [Fact]
    public async Task UploadTourImage_sends_multipart_metadata_and_reads_created_image()
    {
        // Arrange
        var requestMethod = string.Empty;
        var requestPath = string.Empty;
        var requestMediaType = string.Empty;
        var requestBody = string.Empty;
        var tourId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var content = new MemoryStream("image"u8.ToArray());
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestMethod = request.Method.Method;
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestMediaType = request.Content?.Headers.ContentType?.MediaType ?? string.Empty;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"33333333-3333-3333-3333-333333333333",
                  "responsiveVariants":[],
                  "altText":"Cyclist on a mountain pass",
                  "isDecorative":false,
                  "requiresHumanReview":true,
                  "isAiGenerated":false
                }
                """, HttpStatusCode.Created);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UploadTourImage(
            tourId,
            new CatalogTourImageUploadRequest(content, content.Length, "mountain.jpg", "image/jpeg", "Cyclist on a mountain pass", "Mountain pass", "Photo team", "© Viajantes"),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        var image = outcome.Value.ShouldNotBeNull();
        image.Id.ShouldBe(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        requestMethod.ShouldBe(HttpMethods.Post);
        requestPath.ShouldBe("/api/v1/catalog/tours/11111111-1111-1111-1111-111111111111/images");
        requestMediaType.ShouldBe("multipart/form-data");
        requestBody.ShouldContain("name=file", StringComparison.Ordinal);
        requestBody.ShouldContain("filename=mountain.jpg", StringComparison.Ordinal);
        requestBody.ShouldContain("name=altText", StringComparison.Ordinal);
        requestBody.ShouldContain("Cyclist on a mountain pass", StringComparison.Ordinal);
        requestBody.ShouldContain("name=caption", StringComparison.Ordinal);
        requestBody.ShouldContain("Mountain pass", StringComparison.Ordinal);
        requestBody.ShouldContain("name=attribution", StringComparison.Ordinal);
        requestBody.ShouldContain("Photo team", StringComparison.Ordinal);
        requestBody.ShouldContain("name=copyright", StringComparison.Ordinal);
        requestBody.ShouldContain("© Viajantes", StringComparison.Ordinal);
    }

    [Fact]
    public async Task UploadTourImage_returns_null_when_tour_is_not_found()
    {
        // Arrange
        using var content = new MemoryStream("image"u8.ToArray());
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UploadTourImage(
            Guid.CreateVersion7(),
            new CatalogTourImageUploadRequest(content, content.Length, "missing.jpg", "image/jpeg", "Missing tour"),
            null,
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task UploadTourImage_preserves_local_content_read_failures()
    {
        // Arrange
        const string failureMessage = "The selected image could not be read.";
        using var content = new ThrowingReadStream(new IOException(failureMessage));
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(
            request => ThrowingReadStream.SerializeRequest(request, TestContext.Current.CancellationToken));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        Func<Task> action = async () => await sut.UploadTourImage(
                Guid.CreateVersion7(),
                new CatalogTourImageUploadRequest(content, "unreadable.jpg", "image/jpeg", "Unreadable image"),
                TestContext.Current.CancellationToken)
            .ConfigureAwait(false);

        // Assert
        var exception = await action.ShouldThrow<IOException>();
        exception.Message.ShouldBe(failureMessage);
        content.ReadCount.ShouldBe(1);
        content.IsDisposed.ShouldBeTrue();
    }

    [Fact]
    public async Task UploadTourImage_returns_transport_failure_for_genuine_transport_errors()
    {
        // Arrange
        var requestCount = 0;
        using var content = new MemoryStream("image"u8.ToArray());
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ =>
        {
            requestCount++;
            throw new HttpRequestException(
                HttpRequestError.ConnectionError,
                "The server connection failed.",
                new IOException("The socket closed."));
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.UploadTourImage(
            Guid.CreateVersion7(),
            new CatalogTourImageUploadRequest(content, "image.jpg", "image/jpeg", "Tour image"),
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.TransportFailure);
        outcome.StatusCode.ShouldBeNull();
        outcome.Message.ShouldBe("HTTP transport failed.");
        requestCount.ShouldBe(1);
        content.CanRead.ShouldBeFalse();
    }

    [Fact]
    public async Task UploadTourImage_reports_only_file_bytes_written_to_the_transport()
    {
        // Arrange
        var fileContent = new byte[100];
        var reportedBytes = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var content = new MemoryStream(fileContent, writable: false);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(async (request, ct) =>
        {
            await request.Content.ShouldNotBeNull().CopyToAsync(Stream.Null, ct);
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"33333333-3333-3333-3333-333333333333",
                  "responsiveVariants":[],
                  "altText":"Cyclist on a mountain pass",
                  "isDecorative":false,
                  "requiresHumanReview":true,
                  "isAiGenerated":false
                }
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);
        var progress = new Progress<long>(bytes => reportedBytes.TrySetResult(bytes));

        // Act
        _ = await sut.UploadTourImage(
            Guid.CreateVersion7(),
            new CatalogTourImageUploadRequest(content, fileContent.LongLength, "mountain.jpg", "image/jpeg", "Cyclist on a mountain pass"),
            progress,
            TestContext.Current.CancellationToken);
        var transferredBytes = await reportedBytes.Task.WaitAsync(TestContext.Current.CancellationToken);

        // Assert
        transferredBytes.ShouldBe(fileContent.LongLength);
    }

    [Fact]
    public async Task UploadTourImage_propagates_cancellation_to_the_http_request()
    {
        // Arrange
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var content = new MemoryStream([1, 2, 3], writable: false);
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(async (_, ct) =>
        {
            requestStarted.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var sut = new CatalogToursApiClient(httpClient);
        using var requestCancellation = new CancellationTokenSource();

        // Act
        var upload = sut.UploadTourImage(
            Guid.CreateVersion7(),
            new CatalogTourImageUploadRequest(content, content.Length, "mountain.jpg", "image/jpeg", "Cyclist on a mountain pass"),
            null,
            requestCancellation.Token);
        await requestStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        await requestCancellation.CancelAsync();
        Func<Task> action = async () => await upload;

        // Assert
        _ = await action.ShouldThrow<OperationCanceledException>();
    }

    [Fact]
    public async Task ReviewMediaImageAccessibility_sends_review_request_and_reads_updated_image()
    {
        // Arrange
        var requestMethod = string.Empty;
        var requestPath = string.Empty;
        var requestBody = string.Empty;
        var imageId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(request =>
        {
            requestMethod = request.Method.Method;
            requestPath = request.RequestUri?.PathAndQuery ?? string.Empty;
            requestBody = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty;
            return CatalogToursApiClientTestsHelpers.JsonResponse("""
                {
                  "id":"33333333-3333-3333-3333-333333333333",
                  "responsiveVariants":[],
                  "altText":"Cyclist on a mountain pass",
                  "isDecorative":false,
                  "requiresHumanReview":false,
                  "isAiGenerated":true
                }
                """);
        });
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.ReviewMediaImageAccessibility(
            imageId,
            new PublicMediaImageAccessibilityReviewRequest
            {
                Language = PublicContentLanguageDto.EnUs,
                AltText = "Cyclist on a mountain pass",
                IsDecorative = false
            },
            TestContext.Current.CancellationToken);

        // Assert
        var image = outcome.Value.ShouldNotBeNull();
        image.RequiresHumanReview.ShouldBeFalse();
        requestMethod.ShouldBe(HttpMethods.Put);
        requestPath.ShouldBe("/api/v1/catalog/media/images/33333333-3333-3333-3333-333333333333/accessibility-review");
        requestBody.ShouldContain("Cyclist on a mountain pass", StringComparison.Ordinal);
    }

    [Fact]
    public async Task ReviewMediaImageAccessibility_returns_null_when_image_is_not_found()
    {
        // Arrange
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var outcome = await sut.ReviewMediaImageAccessibility(
            Guid.CreateVersion7(),
            new PublicMediaImageAccessibilityReviewRequest
            {
                Language = PublicContentLanguageDto.EnUs,
                AltText = "Missing image",
                IsDecorative = false
            },
            TestContext.Current.CancellationToken);

        // Assert
        outcome.Kind.ShouldBe(SharedKernel.HttpClients.HttpOutcomeKind.NotFound);
        outcome.Value.ShouldBeNull();
    }

    [Fact]
    public async Task GetMediaPreview_returns_null_and_disposes_the_not_found_response()
    {
        // Arrange
        var content = new TrackingHttpContent();
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = content
        };
        using var httpClient = CatalogToursApiClientTestsHelpers.CreateClient(_ => response);
        var sut = new CatalogToursApiClient(httpClient);

        // Act
        var preview = await sut.GetMediaPreview(Guid.CreateVersion7(), 640, "jpg", TestContext.Current.CancellationToken);

        // Assert
        preview.ShouldBeNull();
        content.IsDisposed.ShouldBeTrue();
    }
}
