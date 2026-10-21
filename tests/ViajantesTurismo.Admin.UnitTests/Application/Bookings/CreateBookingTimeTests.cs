using ViajantesTurismo.Admin.Application.Bookings.CreateBooking;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Application.Bookings;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, "Regression")]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, PandaBear.Testing.SharedKernelTestTraitNames.UnitScope)]
public sealed class CreateBookingTimeTests
{
    [Fact]
    public async Task Captures_the_controlled_booking_creation_instant()
    {
        // Arrange
        var now = new DateTimeOffset(2030, 2, 3, 14, 0, 0, TimeSpan.Zero);
        var tour = EntityBuilders.BuildTour();
        var customer = EntityBuilders.BuildCustomer(null);
        var tours = new FakeTourStore();
        tours.Add(tour);
        var customers = new FakeCustomerStore();
        customers.Add(customer);
        var clock = new FakeTimeProvider(now);
        var handler = new CreateBookingCommandHandler(tours, customers, new FakeUnitOfWork(), clock);
        var command = new CreateBookingCommand(tour.Id, customer.Id, BikeTypeDto.Regular,
            null, null, RoomTypeDto.DoubleOccupancy, DiscountTypeDto.None, 0m, null, null);
        // Act
        var result = await handler.Handle(command, TestContext.Current.CancellationToken);
        // Assert
        result.IsSuccess.ShouldBeTrue();
        tour.Bookings.ShouldHaveSingleItem().BookingDate.ShouldBe(now.UtcDateTime);
        clock.ReadCount.ShouldBe(1);
    }
}
