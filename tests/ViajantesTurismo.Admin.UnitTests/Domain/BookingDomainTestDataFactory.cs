using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class BookingDomainTestDataFactory
{
    public static Booking CreateSingleBooking(SingleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour, options);

        Assert.True(bookingResult.IsSuccess, failureMessage ?? "Failed to create single booking for test setup.");
        return bookingResult.Value;
    }

    public static Booking CreateDoubleBooking(DoubleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddDoubleCustomerBooking(tour, options);

        Assert.True(bookingResult.IsSuccess, failureMessage ?? "Failed to create double booking for test setup.");
        return bookingResult.Value;
    }

    public static (Tour Tour, Booking Booking) CreateTourWithSingleBooking(SingleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(tour, options);

        Assert.True(bookingResult.IsSuccess, failureMessage ?? "Failed to create single booking for test setup.");
        return (tour, bookingResult.Value);
    }

    public static (Tour Tour, Booking Booking) CreateTourWithDoubleBooking(DoubleBookingOptions? options = null, string? failureMessage = null)
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddDoubleCustomerBooking(tour, options);

        Assert.True(bookingResult.IsSuccess, failureMessage ?? "Failed to create double booking for test setup.");
        return (tour, bookingResult.Value);
    }

    public static BookingCustomer CreateValidCompanionCustomer(Guid? customerId = null)
    {
        var companionCustomerResult = BookingCustomer.Create(
            customerId ?? Guid.CreateVersion7(),
            BikeType.EBike,
            200m);

        Assert.True(companionCustomerResult.IsSuccess, "Failed to create companion customer for test setup.");
        return companionCustomerResult.Value;
    }

    public static TourBookingRequest CreateValidSingleRequest(Guid? customerId = null)
    {
        return TourBookingRequest.CreateSingle(
            customerId ?? Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            BookingDiscountDefinition.None,
            notes: "Window seat preference");
    }

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
