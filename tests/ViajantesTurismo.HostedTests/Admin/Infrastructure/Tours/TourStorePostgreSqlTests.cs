using PandaBear.Results;
using PandaBear.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
[TestScope(TestScope.Integration)]
public sealed class TourStorePostgreSqlTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime, IClassFixture<PostgreSqlTestServerFixture>
{
    private TourStorePostgreSqlScenario? scenario;

    private TourStorePostgreSqlScenario Scenario =>
        scenario ?? throw new InvalidOperationException("Test scenario is not initialized.");

    public async ValueTask InitializeAsync()
    {
        scenario = await TourStorePostgreSqlScenario.Create(fixture, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (scenario is not null)
        {
            await scenario.DisposeAsync();
        }
    }

    [Fact]
    public async Task GetByBookingIdForUpdate_loads_the_owning_tour_for_an_existing_booking()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour);
        bookingResult.IsSuccess.ShouldBeTrue();
        var booking = bookingResult.Value.ShouldNotBeNull();
        await Scenario.Seed(tour, ct);

        // Act
        var found = await Scenario.GetByBookingIdForUpdate(booking.Id, ct);
        var missing = await Scenario.GetByBookingIdForUpdate(Guid.CreateVersion7(), ct);

        // Assert
        found.ShouldNotBeNull();
        found.Id.ShouldBe(tour.Id);
        missing.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdForUpdate_loads_the_tour_and_its_bookings()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await Scenario.Seed(tour, ct);

        // Act
        var found = await Scenario.GetByIdForUpdate(tour.Id, ct);
        var missing = await Scenario.GetByIdForUpdate(Guid.CreateVersion7(), ct);

        // Assert
        found.ShouldNotBeNull();
        found.Id.ShouldBe(tour.Id);
        found.Bookings.ShouldHaveSingleItem().Id.ShouldBe(booking.Id);
        missing.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdForUpdate_rejects_lookup_without_a_transaction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        await Scenario.Seed(tour, ct);

        // Act
        Func<Task> load = () => Scenario.GetByIdWithoutTransaction(tour.Id, ct);
        var error = await load.ShouldThrow<InvalidOperationException>();

        // Assert
        error.Message.ShouldContain("requires an active transaction", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Failed_confirmation_result_aborts_the_save_and_releases_the_tour_row()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await Scenario.Seed(tour, ct);
        var failure = Result.Conflict(detail: "Rejected after saving confirmation.");

        // Act
        var result = await Scenario.ConfirmWithOutcome(booking.Id, () => failure, ct);
        var reloaded = await Scenario.GetByBookingIdForUpdate(booking.Id, ct);

        // Assert
        result.ShouldBe(failure);
        reloaded.ShouldNotBeNull();
        reloaded.Bookings.ShouldHaveSingleItem().IsPending.ShouldBeTrue();
    }

    [Fact]
    public async Task Thrown_confirmation_failure_aborts_the_save_and_releases_the_tour_row()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await Scenario.Seed(tour, ct);
        var failure = new InvalidOperationException("Confirmation failed after saving.");

        // Act
        Func<Task> confirm = () => Scenario.ConfirmWithOutcome(booking.Id, () => throw failure, ct);
        var exception = await confirm.ShouldThrow<InvalidOperationException>();
        var reloaded = await Scenario.GetByBookingIdForUpdate(booking.Id, ct);

        // Assert
        exception.ShouldBeSameAs(failure);
        reloaded.ShouldNotBeNull();
        reloaded.Bookings.ShouldHaveSingleItem().IsPending.ShouldBeTrue();
    }
}
