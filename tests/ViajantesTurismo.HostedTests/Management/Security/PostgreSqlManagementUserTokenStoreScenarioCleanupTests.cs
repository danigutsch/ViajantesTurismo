using PandaBear.Testing;
using PostgreSqlTestCleanup = ViajantesTurismo.HostedTests.Infrastructure.HostedTestCleanup;
namespace ViajantesTurismo.Management.WebIntegrationTests;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
[Trait(PandaBear.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
[Trait(PandaBear.Testing.TestTraitNames.AreaName, TestTraits.ManagementWebArea)]
[TestScope(TestScope.Integration)]
public sealed class PostgreSqlManagementUserTokenStoreScenarioCleanupTests
{
    [Fact]
    public async Task Cleanup_attempts_every_resource_in_order_and_preserves_failures()
    {
        // Arrange
        var operationFailure = new InvalidOperationException("setup failed");
        var cleanupFailure = new IOException("cleanup failed");
        var disposalOrder = new List<string>();
        await using var first = new TrackingAsyncDisposable("first", disposalOrder, cleanupFailure);
        await using var second = new TrackingAsyncDisposable("second", disposalOrder);

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
