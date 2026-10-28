using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

public sealed partial class BookingFinancialHistory
{
    /// <summary>Records a payment receipt, optionally with proposed allocations.</summary>
    internal Result<Guid> RecordReceipt(long expectedVersion, ReceiptSettlement settlement, DateOnly paymentDate, DateTimeOffset? settledAt,
        PaymentMethod method, string reference, IReadOnlyList<InstallmentAllocation> proposedAllocations, string actorId, DateTimeOffset now, DateOnly businessDate)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(proposedAllocations);
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var proposedSchedule = _state.Schedules.LastOrDefault();
        if (paymentDate == default || paymentDate > businessDate || settledAt is { } instant && (instant.Offset != TimeSpan.Zero || instant > now)
            || !ValidReference(reference) || !ValidSelections(proposedAllocations) || proposedSchedule is not null && proposedAllocations.Count == 0
            || proposedAllocations.Any(item => proposedSchedule?.Installments.Any(installment => installment.Id == item.TargetId) != true))
        {
            return FinancialErrors.Invalid("receipt").ConvertError<Guid>();
        }
        var id = Guid.CreateVersion7();
        _state = _state with
        {
            Receipts = [.. _state.Receipts, new PaymentReceiptState(id, settlement.Gross.Currency, settlement.Gross.Amount, settlement.Fees.Amount,
            paymentDate, settledAt, method, reference, actorId, now, [.. proposedAllocations.Select(item => item.ExportState())])]
        };
        Changed(id, actorId, now, "ReceiptRecordedPending", FinancialReason.ReceiptRecorded, false);
        return Result.Ok(id);
    }

    /// <summary>Drafts a reconciliation that converts a receipt into EUR credit.</summary>
    internal Result<Guid> DraftReconciliation(long expectedVersion, Guid receiptId, decimal euroCredit, Guid? quoteId,
        IReadOnlyList<InstallmentAllocation> allocations, string actorId, DateTimeOffset now, Guid? instructionId = null)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var receipt = _state.Receipts.SingleOrDefault(item => item.Id == receiptId);
        if (receipt is null || Reversed(receiptId) || !ExactEuro(euroCredit) || !ValidSelections(allocations)
            || !SelectionsFit(allocations, euroCredit) || Reconciliation(receiptId) is { } original && original.QuoteId != quoteId
            || receipt.Currency == Currency.Real && (quoteId is null || !_state.Quotes.Any(item => item.Id == quoteId && !item.ForRefund && item.Approval is not null))
            || receipt.Currency == Currency.Euro && quoteId is not null
            || quoteId is { } selectedQuote && QuoteTerms(_state.Quotes.Single(item => item.Id == selectedQuote)).Settle(euroCredit).IsFailure)
        {
            return FinancialErrors.Invalid("reconciliation").ConvertError<Guid>();
        }
        var prospective = new ReceiptReconciliationState(Guid.CreateVersion7(), receiptId, euroCredit, quoteId, instructionId,
            [.. allocations.Select(item => item.ExportState())], actorId, now, null);
        if (!CreditFits(receipt, prospective))
        {
            return FinancialErrors.Invalid("remainingCashCredit").ConvertError<Guid>();
        }
        var id = prospective.Id;
        _state = _state with
        {
            Reconciliations = [.. _state.Reconciliations, new ReceiptReconciliationState(id, receiptId, euroCredit, quoteId, instructionId,
            [.. allocations.Select(item => item.ExportState())], actorId, now, null)]
        };
        Changed(id, actorId, now, "ReconciliationDrafted", FinancialReason.DraftSaved, false);
        return Result.Ok(id);
    }

    /// <summary>Approves a drafted reconciliation and credits the receipt.</summary>
    internal Result<Guid> ApproveReconciliation(long expectedVersion, Guid reconciliationId, FinancialApproval approval, FrozenPaymentInstruction? frozenTerms = null, DateOnly? settlementBusinessDate = null)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var draft = _state.Reconciliations.SingleOrDefault(item => item.Id == reconciliationId);
        var receipt = _state.Receipts.SingleOrDefault(item => item.Id == draft?.ReceiptId);
        if (draft is null || receipt is null || draft.Approval is not null || _state.Reconciliations.Last(item => item.ReceiptId == receipt.Id).Id != draft.Id || Reversed(receipt.Id) || !CreditFits(receipt, draft)
            || approval.Reason is not (FinancialReason.ExactReceipt or FinancialReason.LateReceipt or FinancialReason.ShortReceipt or FinancialReason.ExcessReceipt or FinancialReason.FeeDifference or FinancialReason.MismatchedReceipt)
            || approval.Reason == FinancialReason.ExactReceipt && (Reconciliation(receipt.Id) is not null || draft.InstructionId is null || frozenTerms is null || settlementBusinessDate is not { } businessDate || !frozenTerms.IsTimelySettlement(businessDate) || !frozenTerms.MatchesAllocations(draft.Allocations) || !frozenTerms.MatchesReceipt(BookingId, receipt.Currency, receipt.Gross, receipt.Fees, receipt.PaymentDate, receipt.SettledAt, receipt.Reference, draft.EuroCredit, draft.QuoteId)))
        {
            return FinancialErrors.Unavailable().ConvertError<Guid>();
        }
        var selected = draft.Allocations.Select(item => InstallmentAllocation.Create(item.TargetId, item.EuroAmount).Value).ToArray();
        if (!SelectionsFit(selected, draft.EuroCredit))
        {
            return FinancialErrors.Changed().ConvertError<Guid>();
        }
        _state = _state with
        {
            Reconciliations = [.. _state.Reconciliations.Select(item => item.Id == reconciliationId ? item with { Approval = Stamp(approval) } : item)]
        };
        foreach (var item in selected)
        {
            _state = _state with
            {
                Allocations = [.. _state.Allocations, new PaymentAllocationState(Guid.CreateVersion7(), receipt.Id, item.TargetId, item.EuroAmount, approval.ActorId, approval.ApprovedAt)]
            };
        }
        Changed(reconciliationId, approval.ActorId, approval.ApprovedAt, "ReconciliationApproved", approval.Reason, draft.CreatedBy == approval.ActorId);
        return Result.Ok(reconciliationId);
    }

    /// <summary>Allocates unallocated receipt credit to installments.</summary>
    internal Result<Guid> Allocate(long expectedVersion, Guid receiptId, IReadOnlyList<InstallmentAllocation> allocations, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(allocations);
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        if (approval.Reason != FinancialReason.AllocationSelected || Reversed(receiptId) || Reconciliation(receiptId) is null
            || allocations.Count == 0 || !ValidSelections(allocations) || !SelectionsFit(allocations, AvailableCredit(receiptId)))
        {
            return FinancialErrors.Invalid("allocations").ConvertError<Guid>();
        }
        foreach (var item in allocations)
        {
            _state = _state with
            {
                Allocations = [.. _state.Allocations, new PaymentAllocationState(Guid.CreateVersion7(), receiptId, item.TargetId, item.EuroAmount, approval.ActorId, approval.ApprovedAt)]
            };
        }
        Changed(receiptId, approval.ActorId, approval.ApprovedAt, "CreditAllocated", approval.Reason, false);
        return Result.Ok(receiptId);
    }

    /// <summary>Releases earlier allocations back to unallocated credit.</summary>
    internal Result<Guid> ReleaseAllocations(long expectedVersion, IReadOnlyList<InstallmentAllocation> releases, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(releases);
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        if (approval.Reason != FinancialReason.AllocationSelected || releases.Count == 0 || !ValidSelections(releases)
            || releases.Any(item => _state.Allocations.SingleOrDefault(allocation => allocation.Id == item.TargetId) is not { } allocation || item.EuroAmount > NetAllocation(allocation)))
        {
            return FinancialErrors.Invalid("releasedAllocations").ConvertError<Guid>();
        }
        foreach (var item in releases)
        {
            _state = _state with
            {
                Releases = [.. _state.Releases, new PaymentAllocationReleaseState(Guid.CreateVersion7(), item.TargetId, item.EuroAmount, Stamp(approval))]
            };
        }
        Changed(BookingId, approval.ActorId, approval.ApprovedAt, "AllocationsReleased", approval.Reason, false);
        return Result.Ok(BookingId);
    }

    /// <summary>Reverses a receipt and the allocations it funded.</summary>
    internal Result<Guid> ReverseReceipt(long expectedVersion, Guid receiptId, FinancialApproval approval)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        if (approval.Reason != FinancialReason.ReceiptError || Reversed(receiptId) || !_state.Receipts.Any(item => item.Id == receiptId)
            || ActiveRefunds().Any(item => item.ReceiptId == receiptId))
        {
            return FinancialErrors.Unavailable().ConvertError<Guid>();
        }
        var reversal = new PaymentReceiptReversalState(Guid.CreateVersion7(), receiptId,
            [.. _state.Allocations.Where(item => item.ReceiptId == receiptId).Select(item => item.Id)], Stamp(approval));
        _state = _state with
        {
            Reversals = [.. _state.Reversals, reversal]
        };
        Changed(reversal.Id, approval.ActorId, approval.ApprovedAt, "ReceiptReversed", approval.Reason, false);
        return Result.Ok(reversal.Id);
    }

    /// <summary>Gets explicit remaining cash credit in the receipt currency, independently of pending or approved EUR credit.</summary>
    public decimal UnallocatedCashCredit(Guid receiptId)
    {
        var receipt = _state.Receipts.SingleOrDefault(item => item.Id == receiptId);
        if (receipt is null || Reversed(receiptId))
        {
            return 0;
        }
        return Math.Max(0, UncreditedRefundableCash(receiptId) - receipt.Fees);
    }

    private decimal UncreditedRefundableCash(Guid receiptId) => Math.Max(0, _state.Receipts.Single(item => item.Id == receiptId).Gross - NominalCash(receiptId, ApprovedEuro(receiptId)))
        - ActiveRefunds().Where(item => item.ReceiptId == receiptId).Sum(item => Math.Max(0, item.OriginalCashBasis - NominalCash(receiptId, item.EuroCreditReversed)));

    private decimal NominalCash(Guid receiptId, decimal euroCredit)
    {
        var quoteId = Reconciliation(receiptId)?.QuoteId;
        return quoteId is { } id ? QuoteTerms(_state.Quotes.Single(item => item.Id == id)).Settle(euroCredit).Value.Amount : euroCredit;
    }

    /// <summary>Gets evidenced bank settlement time for the receipt linked to a reconciliation.</summary>
    public DateTimeOffset? GetReconciliationSettlementInstant(Guid id) => _state.Receipts.SingleOrDefault(item => item.Id == _state.Reconciliations.SingleOrDefault(reconciliation => reconciliation.Id == id)?.ReceiptId)?.SettledAt;
    /// <summary>Gets an immutable reconciliation's pinned instruction identifier for validated frozen-term lookup.</summary>
    public Guid? GetReconciliationInstructionId(Guid id) => _state.Reconciliations.SingleOrDefault(item => item.Id == id)?.InstructionId;
    /// <summary>Gets the approved EUR credit, before selected refund credit reversals.</summary>
    public decimal GetReceiptApprovedEuro(Guid receiptId) => ApprovedEuro(receiptId);
    /// <summary>Gets approved EUR credit still awaiting allocation or refund.</summary>
    public decimal GetReceiptUnallocatedEuro(Guid receiptId) => AvailableCredit(receiptId);
    /// <summary>Gets a receipt's remaining refundable cash in its original settlement currency.</summary>
    public decimal GetReceiptRefundableCash(Guid receiptId) => Reversed(receiptId) ? 0 : _state.Receipts.Single(item => item.Id == receiptId).Gross
        - ActiveRefunds().Where(item => item.ReceiptId == receiptId).Sum(item => item.OriginalCashBasis);
    /// <summary>Gets the exact nominal settlement difference; fees and net are projected separately.</summary>
    public decimal GetReceiptSettlementDifference(Guid receiptId) => _state.Receipts.Single(item => item.Id == receiptId).Gross
        - _state.Receipts.Single(item => item.Id == receiptId).Fees - NominalCash(receiptId, ApprovedEuro(receiptId));
    /// <summary>Gets whether a receipt has a linked full reversal.</summary>
    public bool IsReceiptReversed(Guid receiptId) => Reversed(receiptId);
    /// <summary>Gets whether a receipt still awaits explicit reconciliation.</summary>
    public bool IsReceiptPending(Guid receiptId) => !Reversed(receiptId) && Reconciliation(receiptId) is null;
    /// <summary>Gets the remaining credit of one immutable allocation after releases, refunds and full reversals.</summary>
    public decimal GetAllocationActiveEuro(Guid allocationId) => _state.Allocations.SingleOrDefault(item => item.Id == allocationId) is { } allocation ? NetAllocation(allocation) : 0;
    /// <summary>Gets active credit against any historical installment, retaining allocation lineage after amendments.</summary>
    public decimal GetInstallmentAllocatedEuro(Guid installmentId) => PaidToInstallment(installmentId);
    /// <summary>Gets whether an explicit append-only correction invalidated a refund record.</summary>
    public bool IsRefundCorrected(Guid refundId) => _state.RefundCorrections.Any(item => item.RefundId == refundId);
    /// <summary>Gets whether a financial approval was explicitly withdrawn.</summary>
    public bool IsApprovalWithdrawn(Guid targetId) => Withdrawn(targetId);

    private ReceiptReconciliationState? Reconciliation(Guid receiptId) => _state.Reconciliations.FirstOrDefault(item => item.ReceiptId == receiptId && item.Approval is not null);
    /// <summary>One receipt retains its originally agreed conversion basis. Later explicit credit tranches consume only its residual cash.</summary>
    private decimal ApprovedEuro(Guid receiptId) => _state.Reconciliations.Where(item => item.ReceiptId == receiptId && item.Approval is not null).Sum(item => item.EuroCredit);
    private bool CreditFits(PaymentReceiptState receipt, ReceiptReconciliationState draft)
    {
        var prior = ApprovedEuro(receipt.Id);
        if (draft.EuroCredit > decimal.MaxValue - prior || draft.EuroCredit > decimal.MaxValue - _state.Reconciliations.Where(item => item.Approval is not null).Sum(item => item.EuroCredit))
        {
            return false;
        }
        var quote = draft.QuoteId is { } id ? _state.Quotes.SingleOrDefault(item => item.Id == id) : null;
        if (draft.QuoteId is not null && (quote is not { Approval: not null, ForRefund: false } || Withdrawn(quote.Id)))
        {
            return false;
        }
        var converted = quote is null ? PaymentMoney.Create(prior + draft.EuroCredit, Currency.Euro) : QuoteTerms(quote).Settle(prior + draft.EuroCredit);
        if (converted.IsFailure)
        {
            return false;
        }
        var cashRefunded = ActiveRefunds().Where(item => item.ReceiptId == receipt.Id).Sum(item => Math.Max(0, item.OriginalCashBasis - NominalCash(receipt.Id, item.EuroCreditReversed)));
        return (Reconciliation(receipt.Id) is null && cashRefunded == 0 || converted.Value.Amount <= receipt.Gross - receipt.Fees - cashRefunded)
            && (Reconciliation(receipt.Id) is null || Reconciliation(receipt.Id)?.QuoteId == draft.QuoteId);
    }
    private bool Reversed(Guid receiptId) => _state.Reversals.Any(item => item.ReceiptId == receiptId);
    private decimal NetAllocation(PaymentAllocationState allocation) => Reversed(allocation.ReceiptId) ? 0 : allocation.EuroAmount
        - _state.Releases.Where(item => item.AllocationId == allocation.Id).Sum(item => item.EuroAmount)
        - ActiveRefunds().SelectMany(item => item.ReversedAllocations).Where(item => item.TargetId == allocation.Id).Sum(item => item.EuroAmount);
    private decimal PaidToInstallment(Guid installmentId) => _state.Allocations.Where(item => item.InstallmentId == installmentId).Sum(NetAllocation);
    private decimal AvailableCredit(Guid receiptId) => Reversed(receiptId) ? 0 : ApprovedEuro(receiptId)
        - ActiveRefunds().Where(item => item.ReceiptId == receiptId).Sum(item => item.EuroCreditReversed)
        - _state.Allocations.Where(item => item.ReceiptId == receiptId).Sum(NetAllocation);
    private bool SelectionsFit(IReadOnlyList<InstallmentAllocation> allocations, decimal available)
    {
        decimal total = 0;
        foreach (var item in allocations)
        {
            if (item.EuroAmount > available - total || item.EuroAmount > OutstandingFor(item.TargetId))
            {
                return false;
            }
            total += item.EuroAmount;
        }
        return true;
    }
    private static bool ExactEuro(decimal amount) => amount >= 0 && decimal.Round(amount, 2) == amount;
    private static bool ValidReference(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 128 && value == value.Trim() && !value.Any(char.IsControl);
    private static bool ValidSelections(IReadOnlyList<InstallmentAllocation> selections) => selections.All(item => item is not null)
        && selections.Select(item => item.TargetId).Distinct().Count() == selections.Count;
}
