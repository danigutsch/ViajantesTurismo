using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

internal static class BookingStepDataFactory
{
    public static BookingCustomer CreatePrincipalCustomer(decimal bikePrice = 100m, BikeType bikeType = BikeType.Regular)
    {
        var result = BookingCustomer.Create(Guid.CreateVersion7(), bikeType, bikePrice);
        Assert.True(result.IsSuccess, "Failed to create principal customer for test setup.");
        return result.Value;
    }

    public static BookingCustomer CreateCompanionCustomer(decimal bikePrice = 200m, BikeType bikeType = BikeType.EBike)
    {
        var result = BookingCustomer.Create(Guid.CreateVersion7(), bikeType, bikePrice);
        Assert.True(result.IsSuccess, "Failed to create companion customer for test setup.");
        return result.Value;
    }

    public static Result<Booking> CreateBookingWithNotes(int notesLength)
    {
        var principal = CreatePrincipalCustomer();
        var notes = new string('x', notesLength);

        return Booking.Create(
            Guid.CreateVersion7(),
            1000m,
            new BookingRoom(RoomType.SingleOccupancy, 0m),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            notes);
    }

    public static Result<Booking> CreateBookingWithBasePrice(decimal basePrice)
    {
        var principal = CreatePrincipalCustomer();

        return Booking.Create(
            Guid.CreateVersion7(),
            basePrice,
            new BookingRoom(RoomType.SingleOccupancy, 0m),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);
    }

    public static Result<Booking> CreateBookingWithRoomCost(decimal basePrice, decimal roomCost)
    {
        var principal = CreatePrincipalCustomer();

        return Booking.Create(
            Guid.CreateVersion7(),
            basePrice,
            new BookingRoom(RoomType.DoubleOccupancy, roomCost),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);
    }

    public static Result<Booking> CreateBookingWithInvalidRoomType(int invalidRoomType)
    {
        var principal = CreatePrincipalCustomer();

        return Booking.Create(
            Guid.CreateVersion7(),
            2000m,
            new BookingRoom((RoomType)invalidRoomType, 0m),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);
    }
}
