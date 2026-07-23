using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
public sealed class DevelopmentDataInitializerTests
{
    [Fact]
    public async Task Initialize_inserts_tours_customers_and_bookings_when_database_is_empty()
    {
        // Arrange
        await using var scenario = DevelopmentDataInitializerScenario.Create();

        // Act
        await scenario.Initialize(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainDevelopmentData(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Initialize_preserves_the_booking_status_distribution_by_tour()
    {
        // Arrange
        await using var scenario = DevelopmentDataInitializerScenario.Create();

        // Act
        await scenario.Initialize(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainExpectedBookingStatuses(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Initialize_honors_cancellation_without_inserting_partial_data()
    {
        // Arrange
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        Func<Task> initialize = () => scenario.Initialize(cancellation.Token);

        // Assert
        _ = await initialize.ShouldThrow<OperationCanceledException>();
        await scenario.ShouldNotContainDevelopmentData(TestContext.Current.CancellationToken);
    }
}
