using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

public sealed partial class BookingFinancialHistory
{
    internal Result<Guid> DraftRefund(long expectedVersion, Guid receiptId, decimal originalCashBasis, decimal euroCreditReversed, decimal euroCashPayable,
        Guid? refundQuoteId, IReadOnlyList<InstallmentAllocation> reversedAllocations, string reference, DateTimeOffset performedAt, string actorId, DateTimeOffset now, decimal? actualSettlement = null)
    {
        ArgumentNullException.ThrowIfNull(reversedAllocations);
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var receipt = _state.Receipts.SingleOrDefault(item => item.Id == receiptId);
        if (receipt is null || Reversed(receiptId) || !ExactEuro(originalCashBasis) || originalCashBasis <= 0 || !ExactEuro(euroCreditReversed)
            || !ExactEuro(euroCashPayable) || euroCashPayable <= 0 || !ValidSelections(reversedAllocations) || !ValidReference(reference)
            || performedAt == default || performedAt.Offset != TimeSpan.Zero || performedAt > now || receipt.SettledAt is { } settledAt && performedAt < settledAt)
        {
            return FinancialErrors.Invalid("refund").ConvertError<Guid>();
        }
        decimal actual;
        if (receipt.Currency == Currency.Euro)
        {
            if (refundQuoteId is not null || euroCashPayable != originalCashBasis)
            {
                return FinancialErrors.Invalid("refundSettlement").ConvertError<Guid>();
            }
            actual = originalCashBasis;
        }
        else
        {
            var quote = _state.Quotes.SingleOrDefault(item => item.Id == refundQuoteId);
            if (quote is not { Approval: not null, ForRefund: true } || quote.Id == Reconciliation(receiptId)?.QuoteId || Withdrawn(quote.Id)
                || !QuoteTerms(quote).IsTimely(performedAt) || receipt.SettledAt is { } receivedAt && quote.AgreedAt < receivedAt)
            {
                return FinancialErrors.Invalid("refundQuote").ConvertError<Guid>();
            }
            var settlement = QuoteTerms(quote).Settle(euroCashPayable);
            if (settlement.IsFailure || settlement.Value.Amount <= 0)
            {
                return FinancialErrors.Invalid("refundSettlement").ConvertError<Guid>();
            }
            actual = settlement.Value.Amount;
        }
        if (actualSettlement is { } evidenced && evidenced != actual)
        {
            return FinancialErrors.Invalid("actualRefundSettlement").ConvertError<Guid>();
        }
        var draft = new PaymentRefundState(Guid.CreateVersion7(), receiptId, originalCashBasis, euroCreditReversed, euroCashPayable, actual,
            actual - originalCashBasis, refundQuoteId, reversedAllocations.Select(item => item.ExportState()).ToArray(), reference, performedAt, actorId, now, null);
        if (!RefundFits(draft))
        {
            return FinancialErrors.Invalid("refundBalance").ConvertError<Guid>();
        }
        _state = _state with
        {
            Refunds = [.. _state.Refunds, draft]
        };
        Changed(draft.Id, actorId, now, "RefundDrafted", FinancialReason.RefundPerformed, false);
        return Result.Ok(draft.Id);
    }

    internal Result<Guid> ApproveRefund(long expectedVersion, Guid refundId, FinancialApproval approval)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var draft = _state.Refunds.SingleOrDefault(item => item.Id == refundId);
        if (draft is null || draft.Approval is not null || approval.Reason != FinancialReason.RefundPerformed || !RefundFits(draft))
        {
            return FinancialErrors.Changed().ConvertError<Guid>();
        }
        if (draft.QuoteId is { } quoteId && (_state.Quotes.SingleOrDefault(item => item.Id == quoteId) is not { Approval: not null, ForRefund: true } quote
            || Withdrawn(quoteId) || ActiveRefunds().Any(item => item.QuoteId == quoteId) || !QuoteTerms(quote).IsTimely(draft.PerformedAt)))
        {
            return FinancialErrors.Unavailable().ConvertError<Guid>();
        }
        _state = _state with
        {
            Refunds = _state.Refunds.Select(item => item.Id == refundId ? item with { Approval = Stamp(approval) } : item).ToArray()
        };
        Changed(refundId, approval.ActorId, approval.ApprovedAt, "RefundApproved", approval.Reason, draft.CreatedBy == approval.ActorId);
        return Result.Ok(refundId);
    }

    internal Result<Guid> CorrectRefund(long expectedVersion, Guid refundId, FinancialApproval approval)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var refund = ActiveRefunds().SingleOrDefault(item => item.Id == refundId);
        if (approval.Reason != FinancialReason.RefundRecordError || refund is null)
        {
            return FinancialErrors.Unavailable().ConvertError<Guid>();
        }
        foreach (var restored in refund.ReversedAllocations.GroupBy(item => _state.Allocations.Single(allocation => allocation.Id == item.TargetId).InstallmentId))
        {
            var installment = _state.Schedules.SelectMany(item => item.Installments).Single(item => item.Id == restored.Key);
            if (restored.Sum(item => item.EuroAmount) > installment.Amount - PaidToInstallment(installment.Id))
            {
                return FinancialErrors.Invalid("releaseConflictingAllocationsBeforeRefundCorrection").ConvertError<Guid>();
            }
        }
        var correction = new PaymentRefundCorrectionState(Guid.CreateVersion7(), refundId, Stamp(approval));
        _state = _state with
        {
            RefundCorrections = [.. _state.RefundCorrections, correction]
        };
        Changed(correction.Id, approval.ActorId, approval.ApprovedAt, "RefundRecordCorrected", approval.Reason, false);
        return Result.Ok(correction.Id);
    }

    private IEnumerable<PaymentRefundState> ActiveRefunds() => _state.Refunds.Where(item => item.Approval is not null && !_state.RefundCorrections.Any(correction => correction.RefundId == item.Id));
    private bool RefundFits(PaymentRefundState refund)
    {
        var receipt = _state.Receipts.SingleOrDefault(item => item.Id == refund.ReceiptId);
        if (receipt is null || Reversed(receipt.Id))
        {
            return false;
        }
        var active = ActiveRefunds().Where(item => item.ReceiptId == receipt.Id).ToArray();
        if (refund.OriginalCashBasis > receipt.Gross - active.Sum(item => item.OriginalCashBasis)
            || refund.EuroCreditReversed > ApprovedEuro(receipt.Id) - active.Sum(item => item.EuroCreditReversed))
        {
            return false;
        }
        var extraCash = Math.Max(0, refund.OriginalCashBasis - NominalCash(receipt.Id, refund.EuroCreditReversed));
        var rateId = Reconciliation(receipt.Id)?.QuoteId ?? refund.QuoteId;
        var rate = rateId is { } id ? _state.Quotes.Single(item => item.Id == id).BrlPerEuro : 1m;
        if (rate < 1 && extraCash > decimal.MaxValue * rate)
        {
            return false;
        }
        var extraEuroLimit = decimal.Round(extraCash / rate, 2, MidpointRounding.AwayFromZero);
        if (refund.EuroCashPayable - refund.EuroCreditReversed > extraEuroLimit)
        {
            return false;
        }
        if (refund.EuroCashPayable < refund.EuroCreditReversed
            || Math.Max(0, refund.OriginalCashBasis - NominalCash(receipt.Id, refund.EuroCreditReversed)) > UncreditedRefundableCash(receipt.Id))
        {
            return false;
        }
        decimal selected = 0;
        foreach (var item in refund.ReversedAllocations)
        {
            var allocation = _state.Allocations.SingleOrDefault(allocation => allocation.Id == item.TargetId && allocation.ReceiptId == receipt.Id);
            if (allocation is null || item.EuroAmount > NetAllocation(allocation) || item.EuroAmount > refund.EuroCreditReversed - selected)
            {
                return false;
            }
            selected += item.EuroAmount;
        }
        // Refunding unapplied excess never silently reverses EUR already allocated to debt.
        return refund.EuroCreditReversed - selected <= AvailableCredit(receipt.Id);
    }
}
