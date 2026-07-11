using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.NotFound, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("not found in this tour", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Invalid discount type", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("cannot exceed", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("at least", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Invalid, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("cannot exceed subtotal", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_when_booking_is_cancelled_returns_conflict()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");
        TestAssert.True(booking.Cancel().IsSuccess);

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            10m,
            "Seasonal sale");

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Conflict, result.Status);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("cannot be modified", result.ErrorDetails.Detail, StringComparison.Ordinal);
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
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(DiscountType.Percentage, booking.Discount.Type);
        TestAssert.Equal(10m, booking.Discount.Amount);
        TestAssert.Equal("Seasonal sale", booking.Discount.Reason);
    }

}
