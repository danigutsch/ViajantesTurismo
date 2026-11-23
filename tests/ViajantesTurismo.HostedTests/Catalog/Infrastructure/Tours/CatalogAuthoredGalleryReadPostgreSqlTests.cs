using PandaBear.Testing;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies indexed and paged album reads use the applied Catalog snapshot.</summary>
[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogAuthoredGalleryReadPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task Applied_album_membership_order_and_cover_are_visible_while_the_next_placement_waits()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);
        await using var scenario = await CatalogGalleryIntentPostgreSqlScenario.Create(database.ConnectionString, ct);
        var tour = await scenario.SeedPublishableDraft(ct);
        var photos = Enumerable.Range(0, 27).Select(index => PublicMediaImageTestFactory.CreateReadyImage(
            tour.Id, $"source-{index}.jpg", $"public-{index}.jpg", $"sha256:{index}", $"Photo {index}", index, index == 0)).ToArray();
        scenario.Database.PublicMediaImages.AddRange(photos);
        await scenario.Database.SaveChangesAsync(ct);
        var published = await scenario.Writer.Publish(tour.Id, 2, ct);
        published.IsSuccess.ShouldBeTrue();
        await scenario.Runner.Project(ct);
        Guid[] photoIds = [.. photos.Skip(1).Reverse().Select(photo => photo.Id)];
        var cover = photoIds[^1];
        var placement = await scenario.Writer.ChangeGallery(tour.Id, photoIds, cover, 3, canPublish: true, ct);
        placement.IsSuccess.ShouldBeTrue();
        var waiting = await scenario.Writer.QueueGallery(
            tour.Id, [photos[0].Id], photos[0].Id, 4, 0, canPublish: true, ct);
        waiting.IsSuccess.ShouldBeTrue();

        // Act
        var culture = PublicContentCultures.Supported.Default;
        var index = await scenario.PublicGallery.ListAlbums(culture, null, ct);
        var first = await scenario.PublicGallery.GetAlbum(tour.Slug, culture, null, ct);
        var second = await scenario.PublicGallery.GetAlbum(tour.Slug, culture, "2", ct);

        // Assert
        var summary = index.ShouldNotBeNull().Albums.ShouldHaveSingleItem();
        summary.PhotoCount.ShouldBe(26);
        summary.Cover.Id.ShouldBe(cover);
        summary.Cover.IsCover.ShouldBeTrue();
        first.ShouldNotBeNull().Photos.Select(photo => photo.Id).ShouldBe(photoIds.Take(24));
        var lastPage = second.ShouldNotBeNull();
        lastPage.Photos.Select(photo => photo.Id).ShouldBe(photoIds.Skip(24));
        lastPage.Photos.Where(photo => photo.IsCover).ShouldHaveSingleItem().Id.ShouldBe(cover);
    }
}
