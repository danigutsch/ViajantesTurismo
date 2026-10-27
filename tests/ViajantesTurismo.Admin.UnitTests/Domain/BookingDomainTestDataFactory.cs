using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Builds valid bookings, tours with bookings and booking requests for domain unit-test setup.</summary>
internal static class BookingDomainTestDataFactory
{
    /// <summary>Creates a tour and returns a single-customer booking added to it, failing setup if the add is rejected.</summary>
    public static Booking CreateSingleBooking(SingleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour, options);

        bookingResult.IsSuccess.ShouldBeTrue(failureMessage ?? "Failed to create single booking for test setup.");
        return bookingResult.Value;
    }

    /// <summary>Creates a tour and returns a double-customer booking added to it, failing setup if the add is rejected.</summary>
    public static Booking CreateDoubleBooking(DoubleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddDoubleCustomerBooking(tour, options);

        bookingResult.IsSuccess.ShouldBeTrue(failureMessage ?? "Failed to create double booking for test setup.");
        return bookingResult.Value;
    }

    /// <summary>Creates a tour with one single-customer booking and returns both.</summary>
    public static (Tour Tour, Booking Booking) CreateTourWithSingleBooking(SingleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour, options);

        bookingResult.IsSuccess.ShouldBeTrue(failureMessage ?? "Failed to create single booking for test setup.");
        return (tour, bookingResult.Value);
    }

    /// <summary>Creates a tour with one double-customer booking and returns both.</summary>
    public static (Tour Tour, Booking Booking) CreateTourWithDoubleBooking(DoubleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddDoubleCustomerBooking(tour, options);

        bookingResult.IsSuccess.ShouldBeTrue(failureMessage ?? "Failed to create double booking for test setup.");
        return (tour, bookingResult.Value);
    }

    /// <summary>Creates a valid companion booking customer with an e-bike and a fixed extra price, using a new customer id when none is given.</summary>
    public static BookingCustomer CreateValidCompanionCustomer(Guid? customerId = null)
    {
        var companionCustomerResult = BookingCustomer.Create(
            customerId ?? Guid.CreateVersion7(),
            BikeType.EBike,
            200m);

        companionCustomerResult.IsSuccess.ShouldBeTrue("Failed to create companion customer for test setup.");
        return companionCustomerResult.Value;
    }

    /// <summary>Creates a valid single-customer booking request with a regular bike, double occupancy and no discount.</summary>
    public static TourBookingRequest CreateValidSingleRequest(Guid? customerId = null)
    {
        return TourBookingRequest.CreateSingle(
            customerId ?? Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            BookingDiscountDefinition.None,
            notes: "Window seat preference");
    }

    /// <summary>Creates a valid double-customer booking request for the given customers (new ids when omitted) and room type.</summary>
    public static TourBookingRequest CreateValidDoubleRequest(
        Guid? principalCustomerId = null,
        Guid? companionCustomerId = null,
        RoomType roomType = RoomType.DoubleOccupancy)
    {
        return TourBookingRequest.CreateDouble(
            principalCustomerId ?? Guid.CreateVersion7(),
            BikeType.Regular,
            companionCustomerId ?? Guid.CreateVersion7(),
            BikeType.EBike,
            roomType,
            BookingDiscountDefinition.None,
            notes: "Near elevator");
    }
}
