using SharedKernel.Results;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

public sealed class UpdateBookingDetailsCommandHandlerTests
{
    [Fact]
    public async Task Handle_updates_an_existing_booking_and_persists()
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value.ShouldNotBeNull();
        tourStore.AddExistingTour(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateBookingDetailsCommandHandler(
            tourStore,
            new NoOpTourCapacityMutationLock(),
            unitOfWork);
        var command = new UpdateBookingDetailsCommand(
            booking.Id,
            RoomTypeDto.DoubleOccupancy,
            BikeTypeDto.EBike,
            null,
            null);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        booking.PrincipalCustomer.BikeType.ShouldBe(BikeType.EBike);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_returns_not_found_without_persisting_when_booking_is_missing()
    {
        // Arrange
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateBookingDetailsCommandHandler(
            new FakeTourStore(),
            new NoOpTourCapacityMutationLock(),
            unitOfWork);
        var command = new UpdateBookingDetailsCommand(
            Guid.CreateVersion7(),
            RoomTypeDto.DoubleOccupancy,
            BikeTypeDto.Regular,
            null,
            null);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }
}
