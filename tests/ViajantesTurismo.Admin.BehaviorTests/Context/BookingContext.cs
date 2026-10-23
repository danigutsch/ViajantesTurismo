using JetBrains.Annotations;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.BehaviorTests.Context;

[UsedImplicitly]
public sealed class BookingContext
{
    public required Booking Booking
    {
        get; set;
    }

    public Result<Booking>? BookingCreationResult
    {
        get; set;
    }
    public Result? BookingOperationResult
    {
        get; set;
    }
    public Result<BookingCustomer>? BookingCustomerResult
    {
        get; set;
    }

    public required Action Action
    {
        get; set;
    }
    public void RecordAndApproveCredit(Tour tour, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(tour);
        var now = new DateTimeOffset(2030, 1, 5, 12, 0, 0, TimeSpan.Zero);
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var installment = PaymentInstallment.Create(Booking.TotalPrice, today).Value;
        var schedule = tour.DraftBookingPaymentSchedule(Booking.Id, 0, Booking.TotalPrice, [installment], "admin", now).Value;
        tour.ApproveBookingPaymentSchedule(Booking.Id, Booking.FinancialHistory.ShouldNotBeNull().Version, schedule,
            FinancialApproval.Create("admin", now, FinancialReason.InitialObligation, true).Value).IsSuccess.ShouldBeTrue();
        InstallmentAllocation[] allocations = [InstallmentAllocation.Create(installment.Id, amount).Value];
        var receipt = tour.RecordBookingReceipt(Booking.Id, Booking.FinancialHistory.Version,
            ReceiptSettlement.Create(Currency.Euro, amount, 0).Value, today, now, PaymentMethod.Cash, "receipt", allocations, "operator", now, today).Value;
        var reconciliation = tour.DraftBookingReceiptReconciliation(Booking.Id, Booking.FinancialHistory.Version,
            receipt, amount, null, allocations, "admin", now).Value;
        tour.ApproveBookingReceiptReconciliation(Booking.Id, Booking.FinancialHistory.Version, reconciliation,
            FinancialApproval.Create("admin", now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();
    }
}
