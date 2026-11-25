using SharedKernel.Testing;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Verifies that shared photos cannot expand the placements read for a public photo page.</summary>
[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class CatalogGalleryLinkBoundsPostgreSqlTests(HostedTestFixture shared)
{
    [Fact]
    public async Task A_photo_shared_by_many_tours_only_materializes_the_requested_tour_links()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var database = await shared.Databases.Create(ct);

        // Act
        var read = await CatalogGalleryLinkBoundsProbe.Read(database.ConnectionString, ct);

        // Assert
        read.Index.Albums.Count.ShouldBe(24);
        read.Index.Albums.Select(item => item.Cover.Id).Distinct().ShouldHaveSingleItem().ShouldBe(read.SharedPhoto);
        read.Album.Photos.ShouldHaveSingleItem().Id.ShouldBe(read.SharedPhoto);
        read.IndexLinks.Distinct().Order().ShouldBe(read.ExpectedIndexTours.Order());
        read.AlbumLinks.Distinct().ShouldBe([read.AlbumTour]);
        read.DetailImages.ShouldHaveSingleItem().Id.ShouldBe(read.SharedPhoto);
        read.DetailLinks.Distinct().ShouldBe([read.AlbumTour]);
    }
}
