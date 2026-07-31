using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Bookings.ConfirmBooking;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

public sealed class ConfirmBookingCommandHandlerTests
{
    [Fact]
    public async Task Handle_confirms_an_existing_booking_and_persists()
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value.ShouldNotBeNull();
        tourStore.AddExistingTour(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmBookingCommandHandler(
            tourStore,
            new NoOpTourCapacityMutationLock(),
            unitOfWork);

        // Act
        var result = await handler.Handle(
            new ConfirmBookingCommand(booking.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        booking.IsConfirmed.ShouldBeTrue();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_booking_is_missing()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var handler = new ConfirmBookingCommandHandler(
            new FakeTourStore(),
            new NoOpTourCapacityMutationLock(),
            unitOfWork);

        // Act
        var result = await handler.Handle(
            new ConfirmBookingCommand(Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
