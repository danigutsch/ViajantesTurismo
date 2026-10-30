using ViajantesTurismo.Catalog.ApiService;
using PandaBear.Testing.Logging;

namespace ViajantesTurismo.Catalog.ApiServiceTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
public sealed class CatalogCacheLogPrivacyTests
{
    [Fact]
    public void Pending_projection_log_omits_the_tour_identifier()
    {
        // Arrange
        var logger = new RecordingLogger<CatalogCacheLogPrivacyTests>();

        // Act
        logger.TourProjectionPending(nameof(TimeoutException));

        // Assert
        var logText = string.Join(
            '|',
            logger.Records.Select(record => record.Message).Concat(logger.Records.SelectMany(record => record.State).Select(value => value.Value?.ToString() ?? string.Empty)));
        logger.Records.SelectMany(record => record.State).ShouldNotContain(value =>
            string.Equals(value.Key, "CatalogTourId", StringComparison.Ordinal));
        logText.ShouldContain(nameof(TimeoutException), StringComparison.Ordinal);
    }
}
