using PandaBear.Results;
using ViajantesTurismo.Admin.Application.Bookings.UpdateBookingDetails;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, PandaBear.Testing.TestTraitValues.SecurityCategory)]
public sealed class UpdateBookingDetailsCommandHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_adds_an_existing_companion_and_allows_removal(bool confirmed)
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var customers = new FakeCustomerStore();
        var companion = EntityBuilders.BuildCustomer(null);
        customers.Seed(companion);
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value.ShouldNotBeNull();
        if (confirmed)
        {
            tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        }
        tourStore.AddExistingTour(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateBookingDetailsCommandHandler(tourStore, customers, new NoOpTourCapacityMutationLock(), unitOfWork);
        var command = new UpdateBookingDetailsCommand(booking.Id, RoomTypeDto.DoubleOccupancy, BikeTypeDto.Regular, companion.Id, BikeTypeDto.EBike);

        // Act
        var added = await handler.Handle(command, TestContext.Current.CancellationToken);
        var assignedCompanion = booking.CompanionCustomer;
        var removed = await handler.Handle(command with
        {
            CompanionCustomerId = null,
            CompanionBikeType = null
        }, TestContext.Current.CancellationToken);

        // Assert
        added.IsSuccess.ShouldBeTrue();
        assignedCompanion.ShouldNotBeNull();
        assignedCompanion.CustomerId.ShouldBe(companion.Id);
        removed.IsSuccess.ShouldBeTrue();
        booking.CompanionCustomer.ShouldBeNull();
        unitOfWork.SaveEntitiesCallCount.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Handle_rejects_a_missing_companion_without_changing_or_persisting_the_booking(bool confirmed)
    {
        // Arrange
        var tourStore = new FakeTourStore();
        var tour = EntityBuilders.BuildTour();
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour).Value.ShouldNotBeNull();
        if (confirmed)
        {
            tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        }
        tourStore.AddExistingTour(tour);
        var unitOfWork = new FakeUnitOfWork();
        var handler = new UpdateBookingDetailsCommandHandler(tourStore, new FakeCustomerStore(), new NoOpTourCapacityMutationLock(), unitOfWork);
        var command = new UpdateBookingDetailsCommand(booking.Id, RoomTypeDto.DoubleOccupancy, BikeTypeDto.EBike, Guid.CreateVersion7(), BikeTypeDto.Regular);

        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        result.Status.ShouldBe(ResultStatus.NotFound);
        booking.CompanionCustomer.ShouldBeNull();
        booking.PrincipalCustomer.BikeType.ShouldBe(BikeType.Regular);
        unitOfWork.SaveEntitiesCallCount.ShouldBe(0);
    }

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
            new FakeCustomerStore(),
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
            new FakeCustomerStore(),
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
