using SharedKernel.Testing;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Tours;

[Trait(SharedKernelTestTraitNames.CategoryName, TestTraits.DatabaseIntegrationCategory)]
public sealed class TourStorePostgreSqlTests(PostgreSqlTestServerFixture fixture) : IAsyncLifetime
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
    public async Task GetTourIdByBookingId_returns_some_for_an_existing_booking_and_none_when_missing()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour);
        bookingResult.IsSuccess.ShouldBeTrue();
        var booking = bookingResult.Value.ShouldNotBeNull();
        await Scenario.Seed(tour, ct);

        // Act
        var foundTourId = await Scenario.GetTourIdByBookingId(booking.Id, ct);
        var missingTourId = await Scenario.GetTourIdByBookingId(Guid.CreateVersion7(), ct);
        var found = foundTourId.TryGetValue(out var tourId);

        // Assert
        found.ShouldBeTrue();
        tourId.ShouldBe(tour.Id);
        missingTourId.IsEmpty.ShouldBeTrue();
    }
}
