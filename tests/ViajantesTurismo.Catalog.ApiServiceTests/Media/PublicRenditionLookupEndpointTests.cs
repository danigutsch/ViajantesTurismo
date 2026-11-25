using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Media;

/// <summary>Checks bounded public image lookup through the ordinary Catalog HTTP route.</summary>
[Trait(TestTraitNames.CategoryName, TestTraitValues.EndpointCategory)]
[Trait(TestTraitNames.AreaName, TestAreas.Catalog)]
public sealed class PublicRenditionLookupEndpointTests
{
    [Fact]
    public async Task A_public_rendition_uses_a_single_visibility_read_for_widely_shared_media()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tourId = Guid.CreateVersion7();
        var imageId = Guid.CreateVersion7();
        var tours = new TestCatalogTourReadModelStore();
        await tours.UpsertDraft(new CatalogTourDraftReadModel(
            tourId, Guid.CreateVersion7(), "LIVE", "Published tour", "live", true, 1, TimeProvider.System.GetUtcNow())
        {
            GalleryPhotoIds = [imageId]
        }, ct);
        var links = Enumerable.Range(0, 802).Select(_ => new MediaImageTourLink(Guid.CreateVersion7(), 0, true));
        var image = PublicMediaImage.Create(new PublicMediaImageMetadata
        {
            Id = imageId,
            SourceObjectKey = "shared-source.jpg",
            Checksum = "sha256:shared-rendition",
            ContentType = "image/jpeg",
            FileSizeBytes = 12,
            Dimensions = new MediaImageDimensions(640, 427),
            ProcessingStatus = MediaImageProcessingStatus.Ready,
            AltText = "Shared photo"
        }, [new MediaImageResponsiveVariant("shared-640.jpg", 640, 427, "image/jpeg", 12)], [],
            [.. links, new MediaImageTourLink(tourId, 0, true)]).Value;
        var images = new TestPublicMediaImageStore();
        await images.Upsert(image, ct);
        var objects = MediaObjectStoreTestFactory.Create();
        var expected = "shared-photo"u8.ToArray();
        objects.Seed("shared-640.jpg", expected, "image/jpeg", "sha256:shared-rendition");
        await using var factory = CatalogApiTestHost.Create(tours, images, objects);
        factory.UseKestrel(0);
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync(new Uri($"/api/v1/public/catalog/media/{imageId}/640/jpg", UriKind.Relative),
            HttpCompletionOption.ResponseHeadersRead, ct);
        var body = await response.Content.ReadAsByteArrayAsync(ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.CacheControl.ShouldNotBeNull().NoStore.ShouldBeTrue();
        response.Headers.GetValues("X-Content-Type-Options").ShouldHaveSingleItem().ShouldBe("nosniff");
        body.ShouldBe(expected);
        tours.GetTourCallCount.ShouldBe(0);
    }
}
