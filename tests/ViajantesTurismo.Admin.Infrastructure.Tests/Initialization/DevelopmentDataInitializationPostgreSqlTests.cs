using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Initialization;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.DatabaseInitializationCapability)]
public sealed class DevelopmentDataInitializationPostgreSqlTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime
{
    private DevelopmentDataInitializationPostgreSqlScenario? scenario;

    private DevelopmentDataInitializationPostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await DevelopmentDataInitializationPostgreSqlScenario.Create(
            fixture,
            TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var currentScenario = scenario;
        scenario = null;

        if (currentScenario is not null)
        {
            await currentScenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task Failure_before_commit_rolls_back_aggregates_and_generated_outbox()
    {
        // Act
        Func<Task> initialize = () => Scenario.InitializeWithFailure(TestContext.Current.CancellationToken);

        // Assert
        _ = await initialize.ShouldThrow<InvalidOperationException>();
        var counts = await Scenario.CountData(TestContext.Current.CancellationToken);
        counts.Tours.ShouldBe(0);
        counts.Customers.ShouldBe(0);
        counts.Bookings.ShouldBe(0);
        counts.Outbox.ShouldBe(0);
    }

    [Fact]
    public async Task Cancellation_before_commit_rolls_back_aggregates_and_generated_outbox()
    {
        // Act
        Func<Task> initialize = () => Scenario.InitializeWithCancellation(TestContext.Current.CancellationToken);

        // Assert
        _ = await initialize.ShouldThrow<OperationCanceledException>();
        var counts = await Scenario.CountData(TestContext.Current.CancellationToken);
        counts.Tours.ShouldBe(0);
        counts.Customers.ShouldBe(0);
        counts.Bookings.ShouldBe(0);
        counts.Outbox.ShouldBe(0);
    }

    [Fact]
    public async Task Successful_retry_after_rollback_completes_all_data_once()
    {
        // Arrange
        Func<Task> firstAttempt = () => Scenario.InitializeWithFailure(TestContext.Current.CancellationToken);
        _ = await firstAttempt.ShouldThrow<InvalidOperationException>();

        // Act
        await Scenario.Initialize(TestContext.Current.CancellationToken);
        await Scenario.Initialize(TestContext.Current.CancellationToken);

        // Assert
        var counts = await Scenario.CountData(TestContext.Current.CancellationToken);
        counts.Tours.ShouldBe(5);
        counts.Customers.ShouldBe(15);
        counts.Bookings.ShouldBe(10);
        counts.Outbox.ShouldBe(5);
    }

    [Fact]
    public async Task Concurrent_initialization_completes_once_without_duplicates()
    {
        // Act
        await Scenario.InitializeConcurrently(TestContext.Current.CancellationToken);

        // Assert
        var counts = await Scenario.CountData(TestContext.Current.CancellationToken);
        counts.Tours.ShouldBe(5);
        counts.Customers.ShouldBe(15);
        counts.Bookings.ShouldBe(10);
        counts.Outbox.ShouldBe(5);
    }
}
