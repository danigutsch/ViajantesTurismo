using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

/// <summary>Builds valid and deliberately invalid booking domain objects for behavior-test steps.</summary>
internal static class BookingStepDataFactory
{
    /// <summary>Creates a principal booking customer with a default regular bike, failing test setup if creation fails.</summary>
    public static BookingCustomer CreatePrincipalCustomer(decimal bikePrice = 100m, BikeType bikeType = BikeType.Regular)
    {
        var result = BookingCustomer.Create(Guid.CreateVersion7(), bikeType, bikePrice);
        result.IsSuccess.ShouldBeTrue("Failed to create principal customer for test setup.");
        return result.Value;
    }

    /// <summary>Creates a companion booking customer with a default e-bike, failing test setup if creation fails.</summary>
    public static BookingCustomer CreateCompanionCustomer(decimal bikePrice = 200m, BikeType bikeType = BikeType.EBike)
    {
        var result = BookingCustomer.Create(Guid.CreateVersion7(), bikeType, bikePrice);
        result.IsSuccess.ShouldBeTrue("Failed to create companion customer for test setup.");
        return result.Value;
    }

    /// <summary>Creates a booking whose notes are <paramref name="notesLength"/> characters long, to exercise the notes length rule.</summary>
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
            notes, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>Creates a single-occupancy booking with the given base price, to exercise base price validation.</summary>
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
            null, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>Creates a double-occupancy booking with the given base price and room cost, to exercise room cost validation.</summary>
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
            null, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>Creates a booking whose room type is the raw <paramref name="invalidRoomType"/> value, to exercise room type validation.</summary>
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
            null, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }
}
