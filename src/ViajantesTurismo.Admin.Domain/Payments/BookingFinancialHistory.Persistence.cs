using System.Diagnostics.CodeAnalysis;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

public sealed partial class BookingFinancialHistory
{
    /// <summary>Rebuilds a history from a stored snapshot, failing when the snapshot is inconsistent.</summary>
    internal static Result<BookingFinancialHistory> Restore(BookingFinancialState? state)
    {
        try
        {
            return RestoreValidated(state);
        }
        catch (OverflowException) { return FinancialErrors.Invalid("storedFinancialTotals").ConvertError<BookingFinancialHistory>(); }
    }

    private static Result<BookingFinancialHistory> RestoreValidated(BookingFinancialState? state)
    {
        if (!HasValidShell(state))
        {
            return InvalidState();
        }
        var history = new BookingFinancialHistory(state);
        if (!HasUniqueIds(state) || !ValidSchedules(state) || !ValidQuotes(state) || !ValidReceipts(state) || !ValidReconciliations(state)
            || !ValidAllocations(state, history) || !ValidReleases(state) || !ValidRefunds(state) || !ValidRefundCorrections(state, history)
            || !ValidReversals(state, history) || !ValidWithdrawals(state) || !ValidReviews(state) || !ValidReceiptTotals(state, history)
            || !ValidBalances(state, history))
        {
            return InvalidState();
        }
        _ = history.UnallocatedEuroCredit;
        return Result.Ok(history);
    }

    private static bool HasValidShell([NotNullWhen(true)] BookingFinancialState? state) => state is not null && state.SchemaVersion == 1
        && state.BookingId != Guid.Empty && state.Version >= 0 && Utc(state.LastChangedAt) && HasAllCollections(state) && HasNoNullItems(state);

    private static bool HasAllCollections(BookingFinancialState state) => state.Schedules is not null && state.Quotes is not null && state.Receipts is not null
        && state.Reconciliations is not null && state.Allocations is not null && state.Releases is not null && state.Refunds is not null
        && state.Reversals is not null && state.RefundCorrections is not null && state.Reviews is not null && state.Withdrawals is not null;

    private static bool HasNoNullItems(BookingFinancialState state) => !state.Schedules.Any(item => item is null) && !state.Quotes.Any(item => item is null)
        && !state.Receipts.Any(item => item is null) && !state.Reconciliations.Any(item => item is null) && !state.Allocations.Any(item => item is null)
        && !state.Releases.Any(item => item is null) && !state.Refunds.Any(item => item is null) && !state.Reversals.Any(item => item is null)
        && !state.RefundCorrections.Any(item => item is null) && !state.Reviews.Any(item => item is null) && !state.Withdrawals.Any(item => item is null);

    private static bool HasUniqueIds(BookingFinancialState state)
    {
        var ids = state.Schedules.Select(item => item.Id).Concat(state.Quotes.Select(item => item.Id)).Concat(state.Receipts.Select(item => item.Id))
            .Concat(state.Reconciliations.Select(item => item.Id)).Concat(state.Allocations.Select(item => item.Id)).Concat(state.Releases.Select(item => item.Id))
            .Concat(state.Refunds.Select(item => item.Id)).Concat(state.Reversals.Select(item => item.Id)).Concat(state.RefundCorrections.Select(item => item.Id))
            .Concat(state.Reviews.Select(item => item.Id)).Concat(state.Withdrawals.Select(item => item.Id)).ToArray();
        return !ids.Any(id => id == Guid.Empty) && ids.Distinct().Count() == ids.Length;
    }

    private static bool ValidSchedules(BookingFinancialState state)
    {
        var scheduleNumber = 0;
        foreach (var schedule in state.Schedules)
        {
            if (schedule.Number != ++scheduleNumber || PaymentScheduleRevision.Restore(schedule).IsFailure || schedule.Superseded && schedule.Approval is null
                || schedule.CreatedAt > state.LastChangedAt || schedule.Approval is { } approval && (!ValidApproval(approval, schedule.CreatedAt)
                    || approval.Reason is not (FinancialReason.InitialObligation or FinancialReason.TermsChanged or FinancialReason.CancellationAdjustment)))
            {
                return false;
            }
        }
        return state.Schedules.Count(item => item.Approval is not null && !item.Superseded) <= 1
            && state.Schedules.SelectMany(item => item.Installments).Select(item => item.Id).Distinct().Count() == state.Schedules.Sum(item => item.Installments.Length);
    }

    private static bool ValidQuotes(BookingFinancialState state)
    {
        foreach (var quote in state.Quotes)
        {
            if (!Creator(quote.CreatedBy, quote.CreatedAt, state.LastChangedAt)
                || AgreedFxQuote.Create(quote.BrlPerEuro, quote.Source, quote.Reference, quote.AgreedAt, quote.ExpiresAt, quote.CreatedAt).IsFailure
                || quote.Approval is { } approval && (!ValidApproval(approval, quote.CreatedAt) || approval.Reason != FinancialReason.QuoteAgreed))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidReceipts(BookingFinancialState state)
    {
        foreach (var receipt in state.Receipts)
        {
            if (ReceiptSettlement.Create(receipt.Currency, receipt.Gross, receipt.Fees).IsFailure || receipt.PaymentDate == default
                || !Utc(receipt.CreatedAt) || receipt.CreatedAt > state.LastChangedAt || !Creator(receipt.CreatedBy, receipt.CreatedAt, state.LastChangedAt)
                || !Enum.IsDefined(receipt.Method) || receipt.SettledAt is { } instant && (!Utc(instant) || instant > receipt.CreatedAt)
                || !ValidReference(receipt.Reference) || !ValidStoredSelections(receipt.ProposedAllocations))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidReconciliations(BookingFinancialState state)
    {
        foreach (var reconciliation in state.Reconciliations)
        {
            var receipt = state.Receipts.SingleOrDefault(item => item.Id == reconciliation.ReceiptId);
            if (receipt is null || !ExactEuro(reconciliation.EuroCredit) || !Creator(reconciliation.CreatedBy, reconciliation.CreatedAt, state.LastChangedAt)
                || !ValidStoredSelections(reconciliation.Allocations) || reconciliation.Allocations.Sum(item => item.EuroAmount) > reconciliation.EuroCredit
                || reconciliation.Allocations.Any(item => !state.Schedules.SelectMany(schedule => schedule.Installments).Any(installment => installment.Id == item.TargetId))
                || receipt.Currency == Currency.Euro && reconciliation.QuoteId is not null
                || receipt.Currency == Currency.Real && !state.Quotes.Any(item => item.Id == reconciliation.QuoteId && !item.ForRefund && item.Approval is not null)
                || reconciliation.Approval is { } approval && (!ValidApproval(approval, reconciliation.CreatedAt)
                    || approval.Reason is not (FinancialReason.ExactReceipt or FinancialReason.LateReceipt or FinancialReason.ShortReceipt or FinancialReason.ExcessReceipt or FinancialReason.FeeDifference or FinancialReason.MismatchedReceipt)))
            {
                return false;
            }
        }
        return !state.Reconciliations.Where(item => item.Approval is not null).GroupBy(item => item.ReceiptId).Any(group => group.Select(item => item.QuoteId).Distinct().Count() > 1);
    }

    private static bool ValidAllocations(BookingFinancialState state, BookingFinancialHistory history)
    {
        foreach (var allocation in state.Allocations)
        {
            if (!ExactEuro(allocation.EuroAmount) || allocation.EuroAmount <= 0 || !Creator(allocation.CreatedBy, allocation.CreatedAt, state.LastChangedAt)
                || history.Reconciliation(allocation.ReceiptId) is null || !state.Schedules.SelectMany(item => item.Installments).Any(item => item.Id == allocation.InstallmentId))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidReleases(BookingFinancialState state)
    {
        foreach (var release in state.Releases)
        {
            var allocation = state.Allocations.SingleOrDefault(item => item.Id == release.AllocationId);
            if (allocation is null || !ExactEuro(release.EuroAmount) || release.EuroAmount <= 0
                || !ValidApproval(release.Approval, allocation.CreatedAt) || release.Approval.Reason != FinancialReason.AllocationSelected)
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidRefunds(BookingFinancialState state)
    {
        foreach (var refund in state.Refunds)
        {
            var receipt = state.Receipts.SingleOrDefault(item => item.Id == refund.ReceiptId);
            if (receipt is null || !ValidRefundShape(state, refund, receipt))
            {
                return false;
            }
            if (receipt.Currency == Currency.Euro && (refund.QuoteId is not null || refund.EuroCashPayable != refund.OriginalCashBasis || refund.ActualSettlement != refund.OriginalCashBasis))
            {
                return false;
            }
            if (receipt.Currency == Currency.Real && !ValidRealRefundQuote(state, refund))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidRefundShape(BookingFinancialState state, PaymentRefundState refund, PaymentReceiptState receipt) =>
        Creator(refund.CreatedBy, refund.CreatedAt, state.LastChangedAt) && ValidStoredSelections(refund.ReversedAllocations)
        && ExactEuro(refund.OriginalCashBasis) && refund.OriginalCashBasis > 0 && ExactEuro(refund.EuroCreditReversed) && ExactEuro(refund.EuroCashPayable)
        && refund.EuroCashPayable > 0 && refund.EuroCashPayable >= refund.EuroCreditReversed && ExactEuro(refund.ActualSettlement) && refund.ActualSettlement > 0
        && refund.FxDifference == refund.ActualSettlement - refund.OriginalCashBasis && ValidReference(refund.Reference)
        && Utc(refund.PerformedAt) && refund.PerformedAt <= refund.CreatedAt && !(receipt.SettledAt is { } instant && refund.PerformedAt < instant)
        && !refund.ReversedAllocations.Any(item => !state.Allocations.Any(allocation => allocation.Id == item.TargetId && allocation.ReceiptId == receipt.Id))
        && refund.ReversedAllocations.Sum(item => item.EuroAmount) <= refund.EuroCreditReversed
        && !(refund.Approval is { } approval && (!ValidApproval(approval, refund.CreatedAt) || approval.Reason != FinancialReason.RefundPerformed));

    private static bool ValidRealRefundQuote(BookingFinancialState state, PaymentRefundState refund)
    {
        var quote = state.Quotes.SingleOrDefault(item => item.Id == refund.QuoteId);
        return quote is { Approval: not null, ForRefund: true } && QuoteTerms(quote).IsTimely(refund.PerformedAt)
            && QuoteTerms(quote).Settle(refund.EuroCashPayable).IsSuccess && QuoteTerms(quote).Settle(refund.EuroCashPayable).Value.Amount == refund.ActualSettlement;
    }

    private static bool ValidRefundCorrections(BookingFinancialState state, BookingFinancialHistory history)
    {
        foreach (var correction in state.RefundCorrections)
        {
            var refund = state.Refunds.SingleOrDefault(item => item.Id == correction.RefundId && item.Approval is not null);
            if (refund?.Approval is not { } approval || !ValidApproval(correction.Approval, approval.ApprovedAt) || correction.Approval.Reason != FinancialReason.RefundRecordError)
            {
                return false;
            }
        }
        return !state.RefundCorrections.GroupBy(item => item.RefundId).Any(group => group.Count() > 1)
            && !history.ActiveRefunds().Where(item => item.QuoteId is not null).GroupBy(item => item.QuoteId).Any(group => group.Count() > 1);
    }

    private static bool ValidReversals(BookingFinancialState state, BookingFinancialHistory history)
    {
        foreach (var reversal in state.Reversals)
        {
            var receipt = state.Receipts.SingleOrDefault(item => item.Id == reversal.ReceiptId);
            if (receipt is null || reversal.AllocationIds is null || !ValidApproval(reversal.Approval, receipt.CreatedAt) || reversal.Approval.Reason != FinancialReason.ReceiptError
                || history.ActiveRefunds().Any(item => item.ReceiptId == receipt.Id)
                || !reversal.AllocationIds.Order().SequenceEqual(state.Allocations.Where(item => item.ReceiptId == receipt.Id).Select(item => item.Id).Order()))
            {
                return false;
            }
        }
        return !state.Reversals.GroupBy(item => item.ReceiptId).Any(group => group.Count() > 1);
    }

    private static bool ValidWithdrawals(BookingFinancialState state)
    {
        foreach (var withdrawal in state.Withdrawals)
        {
            var approvedAt = state.Schedules.FirstOrDefault(item => item.Id == withdrawal.TargetId)?.Approval?.ApprovedAt
                ?? state.Quotes.FirstOrDefault(item => item.Id == withdrawal.TargetId)?.Approval?.ApprovedAt;
            if (approvedAt is null || !ValidApproval(withdrawal.Approval, approvedAt.Value) || withdrawal.Approval.Reason != FinancialReason.ApprovalWithdrawn)
            {
                return false;
            }
        }
        return !state.Withdrawals.GroupBy(item => item.TargetId).Any(group => group.Count() > 1);
    }

    private static bool ValidReviews(BookingFinancialState state)
    {
        foreach (var review in state.Reviews)
        {
            var creator = state.Schedules.FirstOrDefault(item => item.Id == review.TargetId)?.CreatedBy ?? state.Quotes.FirstOrDefault(item => item.Id == review.TargetId)?.CreatedBy
                ?? state.Reconciliations.FirstOrDefault(item => item.Id == review.TargetId)?.CreatedBy ?? state.Refunds.FirstOrDefault(item => item.Id == review.TargetId)?.CreatedBy;
            if (review.TargetRevision != (state.Schedules.FirstOrDefault(item => item.Id == review.TargetId)?.Number ?? 1) || creator is null || creator == review.ReviewerId || !Creator(review.ReviewerId, review.ReviewedAt, state.LastChangedAt))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidReceiptTotals(BookingFinancialState state, BookingFinancialHistory history)
    {
        foreach (var receipt in state.Receipts)
        {
            var refunds = history.ActiveRefunds().Where(item => item.ReceiptId == receipt.Id).ToArray();
            if (refunds.Sum(item => item.OriginalCashBasis) > receipt.Gross || refunds.Sum(item => item.EuroCreditReversed) > history.ApprovedEuro(receipt.Id)
                || history.AvailableCredit(receipt.Id) < 0 || history.UncreditedRefundableCash(receipt.Id) < 0)
            {
                return false;
            }
        }
        return true;
    }

    private static bool ValidBalances(BookingFinancialState state, BookingFinancialHistory history) =>
        !state.Allocations.Any(allocation => history.NetAllocation(allocation) < 0)
        && !state.Schedules.SelectMany(item => item.Installments).Any(item => history.PaidToInstallment(item.Id) > item.Amount)
        && history.OutstandingEuro >= 0;

    private static bool Utc(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;
    private static bool Creator(string? actorId, DateTimeOffset at, DateTimeOffset latest) => FinancialApproval.ValidActor(actorId) && Utc(at) && at <= latest;
    private static bool ValidApproval(FinancialApprovalState? approval, DateTimeOffset createdAt) => approval is not null && approval.ApprovedAt >= createdAt
        && FinancialApproval.Create(approval.ActorId, approval.ApprovedAt, approval.Reason, true).IsSuccess;
    private static bool ValidStoredSelections(PaymentSelectionState[]? selections) => selections is not null
        && selections.All(item => item is not null && item.TargetId != Guid.Empty && item.EuroAmount > 0 && ExactEuro(item.EuroAmount))
        && selections.Select(item => item.TargetId).Distinct().Count() == selections.Length;
    private static Result<BookingFinancialHistory> InvalidState() => FinancialErrors.Invalid("storedFinancialHistory").ConvertError<BookingFinancialHistory>();
}
