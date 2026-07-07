using SharedKernel.Testing.Assertions;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class CatalogProjectionHostedServiceTests
{
    [Fact]
    public async Task ExecuteBatch_returns_projected_event_count_for_polling_drain()
    {
        // Arrange
        await using var scenario = CatalogProjectionHostedServiceScenario.CreateWithOneEvent();

        // Act
        var projectedEvents = await scenario.ExecuteBatch(TestContext.Current.CancellationToken);

        // Assert
        projectedEvents.ShouldBe(1);
        scenario.ShouldHaveProjectedDraft();
        scenario.ShouldHaveSavedCheckpoint();
    }
}
