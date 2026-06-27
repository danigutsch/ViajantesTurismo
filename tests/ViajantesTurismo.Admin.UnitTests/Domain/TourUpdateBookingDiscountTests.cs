using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourUpdateBookingDiscountTests
{
    [Fact]
    public void UpdateBookingDiscount_when_booking_does_not_exist_returns_not_found()
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
    public void UpdateBookingDiscount_when_discount_type_is_invalid_returns_invalid()
    {
        // Arrange
        var (tour, _) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");
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
    public void UpdateBookingDiscount_when_percentage_exceeds_maximum_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");

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
    public void UpdateBookingDiscount_when_reason_is_too_short_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");

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
    public void UpdateBookingDiscount_when_absolute_discount_exceeds_subtotal_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");

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
    public void UpdateBookingDiscount_when_booking_is_cancelled_returns_conflict()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");
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
    public void UpdateBookingDiscount_when_request_is_valid_updates_discount()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");

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

}
