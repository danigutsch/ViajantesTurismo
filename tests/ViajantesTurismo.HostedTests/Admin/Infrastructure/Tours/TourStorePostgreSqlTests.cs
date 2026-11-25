using SharedKernel.Results;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Tours;

[Trait(TestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
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
    public async Task Finds_the_owning_tour_for_an_existing_booking_in_a_transaction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour);
        bookingResult.IsSuccess.ShouldBeTrue();
        var booking = bookingResult.Value.ShouldNotBeNull();
        await Scenario.Seed(tour, ct);

        // Act
        var found = await Scenario.GetByBookingId(booking.Id, ct);
        var missing = await Scenario.GetByBookingId(Guid.CreateVersion7(), ct);

        // Assert
        found.HasValue.ShouldBeTrue();
        found.Value.ShouldNotBeNull().Id.ShouldBe(tour.Id);
        missing.HasValue.ShouldBeFalse();
    }

    [Fact]
    public async Task Finds_the_tour_and_its_bookings_in_a_transaction()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value;
        await Scenario.Seed(tour, ct);

        // Act
        var found = await Scenario.GetById(tour.Id, ct);
        var missing = await Scenario.GetById(Guid.CreateVersion7(), ct);

        // Assert
        found.HasValue.ShouldBeTrue();
        var loadedTour = found.Value.ShouldNotBeNull();
        loadedTour.Id.ShouldBe(tour.Id);
        loadedTour.Bookings.ShouldHaveSingleItem().Id.ShouldBe(booking.Id);
        missing.HasValue.ShouldBeFalse();
    }

    [Fact]
    public async Task Command_lookup_requires_an_active_transaction()
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
        var reloaded = await Scenario.GetByBookingId(booking.Id, ct);

        // Assert
        result.ShouldBe(failure);
        reloaded.HasValue.ShouldBeTrue();
        reloaded.Value.ShouldNotBeNull().Bookings.ShouldHaveSingleItem().IsPending.ShouldBeTrue();
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
        var reloaded = await Scenario.GetByBookingId(booking.Id, ct);

        // Assert
        exception.ShouldBeSameAs(failure);
        reloaded.HasValue.ShouldBeTrue();
        reloaded.Value.ShouldNotBeNull().Bookings.ShouldHaveSingleItem().IsPending.ShouldBeTrue();
    }
}
