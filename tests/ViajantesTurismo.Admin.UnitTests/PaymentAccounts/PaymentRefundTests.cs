using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentRefundTests
{
    [Fact]
    public void Brl_refund_uses_a_new_quote_and_preserves_actual_cash_and_fx_difference()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var now = PaymentScenario.Now;
        var quote = scenario.Tour.DraftBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version,
            AgreedFxQuote.Create(5m, "recipient", "original", now, now.AddHours(1), now).Value, false, "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version, quote,
            FinancialApproval.Create("owner", now, FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
        var receipt = scenario.Tour.RecordBookingReceipt(scenario.Booking.Id, scenario.History.Version, ReceiptSettlement.Create(Currency.Real, 500m, 0m).Value,
            new DateOnly(2030, 1, 5), now, PaymentMethod.BankTransfer, "received", [InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now, new DateOnly(2030, 1, 5)).Value;
        var reconciliation = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, receipt, 100m, quote,
            [InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now).Value;
        scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, reconciliation,
            FinancialApproval.Create("owner", now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();
        var refundQuote = scenario.Tour.DraftBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version,
            AgreedFxQuote.Create(6m, "recipient", "new refund agreement", now, now.AddHours(1), now).Value, true, "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version, refundQuote,
            FinancialApproval.Create("owner", now, FinancialReason.QuoteAgreed, true).Value).IsSuccess.ShouldBeTrue();
        var allocation = scenario.History.ExportState().Allocations.ShouldHaveSingleItem();
        var selections = new[] { InstallmentAllocation.Create(allocation.Id, 100m).Value };

        // Act
        var excessivePayable = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 500m, 100m, 1000m,
            refundQuote, selections, "excessive", now, "owner", now, 6000m);
        var mismatchedCash = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 500m, 100m, 100m,
            refundQuote, selections, "refund", now, "owner", now, 599m);
        var refund = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 500m, 100m, 100m,
            refundQuote, selections, "refund", now, "owner", now, 600m);

        // Assert
        excessivePayable.IsFailure.ShouldBeTrue();
        mismatchedCash.IsFailure.ShouldBeTrue();
        refund.IsSuccess.ShouldBeTrue();
        scenario.Booking.RemainingBalance.ShouldBe(0m);
        scenario.Tour.ApproveBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, refund.Value,
            FinancialApproval.Create("owner", now, FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        scenario.Booking.RemainingBalance.ShouldBe(100m);
        var saved = scenario.History.ExportState().Refunds.ShouldHaveSingleItem();
        saved.ActualSettlement.ShouldBe(600m);
        saved.OriginalCashBasis.ShouldBe(500m);
        saved.FxDifference.ShouldBe(100m);
        scenario.History.GetReceiptRefundableCash(receipt).ShouldBe(0m);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }
    [Fact]
    public void Correcting_a_refund_requires_releasing_conflicting_credit_before_restoring_original_allocations()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var now = PaymentScenario.Now;
        var receipt = scenario.RecordReceipt(100m, 0m, installment);
        var reconciliation = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, receipt, 100m, null,
            [InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", now).Value;
        scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, reconciliation,
            FinancialApproval.Create("owner", now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();
        var originalAllocation = scenario.History.ExportState().Allocations.ShouldHaveSingleItem();
        var refund = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 50m, 50m, 50m, null,
            [InstallmentAllocation.Create(originalAllocation.Id, 50m).Value], "refund", now, "owner", now).Value;
        scenario.Tour.ApproveBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, refund,
            FinancialApproval.Create("owner", now, FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        var additionalReceipt = scenario.RecordReceipt(50m, 0m, installment);
        var additionalReconciliation = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, additionalReceipt, 50m, null,
            [InstallmentAllocation.Create(installment.Id, 50m).Value], "owner", now).Value;
        scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, additionalReconciliation,
            FinancialApproval.Create("owner", now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();
        var originalVersion = scenario.History.Version;

        // Act
        var conflicting = scenario.Tour.CorrectBookingPaymentRefund(scenario.Booking.Id, originalVersion, refund,
            FinancialApproval.Create("owner", now, FinancialReason.RefundRecordError, true).Value);

        // Assert
        conflicting.IsFailure.ShouldBeTrue();
        scenario.History.Version.ShouldBe(originalVersion);
        scenario.Booking.RemainingBalance.ShouldBe(0m);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
        var additionalAllocation = scenario.History.ExportState().Allocations.ShouldHaveSingleItem(item => item.ReceiptId == additionalReceipt);
        scenario.Tour.ReleaseBookingPaymentAllocations(scenario.Booking.Id, scenario.History.Version,
            [InstallmentAllocation.Create(additionalAllocation.Id, 50m).Value], FinancialApproval.Create("owner", now, FinancialReason.AllocationSelected, true).Value).IsSuccess.ShouldBeTrue();
        scenario.Tour.CorrectBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, refund,
            FinancialApproval.Create("owner", now, FinancialReason.RefundRecordError, true).Value).IsSuccess.ShouldBeTrue();
        scenario.Booking.RemainingBalance.ShouldBe(0m);
        scenario.History.UnallocatedEuroCredit.ShouldBe(50m);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }
}
