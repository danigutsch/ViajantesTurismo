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
        await scenario.ShouldContainOnlyTours(1, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Seed_recovers_when_only_baseline_tours_were_committed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = AdminSeederScenario.Create();
        await scenario.Seed(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        await scenario.KeepOnlyBaselineTours(ct);

        // Act
        await scenario.Seed(ct);

        // Assert
        await scenario.ShouldContainSeedData(ct);
        var recoveredTourIds = await scenario.GetTourIds(ct);
        recoveredTourIds.ShouldBe(originalTourIds);
    }

    [Fact]
    public async Task Seed_recovers_when_baseline_tours_and_customers_were_committed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = AdminSeederScenario.Create();
        await scenario.Seed(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);
        await scenario.RemoveBaselineBookings(ct);

        // Act
        await scenario.Seed(ct);

        // Assert
        await scenario.ShouldContainSeedData(ct);
        var recoveredTourIds = await scenario.GetTourIds(ct);
        var recoveredCustomerIds = await scenario.GetCustomerIds(ct);
        recoveredTourIds.ShouldBe(originalTourIds);
        recoveredCustomerIds.ShouldBe(originalCustomerIds);
    }

    [Fact]
    public async Task Seed_is_idempotent_after_the_complete_baseline_exists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = AdminSeederScenario.Create();
        await scenario.Seed(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);

        // Act
        await scenario.Seed(ct);

        // Assert
        await scenario.ShouldContainSeedData(ct);
        var persistedTourIds = await scenario.GetTourIds(ct);
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedTourIds.ShouldBe(originalTourIds);
        persistedCustomerIds.ShouldBe(originalCustomerIds);
    }

    [Fact]
    public async Task Seed_does_not_supplement_data_when_baseline_and_nonbaseline_tours_exist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = AdminSeederScenario.Create();
        await scenario.Seed(ct);
        await scenario.KeepOnlyBaselineTours(ct);
        await scenario.AddExistingTour(ct);
        var existingTourIds = await scenario.GetTourIds(ct);

        // Act
        await scenario.Seed(ct);

        // Assert
        await scenario.ShouldContainOnlyTours(6, ct);
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBe(existingTourIds);
    }
}
