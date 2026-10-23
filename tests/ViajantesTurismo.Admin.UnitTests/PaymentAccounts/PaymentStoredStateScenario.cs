using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

internal static class PaymentStoredStateScenario
{
    public static BookingFinancialState Create()
    {
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var now = PaymentScenario.Now;
        var receipt = scenario.RecordReceipt(120m, 0m, installment);
        var history = scenario.History;
        var reconciliation = history.DraftReconciliation(history.Version, receipt, 120m, null,
            [InstallmentAllocation.Create(installment.Id, 60m).Value], "owner", now).Value;
        history.ApproveReconciliation(history.Version, reconciliation, FinancialApproval.Create("owner", now, FinancialReason.ExcessReceipt, true).Value).IsSuccess.ShouldBeTrue();
        var quote = history.DraftQuote(history.Version, AgreedFxQuote.Create(5m, "bank", "agreement", now, now.AddHours(1), now).Value, false, "owner", now).Value;
        history.ApproveQuote(history.Version, quote, FinancialApproval.Create("owner", now, FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
        var allocation = history.ExportState().Allocations.Single();
        var refund = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, history.Version, receipt, 20m, 20m, 20m, null,
            [InstallmentAllocation.Create(allocation.Id, 10m).Value], "refund", now, "owner", now).Value;
        history.ApproveRefund(history.Version, refund, FinancialApproval.Create("owner", now, FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        history.CorrectRefund(history.Version, refund, FinancialApproval.Create("owner", now, FinancialReason.RefundRecordError, true).Value).IsSuccess.ShouldBeTrue();
        history.ReleaseAllocations(history.Version, [InstallmentAllocation.Create(allocation.Id, 5m).Value],
            FinancialApproval.Create("owner", now, FinancialReason.AllocationSelected, true).Value).IsSuccess.ShouldBeTrue();
        history.Review(history.Version, refund, true, "reviewer", now).IsSuccess.ShouldBeTrue();
        history.WithdrawApproval(history.Version, quote, FinancialApproval.Create("owner", now, FinancialReason.ApprovalWithdrawn, true).Value).IsSuccess.ShouldBeTrue();
        var erroneous = scenario.RecordReceipt(10m, 0m, installment);
        history.ReverseReceipt(history.Version, erroneous, FinancialApproval.Create("owner", now, FinancialReason.ReceiptError, true).Value).IsSuccess.ShouldBeTrue();
        return history.ExportState();
    }
}
