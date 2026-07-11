using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Testing.Behavior;
using SharedKernel.Results;
using ViajantesTurismo.Admin.Domain.Tours;

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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.NotFound);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("not found in this tour", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("exceeds remaining balance", StringComparison.Ordinal);
        (booking.Payments).ShouldBeEmpty();
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Payment amount must be greater than zero", StringComparison.Ordinal);
        (booking.Payments).ShouldBeEmpty();
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid payment method", StringComparison.Ordinal);
        (booking.Payments).ShouldBeEmpty();
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
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Invalid);
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Payment date cannot be in the future", StringComparison.Ordinal);
        (booking.Payments).ShouldBeEmpty();
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
        (result.IsSuccess).ShouldBeTrue();
        var payment = (booking.Payments).ShouldHaveSingleItem();
        (payment.Id).ShouldBe(result.Value.Id);
        (payment.Amount).ShouldBe(100m);
        (payment.PaymentDate).ShouldBe(paymentDate);
        (payment.Method).ShouldBe(PaymentMethod.CreditCard);
        (payment.ReferenceNumber).ShouldBe("TX-123");
        (payment.Notes).ShouldBe("Paid at reception");
    }

}
