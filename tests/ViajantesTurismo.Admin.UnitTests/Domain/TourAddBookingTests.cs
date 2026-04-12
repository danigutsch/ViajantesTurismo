using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourAddBookingTests
{
    [Fact]
    public void AddBooking_WhenTourIsFullyBooked_ReturnsConflict_And_DoesNotAddAnotherBooking()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(minCustomers: 1, maxCustomers: 1);
        var existingBookingResult = BookingTestHelpers.AddSingleCustomerBooking(
            tour,
            new SingleBookingOptions(CustomerId: Guid.CreateVersion7()));
        Assert.True(existingBookingResult.IsSuccess, "Failed to create existing booking for test setup.");
        Assert.True(existingBookingResult.Value.Confirm().IsSuccess);
        var request = CreateValidSingleRequest();

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Tour is fully booked", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Single(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenPrincipalBikeTypeIsInvalid_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            Guid.CreateVersion7(),
            (BikeType)999,
            RoomType.DoubleOccupancy,
            DiscountType.None);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid bike type", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenPrincipalBikeTypeIsNone_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.None,
            RoomType.DoubleOccupancy,
            DiscountType.None);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Bike type must be selected", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenCompanionBikeTypeIsInvalid_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            principalCustomerId: Guid.CreateVersion7(),
            principalBikeType: BikeType.Regular,
            roomType: RoomType.DoubleOccupancy,
            discountType: DiscountType.None,
            companionCustomerId: Guid.CreateVersion7(),
            companionBikeType: (BikeType)999);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid bike type", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenCompanionBikeTypeIsMissing_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            principalCustomerId: Guid.CreateVersion7(),
            principalBikeType: BikeType.Regular,
            roomType: RoomType.DoubleOccupancy,
            discountType: DiscountType.None,
            companionCustomerId: Guid.CreateVersion7(),
            companionBikeType: null);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Bike type must be selected", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenRoomTypeIsInvalid_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            (RoomType)999,
            DiscountType.None);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid room type", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenSingleRoomHasCompanion_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = CreateValidDoubleRequest(roomType: RoomType.SingleOccupancy);

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot have a companion", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenDiscountIsInvalid_ReturnsInvalid()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = TourBookingRequest.CreateSingle(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            BookingDiscountDefinition.Percentage(150m, "Seasonal promo"));

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot exceed", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(tour.Bookings);
    }

    [Fact]
    public void AddBooking_WhenRequestIsValid_AddsBookingToTour()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();
        var request = CreateValidDoubleRequest();

        // Act
        var result = tour.AddBooking(request);

        // Assert
        Assert.True(result.IsSuccess);
        var booking = Assert.Single(tour.Bookings);
        Assert.Equal(result.Value.Id, booking.Id);
        Assert.NotNull(booking.CompanionCustomer);
        Assert.Equal(request.Travelers.PrincipalCustomerId, booking.PrincipalCustomer.CustomerId);
        Assert.Equal(request.Travelers.CompanionCustomerId, booking.CompanionCustomer.CustomerId);
        Assert.Equal(request.RoomType, booking.RoomType);
    }

    private static TourBookingRequest CreateValidSingleRequest(Guid? customerId = null)
    {
        return TourBookingRequest.CreateSingle(
            customerId ?? Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            BookingDiscountDefinition.None,
            notes: "Window seat preference");
    }

    private static TourBookingRequest CreateValidDoubleRequest(
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
