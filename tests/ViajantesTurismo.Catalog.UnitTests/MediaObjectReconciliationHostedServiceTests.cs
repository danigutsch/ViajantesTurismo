using SharedKernel.Testing.Assertions;

namespace ViajantesTurismo.Catalog.UnitTests;

[Trait(SharedKernel.Testing.TestTraitNames.AreaName, TestTraits.CatalogArea)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.ScopeName, SharedKernel.Testing.SharedKernelTestTraitNames.UnitScope)]
[Trait(SharedKernel.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.MediaCapability)]
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

    [Fact]
    public async Task ExecuteBatch_returns_zero_for_recent_orphans_so_polling_drain_stops()
    {
        // Arrange
        await using var scenario = await MediaObjectReconciliationHostedServiceScenario.CreateWithRecentOrphan();

        // Act
        var reconciledObjects = await scenario.ExecuteBatch(TestContext.Current.CancellationToken);

        // Assert
        reconciledObjects.ShouldBe(0);
        scenario.ObjectStore.ObjectKeys.ShouldContain("media/recent-hosted-orphan.jpg");
    }

    [Fact]
    public async Task ExecuteBatch_returns_zero_for_missing_references_so_polling_drain_stops()
    {
        // Arrange
        await using var scenario = MediaObjectReconciliationHostedServiceScenario.CreateWithMissingObject();

        // Act
        var reconciledObjects = await scenario.ExecuteBatch(TestContext.Current.CancellationToken);

        // Assert
        reconciledObjects.ShouldBe(0);
    }
}
