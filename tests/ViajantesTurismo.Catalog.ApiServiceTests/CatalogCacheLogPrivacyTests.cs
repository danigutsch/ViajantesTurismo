using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, SharedKernel.Testing.TestTraitValues.SecurityCategory)]
public sealed class CatalogCacheLogPrivacyTests
{
    [Fact]
    public void Pending_projection_log_omits_the_tour_identifier()
    {
        // Arrange
        var logger = new CollectingLogger<CatalogCacheLogPrivacyTests>();
        var tourId = Guid.CreateVersion7();

        // Act
        logger.TourProjectionPending(nameof(TimeoutException));

        // Assert
        var logText = string.Join(
            '|',
            logger.Messages.Concat(logger.StructuredValues.Select(value => value.Value?.ToString() ?? string.Empty)));
        logText.ShouldNotContain(tourId.ToString(), StringComparison.OrdinalIgnoreCase);
        logger.StructuredValues.ShouldNotContain(value =>
            string.Equals(value.Key, "CatalogTourId", StringComparison.Ordinal));
        logText.ShouldContain(nameof(TimeoutException), StringComparison.Ordinal);
    }
}
