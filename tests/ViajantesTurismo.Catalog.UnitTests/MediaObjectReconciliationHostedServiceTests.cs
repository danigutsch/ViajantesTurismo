using SharedKernel.Testing.Assertions;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class MediaObjectReconciliationHostedServiceTests
{
    [Fact]
    public async Task ExecuteBatch_deletes_old_orphans_and_reports_work_for_polling_drain()
    {
        // Arrange
        await using var scenario = await MediaObjectReconciliationHostedServiceScenario.CreateWithOldOrphan();

        // Act
        var reconciledObjects = await scenario.ExecuteBatch(TestContext.Current.CancellationToken);

        // Assert
        reconciledObjects.ShouldBe(1);
        scenario.ObjectStore.ObjectKeys.ShouldNotContain("media/hosted-orphan.jpg");
    }
}
