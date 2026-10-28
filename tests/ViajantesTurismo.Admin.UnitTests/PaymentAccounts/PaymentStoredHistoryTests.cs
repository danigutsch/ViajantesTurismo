using TestTraits = ViajantesTurismo.Admin.UnitTests.Infrastructure.TestTraits;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.UnitTests.PaymentAccounts;

[Trait(PandaBear.Testing.TestTraitNames.CategoryName, TestTraits.PersistenceCategory)]
[Trait(PandaBear.Testing.SharedKernelTestTraitNames.CapabilityName, TestTraits.PaymentsCapability)]
public sealed class PaymentStoredHistoryTests
{
    [Fact]
    public void Persisted_refunds_releases_reviews_and_reversals_restore_the_same_balances()
    {
        // Arrange
        var state = PaymentStoredStateScenario.Create();

        // Act
        var restored = BookingFinancialHistory.Restore(state);

        // Assert
        restored.IsSuccess.ShouldBeTrue();
        restored.Value.OutstandingEuro.ShouldBe(45m);
        restored.Value.UnallocatedEuroCredit.ShouldBe(65m);
        restored.Value.IsReceiptReversed(state.Receipts[1].Id).ShouldBeTrue();
        restored.Value.IsRefundCorrected(state.Refunds[0].Id).ShouldBeTrue();
    }

    [Theory]
    [InlineData("schema")]
    [InlineData("booking")]
    [InlineData("version")]
    [InlineData("clock")]
    [InlineData("duplicate-id")]
    [InlineData("schedule-order")]
    [InlineData("schedule-approval-reason")]
    [InlineData("quote-rate")]
    [InlineData("quote-creator")]
    [InlineData("quote-approval")]
    [InlineData("receipt-fees")]
    [InlineData("receipt-date")]
    [InlineData("receipt-time")]
    [InlineData("receipt-reference")]
    [InlineData("reconciliation-receipt")]
    [InlineData("reconciliation-credit")]
    [InlineData("reconciliation-target")]
    [InlineData("reconciliation-currency")]
    [InlineData("allocation-credit")]
    [InlineData("allocation-target")]
    [InlineData("release-target")]
    [InlineData("release-credit")]
    [InlineData("refund-receipt")]
    [InlineData("refund-fx")]
    [InlineData("refund-date")]
    [InlineData("refund-credit")]
    [InlineData("refund-currency")]
    [InlineData("refund-target")]
    [InlineData("correction-target")]
    [InlineData("correction-duplicate")]
    [InlineData("reversal-target")]
    [InlineData("reversal-allocations")]
    [InlineData("reversal-duplicate")]
    [InlineData("withdrawal-target")]
    [InlineData("withdrawal-duplicate")]
    [InlineData("review-creator")]
    [InlineData("review-revision")]
    [InlineData("review-target")]
    [InlineData("Schedules-null")]
    [InlineData("Schedules-null-child")]
    [InlineData("Quotes-null")]
    [InlineData("Quotes-null-child")]
    [InlineData("Receipts-null")]
    [InlineData("Receipts-null-child")]
    [InlineData("Reconciliations-null")]
    [InlineData("Reconciliations-null-child")]
    [InlineData("Allocations-null")]
    [InlineData("Allocations-null-child")]
    [InlineData("Releases-null")]
    [InlineData("Releases-null-child")]
    [InlineData("Refunds-null")]
    [InlineData("Refunds-null-child")]
    [InlineData("Reversals-null")]
    [InlineData("Reversals-null-child")]
    [InlineData("RefundCorrections-null")]
    [InlineData("RefundCorrections-null-child")]
    [InlineData("Reviews-null")]
    [InlineData("Reviews-null-child")]
    [InlineData("Withdrawals-null")]
    [InlineData("Withdrawals-null-child")]
    public void Corrupt_financial_links_amounts_and_approvals_fail_closed(string corruption)
    {
        // Arrange
        var state = PaymentStoredStateScenario.Create();
        var corrupt = corruption switch
        {
            "schema" => state with { SchemaVersion = 2 },
            "booking" => state with { BookingId = Guid.Empty },
            "version" => state with { Version = -1 },
            "clock" => state with { LastChangedAt = default },
            "duplicate-id" => state with { Quotes = [state.Quotes[0] with { Id = state.Schedules[0].Id }] },
            "schedule-order" => state with { Schedules = [state.Schedules[0] with { Number = 2 }] },
            "schedule-approval-reason" => state with { Schedules = [state.Schedules[0] with { Approval = state.Schedules[0].Approval.ShouldNotBeNull() with { Reason = FinancialReason.QuoteAgreed } }] },
            "quote-rate" => state with { Quotes = [state.Quotes[0] with { BrlPerEuro = -1m }] },
            "quote-creator" => state with { Quotes = [state.Quotes[0] with { CreatedBy = "" }] },
            "quote-approval" => state with { Quotes = [state.Quotes[0] with { Approval = state.Quotes[0].Approval.ShouldNotBeNull() with { Reason = FinancialReason.InitialObligation } }] },
            "receipt-fees" => state with { Receipts = [state.Receipts[0] with { Fees = 121m }, state.Receipts[1]] },
            "receipt-date" => state with { Receipts = [state.Receipts[0] with { PaymentDate = default }, state.Receipts[1]] },
            "receipt-time" => state with { Receipts = [state.Receipts[0] with { SettledAt = PaymentScenario.Now.AddHours(1) }, state.Receipts[1]] },
            "receipt-reference" => state with { Receipts = [state.Receipts[0] with { Reference = "" }, state.Receipts[1]] },
            "reconciliation-receipt" => state with { Reconciliations = [state.Reconciliations[0] with { ReceiptId = Guid.CreateVersion7() }] },
            "reconciliation-credit" => state with { Reconciliations = [state.Reconciliations[0] with { EuroCredit = 10m }] },
            "reconciliation-target" => state with { Reconciliations = [state.Reconciliations[0] with { Allocations = [new PaymentSelectionState(Guid.CreateVersion7(), 10m)] }] },
            "reconciliation-currency" => state with { Reconciliations = [state.Reconciliations[0] with { QuoteId = state.Quotes[0].Id }] },
            "allocation-credit" => state with { Allocations = [state.Allocations[0] with { EuroAmount = -1m }] },
            "allocation-target" => state with { Allocations = [state.Allocations[0] with { InstallmentId = Guid.CreateVersion7() }] },
            "release-target" => state with { Releases = [state.Releases[0] with { AllocationId = Guid.CreateVersion7() }] },
            "release-credit" => state with { Releases = [state.Releases[0] with { EuroAmount = 1000m }] },
            "refund-receipt" => state with { Refunds = [state.Refunds[0] with { ReceiptId = Guid.CreateVersion7() }] },
            "refund-fx" => state with { Refunds = [state.Refunds[0] with { FxDifference = 1m }] },
            "refund-date" => state with { Refunds = [state.Refunds[0] with { PerformedAt = PaymentScenario.Now.AddHours(1) }] },
            "refund-credit" => state with { Refunds = [state.Refunds[0] with { EuroCreditReversed = 1m }] },
            "refund-currency" => state with { Refunds = [state.Refunds[0] with { QuoteId = state.Quotes[0].Id }] },
            "refund-target" => state with { Refunds = [state.Refunds[0] with { ReversedAllocations = [new PaymentSelectionState(Guid.CreateVersion7(), 10m)] }] },
            "correction-target" => state with { RefundCorrections = [state.RefundCorrections[0] with { RefundId = Guid.CreateVersion7() }] },
            "correction-duplicate" => state with { RefundCorrections = [state.RefundCorrections[0], state.RefundCorrections[0] with { Id = Guid.CreateVersion7() }] },
            "reversal-target" => state with { Reversals = [state.Reversals[0] with { ReceiptId = Guid.CreateVersion7() }] },
            "reversal-allocations" => state with { Reversals = [state.Reversals[0] with { AllocationIds = [Guid.CreateVersion7()] }] },
            "reversal-duplicate" => state with { Reversals = [state.Reversals[0], state.Reversals[0] with { Id = Guid.CreateVersion7() }] },
            "withdrawal-target" => state with { Withdrawals = [state.Withdrawals[0] with { TargetId = Guid.CreateVersion7() }] },
            "withdrawal-duplicate" => state with { Withdrawals = [state.Withdrawals[0], state.Withdrawals[0] with { Id = Guid.CreateVersion7() }] },
            "review-creator" => state with { Reviews = [state.Reviews[0] with { ReviewerId = "owner" }] },
            "review-revision" => state with { Reviews = [state.Reviews[0] with { TargetRevision = 9 }] },
            "review-target" => state with { Reviews = [state.Reviews[0] with { TargetId = Guid.CreateVersion7() }] },
            "Schedules-null" => state with { Schedules = null! },
            "Schedules-null-child" => state with { Schedules = [null!] },
            "Quotes-null" => state with { Quotes = null! },
            "Quotes-null-child" => state with { Quotes = [null!] },
            "Receipts-null" => state with { Receipts = null! },
            "Receipts-null-child" => state with { Receipts = [null!] },
            "Reconciliations-null" => state with { Reconciliations = null! },
            "Reconciliations-null-child" => state with { Reconciliations = [null!] },
            "Allocations-null" => state with { Allocations = null! },
            "Allocations-null-child" => state with { Allocations = [null!] },
            "Releases-null" => state with { Releases = null! },
            "Releases-null-child" => state with { Releases = [null!] },
            "Refunds-null" => state with { Refunds = null! },
            "Refunds-null-child" => state with { Refunds = [null!] },
            "Reversals-null" => state with { Reversals = null! },
            "Reversals-null-child" => state with { Reversals = [null!] },
            "RefundCorrections-null" => state with { RefundCorrections = null! },
            "RefundCorrections-null-child" => state with { RefundCorrections = [null!] },
            "Reviews-null" => state with { Reviews = null! },
            "Reviews-null-child" => state with { Reviews = [null!] },
            "Withdrawals-null" => state with { Withdrawals = null! },
            "Withdrawals-null-child" => state with { Withdrawals = [null!] },
            _ => throw new ArgumentOutOfRangeException(nameof(corruption))
        };

        // Act
        var restored = BookingFinancialHistory.Restore(corrupt);

        // Assert
        restored.IsFailure.ShouldBeTrue();
        restored.ToString().ShouldNotContain("agreement", StringComparison.Ordinal);
    }
}
