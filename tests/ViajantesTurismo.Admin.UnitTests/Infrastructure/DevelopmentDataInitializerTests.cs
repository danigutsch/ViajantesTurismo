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
    public async Task Initialize_adds_development_data_when_an_unrelated_tour_already_exists()
    {
        // Arrange
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.AddExistingTour(TestContext.Current.CancellationToken);

        // Act
        await scenario.Initialize(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainExistingTourAndDevelopmentData(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Initialize_does_not_duplicate_data_when_run_again()
    {
        // Arrange
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(TestContext.Current.CancellationToken);

        // Act
        await scenario.Initialize(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainDevelopmentData(TestContext.Current.CancellationToken);
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
