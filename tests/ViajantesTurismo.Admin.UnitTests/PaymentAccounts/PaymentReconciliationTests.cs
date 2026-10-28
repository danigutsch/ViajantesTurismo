using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.CoreBehaviorCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentReconciliationTests
{
    [Fact]
    public void Residual_customer_cash_can_be_explicitly_credited_and_allocated_later()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var receipt = scenario.RecordReceipt(120m, 0m, installment);
        var first = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, receipt, 60m, null,
            [InstallmentAllocation.Create(installment.Id, 60m).Value], "owner", PaymentScenario.Now).Value;
        scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, first,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.MismatchedReceipt, true).Value).IsSuccess.ShouldBeTrue();

        // Act
        var later = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, receipt, 60m, null,
            [InstallmentAllocation.Create(installment.Id, 40m).Value], "owner", PaymentScenario.Now);

        // Assert
        later.IsSuccess.ShouldBeTrue();
        scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, later.Value,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.ExcessReceipt, true).Value).IsSuccess.ShouldBeTrue();
        scenario.History.GetReceiptApprovedEuro(receipt).ShouldBe(120m);
        scenario.History.GetReceiptUnallocatedEuro(receipt).ShouldBe(20m);
        scenario.History.UnallocatedCashCredit(receipt).ShouldBe(0m);
        scenario.Booking.RemainingBalance.ShouldBe(0m);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Pending_receipt_refund_prevents_crediting_the_returned_cash()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var receipt = scenario.RecordReceipt(100m, 0m, installment);
        var refund = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 50m, 0m, 50m, null, [], "return", PaymentScenario.Now, "owner", PaymentScenario.Now).Value;
        scenario.Tour.ApproveBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, refund,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        var draft = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, receipt, 100m, null,
            [InstallmentAllocation.Create(installment.Id, 100m).Value], "owner", PaymentScenario.Now);

        // Act
        var result = draft.IsFailure ? draft : scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, draft.Value,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.MismatchedReceipt, true).Value);

        // Assert
        result.IsFailure.ShouldBeTrue();
        scenario.Booking.AmountPaid.ShouldBe(0m);
        scenario.History.UnallocatedCashCredit(receipt).ShouldBe(50m);
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void Historical_quote_approval_records_evidence_without_making_expired_terms_deliverable()
    {
        // Arrange
        var scenario = new PaymentScenario();
        scenario.ApproveSchedule();
        var terms = AgreedFxQuote.Create(5m, "recipient", "historical agreement", PaymentScenario.Now.AddHours(-2), PaymentScenario.Now.AddHours(-1), PaymentScenario.Now).Value;
        var quote = scenario.Tour.DraftBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version, terms, true, "owner", PaymentScenario.Now).Value;

        // Act
        var result = scenario.Tour.ApproveBookingPaymentQuote(scenario.Booking.Id, scenario.History.Version, quote,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.QuoteAgreed, true).Value);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var approved = scenario.History.ApprovedQuote(quote);
        approved.ShouldNotBeNull();
        approved.IsTimely(PaymentScenario.Now).ShouldBeFalse();
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }
    [Fact]
    public void A_full_refund_may_include_the_original_processing_fee_without_overrefunding()
    {
        // Arrange
        var scenario = new PaymentScenario();
        var installment = scenario.ApproveSchedule();
        var receipt = scenario.RecordReceipt(100m, 5m, installment);
        var reconciliation = scenario.Tour.DraftBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, receipt, 95m, null,
            [InstallmentAllocation.Create(installment.Id, 95m).Value], "owner", PaymentScenario.Now).Value;
        scenario.Tour.ApproveBookingReceiptReconciliation(scenario.Booking.Id, scenario.History.Version, reconciliation,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.FeeDifference, true).Value).IsSuccess.ShouldBeTrue();
        var allocation = scenario.History.ExportState().Allocations.ShouldHaveSingleItem();

        // Act
        var refund = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 100m, 95m, 100m, null,
            [InstallmentAllocation.Create(allocation.Id, 95m).Value], "full refund", PaymentScenario.Now, "owner", PaymentScenario.Now);

        // Assert
        refund.IsSuccess.ShouldBeTrue();
        scenario.Tour.ApproveBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, refund.Value,
            FinancialApproval.Create("owner", PaymentScenario.Now, FinancialReason.RefundPerformed, true).Value).IsSuccess.ShouldBeTrue();
        scenario.History.GetReceiptRefundableCash(receipt).ShouldBe(0m);
        scenario.History.UnallocatedCashCredit(receipt).ShouldBe(0m);
        scenario.Booking.RemainingBalance.ShouldBe(100m);
        var extra = scenario.Tour.DraftBookingPaymentRefund(scenario.Booking.Id, scenario.History.Version, receipt, 0.01m, 0m, 0.01m, null, [], "extra", PaymentScenario.Now, "owner", PaymentScenario.Now);
        extra.IsFailure.ShouldBeTrue();
        BookingFinancialHistory.Restore(scenario.History.ExportState()).IsSuccess.ShouldBeTrue();
    }
}
