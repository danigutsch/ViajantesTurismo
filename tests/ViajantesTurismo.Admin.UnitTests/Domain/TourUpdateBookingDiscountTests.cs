using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateBookingDiscountTests
{
    [Fact]
    public void UpdateBookingDiscount_When_Booking_Does_Not_Exist_Returns_Not_Found()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();

        // Act
        var result = tour.UpdateBookingDiscount(
            Guid.CreateVersion7(),
            DiscountType.Percentage,
            10m,
            "Seasonal sale");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("not found in this tour", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_When_Discount_Type_Is_Invalid_Returns_Invalid()
    {
        // Arrange
        var (tour, _) = CreateSingleBooking();
        var booking = tour.Bookings.Single();

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            (DiscountType)999,
            10m,
            "Seasonal sale");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid discount type", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_When_Percentage_Exceeds_Maximum_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            150m,
            "Seasonal sale");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot exceed", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_When_Reason_Is_Too_Short_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            10m,
            "short");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("at least", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_When_Absolute_Discount_Exceeds_Subtotal_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Absolute,
            booking.Subtotal + 1m,
            "Large manual discount");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot exceed subtotal", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_When_Booking_Is_Cancelled_Returns_Conflict()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();
        Assert.True(booking.Cancel().IsSuccess);

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            10m,
            "Seasonal sale");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_When_Request_Is_Valid_Updates_Discount()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            10m,
            "Seasonal sale");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(DiscountType.Percentage, booking.Discount.Type);
        Assert.Equal(10m, booking.Discount.Amount);
        Assert.Equal("Seasonal sale", booking.Discount.Reason);
    }

    private static (Tour Tour, Booking Booking) CreateSingleBooking()
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(
            tour,
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy));

        Assert.True(bookingResult.IsSuccess, "Failed to create booking for discount test setup.");
        return (tour, bookingResult.Value);
    }
}
