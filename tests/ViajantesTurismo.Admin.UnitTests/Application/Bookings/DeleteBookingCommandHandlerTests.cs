using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Bookings.DeleteBooking;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

public sealed class DeleteBookingCommandHandlerTests
{
    [Fact]
    public async Task Handle_deletes_an_existing_booking_and_persists()
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value.ShouldNotBeNull();
        tourStore.AddExistingTour(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteBookingCommandHandler(
            tourStore,
            new NoOpTourCapacityMutationLock(),
            unitOfWork);

        // Act
        var result = await handler.Handle(
            new DeleteBookingCommand(booking.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.Bookings.ShouldBeEmpty();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_booking_is_missing()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var handler = new DeleteBookingCommandHandler(
            new FakeTourStore(),
            new NoOpTourCapacityMutationLock(),
            unitOfWork);

        // Act
        var result = await handler.Handle(
            new DeleteBookingCommand(Guid.CreateVersion7()),
            TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
