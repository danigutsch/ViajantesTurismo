using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.DatabaseInitializationCapability)]
public sealed class PostgreSqlTestCleanupTests
{
    [Fact]
    public async Task Cleanup_attempts_every_resource_in_order_and_preserves_failures()
    {
        // Arrange
        var operationFailure = new InvalidOperationException("setup failed");
        var cleanupFailure = new IOException("cleanup failed");
        var disposalOrder = new List<string>();
        await using var first = new AdminTrackingAsyncDisposable("first", disposalOrder, cleanupFailure);
        await using var second = new AdminTrackingAsyncDisposable("second", disposalOrder);

        // Act
        Func<Task> disposeResources = () => PostgreSqlTestCleanup.DisposeResources(
            operationFailure,
            first,
            second);
        var exception = await disposeResources.ShouldThrow<AggregateException>();

        // Assert
        disposalOrder.ShouldBe(["first", "second"]);
        exception.InnerExceptions.Count.ShouldBe(2);
        exception.InnerExceptions[0].ShouldBeSameAs(operationFailure);
        exception.InnerExceptions[1].ShouldBeSameAs(cleanupFailure);
    }
}
