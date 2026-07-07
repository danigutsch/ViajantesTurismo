using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
public sealed class SeederTests
{
    [Fact]
    public async Task Seed_inserts_baseline_tours_customers_and_bookings_when_database_is_empty()
    {
        // Arrange
        await using var scenario = AdminSeederScenario.Create();

        // Act
        await scenario.Seed(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainSeedData(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Seed_does_not_duplicate_data_when_tours_already_exist()
    {
        // Arrange
        await using var scenario = AdminSeederScenario.Create();
        await scenario.AddExistingTour(TestContext.Current.CancellationToken);

        // Act
        await scenario.Seed(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainOnlyExistingTour(TestContext.Current.CancellationToken);
    }
}
