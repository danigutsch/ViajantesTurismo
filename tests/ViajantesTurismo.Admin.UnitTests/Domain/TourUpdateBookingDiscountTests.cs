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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.NotFound);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("not found in this tour", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid discount type", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot exceed", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("at least", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot exceed subtotal", StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateBookingDiscount_when_booking_is_cancelled_returns_conflict()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for discount test setup.");
        (booking.Cancel().IsSuccess).ShouldBeTrue();

        // Act
        var result = tour.UpdateBookingDiscount(
            booking.Id,
            DiscountType.Percentage,
            10m,
            "Seasonal sale");

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Conflict);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("cannot be modified", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeTrue();
        (booking.Discount.Type).ShouldBe(DiscountType.Percentage);
        (booking.Discount.Amount).ShouldBe(10m);
        (booking.Discount.Reason).ShouldBe("Seasonal sale");
    }

}
