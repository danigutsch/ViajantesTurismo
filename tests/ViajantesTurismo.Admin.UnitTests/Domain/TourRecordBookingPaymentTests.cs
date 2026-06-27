using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourRecordBookingPaymentTests
{
    [Fact]
    public void RecordBookingPayment_when_booking_does_not_exist_returns_not_found()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour();

        // Act
        var result = tour.RecordBookingPayment(
            Guid.CreateVersion7(),
            100m,
            DateTime.UtcNow.AddDays(-1),
            PaymentMethod.CreditCard,
            TimeProvider.System);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("not found in this tour", result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordBookingPayment_when_amount_exceeds_remaining_balance_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for payment test setup.");

        // Act
        var result = tour.RecordBookingPayment(
            booking.Id,
            booking.RemainingBalance + 1m,
            DateTime.UtcNow.AddDays(-1),
            PaymentMethod.CreditCard,
            TimeProvider.System);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("exceeds remaining balance", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(booking.Payments);
    }

    [Fact]
    public void RecordBookingPayment_when_amount_is_invalid_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for payment test setup.");

        // Act
        var result = tour.RecordBookingPayment(
            booking.Id,
            0m,
            DateTime.UtcNow.AddDays(-1),
            PaymentMethod.CreditCard,
            TimeProvider.System);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Payment amount must be greater than zero", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(booking.Payments);
    }

    [Fact]
    public void RecordBookingPayment_when_payment_method_is_invalid_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for payment test setup.");

        // Act
        var result = tour.RecordBookingPayment(
            booking.Id,
            100m,
            DateTime.UtcNow.AddDays(-1),
            (PaymentMethod)999,
            TimeProvider.System);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid payment method", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(booking.Payments);
    }

    [Fact]
    public void RecordBookingPayment_when_payment_date_is_in_the_future_returns_invalid()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for payment test setup.");

        // Act
        var result = tour.RecordBookingPayment(
            booking.Id,
            100m,
            DateTime.UtcNow.AddDays(1),
            PaymentMethod.CreditCard,
            TimeProvider.System);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Payment date cannot be in the future", result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Empty(booking.Payments);
    }

    [Fact]
    public void RecordBookingPayment_when_request_is_valid_records_payment()
    {
        // Arrange
        var (tour, booking) = BookingDomainTestDataFactory.CreateTourWithSingleBooking(
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy),
            "Failed to create booking for payment test setup.");
        var paymentDate = DateTime.UtcNow.AddDays(-2);

        // Act
        var result = tour.RecordBookingPayment(
            booking.Id,
            100m,
            paymentDate,
            PaymentMethod.CreditCard,
            TimeProvider.System,
            referenceNumber: "TX-123",
            notes: "Paid at reception");

        // Assert
        Assert.True(result.IsSuccess);
        var payment = Assert.Single(booking.Payments);
        Assert.Equal(result.Value.Id, payment.Id);
        Assert.Equal(100m, payment.Amount);
        Assert.Equal(paymentDate, payment.PaymentDate);
        Assert.Equal(PaymentMethod.CreditCard, payment.Method);
        Assert.Equal("TX-123", payment.ReferenceNumber);
        Assert.Equal("Paid at reception", payment.Notes);
    }

}
