using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class TourRecordBookingPaymentTests
{
    [Fact]
    public void RecordBookingPayment_When_Booking_Does_Not_Exist_Returns_Not_Found()
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
    public void RecordBookingPayment_When_Amount_Exceeds_Remaining_Balance_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void RecordBookingPayment_When_Amount_Is_Invalid_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void RecordBookingPayment_When_Payment_Method_Is_Invalid_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void RecordBookingPayment_When_Payment_Date_Is_In_The_Future_Returns_Invalid()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();

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
    public void RecordBookingPayment_When_Request_Is_Valid_Records_Payment()
    {
        // Arrange
        var (tour, booking) = CreateSingleBooking();
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

    private static (Tour Tour, Booking Booking) CreateSingleBooking()
    {
        var tour = EntityBuilders.BuildTour();
        var bookingResult = BookingTestHelpers.AddSingleCustomerBooking(
            tour,
            new SingleBookingOptions(BikeType: BikeType.Regular, RoomType: RoomType.DoubleOccupancy));

        Assert.True(bookingResult.IsSuccess, "Failed to create booking for payment test setup.");
        return (tour, bookingResult.Value);
    }
}
