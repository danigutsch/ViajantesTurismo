using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class PublicMediaImageConfigurationTests
{
    [Fact]
    public void Enforces_unique_gallery_display_order_and_cover_per_tour()
    {
        // Arrange
        using var dbContext = CatalogDbContextTestFactory.Create();
        var imageEntity = dbContext.Model.FindEntityType(typeof(PublicMediaImage)).ShouldNotBeNull();
        var tourLinks = imageEntity.FindNavigation(nameof(PublicMediaImage.TourLinks)).ShouldNotBeNull().TargetEntityType;

        // Act
        var displayOrderIndex = tourLinks.GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(MediaImageTourLink.CatalogTourId), nameof(MediaImageTourLink.DisplayOrder)]));
        var coverIndex = tourLinks.GetIndexes()
            .Single(index => index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(MediaImageTourLink.CatalogTourId)]));

        // Assert
        displayOrderIndex.IsUnique.ShouldBeTrue();
        coverIndex.IsUnique.ShouldBeTrue();
        coverIndex.FindAnnotation("Relational:Filter")?.Value.ShouldBe("\"IsCover\" = TRUE");
    }
}
