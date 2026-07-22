using SharedKernel.Testing;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
public sealed class DevelopmentDataInitializerCheckpointTests
{
    [Fact]
    public async Task Initialize_does_not_duplicate_data_when_tours_already_exist()
    {
        // Arrange
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.AddExistingTour(TestContext.Current.CancellationToken);

        // Act
        await scenario.Initialize(TestContext.Current.CancellationToken);

        // Assert
        await scenario.ShouldContainOnlyTours(1, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Initialize_recovers_when_only_baseline_tours_were_committed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        await scenario.KeepOnlyBaselineTours(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        await scenario.ShouldContainDevelopmentData(ct);
        var recoveredTourIds = await scenario.GetTourIds(ct);
        recoveredTourIds.ShouldBe(originalTourIds);
    }

    [Fact]
    public async Task Initialize_recovers_when_baseline_tours_and_customers_were_committed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);
        await scenario.RemoveBaselineBookings(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        await scenario.ShouldContainDevelopmentData(ct);
        var recoveredTourIds = await scenario.GetTourIds(ct);
        var recoveredCustomerIds = await scenario.GetCustomerIds(ct);
        recoveredTourIds.ShouldBe(originalTourIds);
        recoveredCustomerIds.ShouldBe(originalCustomerIds);
    }

    [Fact]
    public async Task Initialize_recovers_when_pending_baseline_bookings_were_committed_before_lifecycle_updates()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.ShouldContainExpectedDevelopmentBookingStates(ct);
        var expectedBookingStates = await scenario.GetBookingStates(ct);
        await scenario.ResetBaselineBookingsToPendingCheckpoint(ct);
        await scenario.ShouldContainPendingBookingCheckpoint(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        await scenario.ShouldContainDevelopmentData(ct);
        var recoveredBookingStates = await scenario.GetBookingStates(ct);
        recoveredBookingStates.ShouldBe(expectedBookingStates);
    }

    [Fact]
    public async Task Initialize_is_idempotent_after_the_complete_baseline_exists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        await scenario.ShouldContainDevelopmentData(ct);
        var persistedTourIds = await scenario.GetTourIds(ct);
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedTourIds.ShouldBe(originalTourIds);
        persistedCustomerIds.ShouldBe(originalCustomerIds);
    }

    [Fact]
    public async Task Initialize_does_not_supplement_data_when_baseline_and_nonbaseline_tours_exist()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.KeepOnlyBaselineTours(ct);
        await scenario.AddExistingTour(ct);
        var existingTourIds = await scenario.GetTourIds(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        await scenario.ShouldContainOnlyTours(6, ct);
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBe(existingTourIds);
    }

    [Fact]
    public async Task Initialize_does_not_add_tours_when_only_an_unrecognized_customer_checkpoint_exists()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.KeepOnlyOneBaselineCustomer(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBeEmpty();
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedCustomerIds.ShouldBe(originalCustomerIds);
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBeEmpty();
    }

    [Fact]
    public async Task Initialize_does_not_mutate_an_unrecognized_customer_only_database()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.AddUnrecognizedCustomer(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBeEmpty();
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedCustomerIds.ShouldBe(originalCustomerIds);
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBeEmpty();
    }

    [Fact]
    public async Task Initialize_does_not_add_bookings_when_an_extra_customer_invalidates_the_baseline_checkpoint()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.RemoveBaselineBookings(ct);
        await scenario.AddUnrecognizedCustomer(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);
        var originalBookings = await scenario.GetBookingStates(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBe(originalTourIds);
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedCustomerIds.ShouldBe(originalCustomerIds);
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBe(originalBookings);
    }

    [Fact]
    public async Task Initialize_does_not_mutate_a_pending_booking_checkpoint_with_an_unrecognized_booking()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.ResetBaselineBookingsToPendingCheckpoint(ct);
        await scenario.ReplaceBaselineBookingWithArbitraryPendingBooking(ct);
        var originalTourIds = await scenario.GetTourIds(ct);
        var originalCustomerIds = await scenario.GetCustomerIds(ct);
        var originalBookings = await scenario.GetBookingStates(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBe(originalTourIds);
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedCustomerIds.ShouldBe(originalCustomerIds);
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBe(originalBookings);
    }

    [Fact]
    public async Task Initialize_does_not_supplement_baseline_keys_when_tour_pricing_was_changed()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.KeepOnlyBaselineTours(ct);
        await scenario.AlterBaselineTourPrice(ct);
        var originalTourIds = await scenario.GetTourIds(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedTourIds = await scenario.GetTourIds(ct);
        persistedTourIds.ShouldBe(originalTourIds);
        var persistedCustomerIds = await scenario.GetCustomerIds(ct);
        persistedCustomerIds.ShouldBeEmpty();
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBeEmpty();
    }

    [Fact]
    public async Task Initialize_recovers_a_baseline_tour_checkpoint_after_timestamp_precision_rounding()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.KeepOnlyBaselineTours(ct);
        await scenario.AlterBaselineTourSchedulePrecision(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        await scenario.ShouldContainDevelopmentData(ct);
        await scenario.ShouldContainExpectedDevelopmentBookingStates(ct);
    }

    [Fact]
    public async Task Initialize_does_not_complete_a_pending_booking_checkpoint_with_changed_snapshot_pricing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.ResetBaselineBookingsToPendingCheckpoint(ct);
        await scenario.AlterPendingBookingBasePrice(ct);
        var originalBookings = await scenario.GetBookingStates(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBe(originalBookings);
    }

    [Fact]
    public async Task Initialize_does_not_mutate_a_pending_booking_checkpoint_with_an_unrecognized_discount()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        await using var scenario = DevelopmentDataInitializerScenario.Create();
        await scenario.Initialize(ct);
        await scenario.ResetBaselineBookingsToPendingCheckpoint(ct);
        await scenario.UpdateFirstBaselineBookingDiscount(ct);
        var originalBookings = await scenario.GetBookingStates(ct);

        // Act
        await scenario.Initialize(ct);

        // Assert
        var persistedBookings = await scenario.GetBookingStates(ct);
        persistedBookings.ShouldBe(originalBookings);
    }
}
