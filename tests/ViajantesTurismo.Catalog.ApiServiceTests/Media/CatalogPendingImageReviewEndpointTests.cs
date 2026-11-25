using System.Text.Json;
using SharedKernel.BuildingBlocks;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[TestHost(TestHost.TestServer)]
public sealed class CatalogPendingImageReviewEndpointTests
{
    [Fact]
    public async Task A_large_gallery_returns_one_pending_review_image()
    {
        // Arrange
        const int imageCount = 64;
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(
            tourId, Guid.CreateVersion7(), "REVIEW-TOUR", "Review Tour", "review-tour", false, 1, TimeProvider.System.GetUtcNow()), ct);
        var images = new TestPublicMediaImageStore();
        Guid expected = default;
        for (var index = 0; index < imageCount; index++)
        {
            var image = PublicMediaImageTestFactory.CreateReadyImage(tourId, $"source-{index}.jpg", $"variant-{index}.jpg",
                $"sha256:review-{index}", $"Image {index}", index, isCover: index == 0);
            if (index == imageCount - 1)
            {
                var language = ContentCulture.Restore(PublicContentLanguageTags.PortugueseBrazil);
                image.SetDraftAccessibilityText(language, "Descrição por revisar", caption: null).IsSuccess.ShouldBeTrue();
                expected = image.Id;
            }

            await images.Upsert(image, ct);
        }

        await using var factory = CatalogApiTestHost.Create(tours, new TestPublicContentStore(), images);
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, "Operator");

        // Act
        using var legacy = await client.GetAsync(new Uri($"/api/v1/catalog/tours/{tourId}/images", UriKind.Relative), ct);
        var legacyImages = await legacy.Content.ReadFromJsonAsync<CatalogMediaImageDto[]>(ct);
        using var next = await client.GetAsync(new Uri($"/api/v1/catalog/tours/{tourId}/images/review/next", UriKind.Relative), ct);

        // Assert
        legacy.StatusCode.ShouldBe(HttpStatusCode.OK);
        legacyImages.ShouldNotBeNull().Length.ShouldBe(imageCount);
        next.StatusCode.ShouldBe(HttpStatusCode.OK);
        next.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        using var payload = await next.Content.ReadFromJsonAsync<JsonDocument>(ct);
        var imageId = payload.ShouldNotBeNull().RootElement.GetProperty("image").GetProperty("id").GetGuid();
        imageId.ShouldBe(expected);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Operator")]
    public async Task Catalog_readers_receive_an_empty_queue_when_the_tour_has_no_pending_image(string role)
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(
            tourId, Guid.CreateVersion7(), "EMPTY-REVIEW", "Empty Review", "empty-review", true, 1, TimeProvider.System.GetUtcNow()), ct);
        await using var factory = CatalogApiTestHost.Create(tours, new TestPublicContentStore());
        using var client = factory.CreateClient();
        CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        var catalog = new CatalogToursApiClient(client);

        // Act
        var outcome = await catalog.GetNextPendingReviewImage(tourId, ct);

        // Assert
        outcome.StatusCode.ShouldBe(HttpStatusCode.OK);
        outcome.Value.ShouldNotBeNull().Image.ShouldBeNull();
    }

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("Reader", HttpStatusCode.Forbidden)]
    public async Task Pending_image_review_requires_the_catalog_read_grant(string? role, HttpStatusCode status)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.CreateAnonymous();
        using var client = factory.CreateClient();
        if (role is not null)
        {
            CatalogApiTestHost.ConfigureAuthenticatedClient(client, role);
        }

        // Act
        using var response = await client.GetAsync(
            new Uri($"/api/v1/catalog/tours/{Guid.CreateVersion7()}/images/review/next", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(status);
    }

    [Theory]
    [InlineData(true, HttpStatusCode.BadRequest)]
    [InlineData(false, HttpStatusCode.NotFound)]
    public async Task Review_distinguishes_an_invalid_identifier_from_an_unknown_tour(bool empty, HttpStatusCode status)
    {
        // Arrange
        await using var factory = CatalogApiTestHost.Create();
        using var client = factory.CreateClient();
        var tourId = empty ? Guid.Empty : Guid.CreateVersion7();

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/catalog/tours/{tourId}/images/review/next", UriKind.Relative),
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(status);
    }
}
