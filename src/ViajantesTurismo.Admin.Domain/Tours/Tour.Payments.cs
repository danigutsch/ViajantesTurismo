using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Payments;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;

namespace ViajantesTurismo.Admin.Domain.Tours;

public sealed partial class Tour
{
    /// <summary>Applies DraftBookingPaymentSchedule through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> DraftBookingPaymentSchedule(Guid bookingId, long expectedVersion, decimal obligation, IReadOnlyList<PaymentInstallment> installments, string actorId, DateTimeOffset now) =>
        ApplyBookingFinancialChange(bookingId, now, (booking, history) => history.DraftSchedule(expectedVersion, obligation, booking.TotalPrice, installments, actorId, now));

    /// <summary>Applies ApproveBookingPaymentSchedule through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ApproveBookingPaymentSchedule(Guid bookingId, long expectedVersion, Guid scheduleId, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.ApproveSchedule(expectedVersion, scheduleId, booking.TotalPrice, booking.Status, approval));
    }

    /// <summary>Applies DraftBookingPaymentQuote through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> DraftBookingPaymentQuote(Guid bookingId, long expectedVersion, AgreedFxQuote quote, bool forRefund, string actorId, DateTimeOffset now) =>
        ApplyBookingFinancialChange(bookingId, now, (booking, history) => history.DraftQuote(expectedVersion, quote, forRefund, actorId, now));

    /// <summary>Applies ApproveBookingPaymentQuote through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ApproveBookingPaymentQuote(Guid bookingId, long expectedVersion, Guid quoteId, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.ApproveQuote(expectedVersion, quoteId, approval));
    }

    /// <summary>Applies DraftBookingReceiptReconciliation through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> DraftBookingReceiptReconciliation(Guid bookingId, long expectedVersion, Guid receiptId, decimal euroCredit, Guid? quoteId, IReadOnlyList<InstallmentAllocation> allocations, string actorId, DateTimeOffset now, Guid? instructionId = null) =>
        ApplyBookingFinancialChange(bookingId, now, (booking, history) => history.DraftReconciliation(expectedVersion, receiptId, euroCredit, quoteId, allocations, actorId, now, instructionId));

    /// <summary>Applies ApproveBookingReceiptReconciliation through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ApproveBookingReceiptReconciliation(Guid bookingId, long expectedVersion, Guid reconciliationId, FinancialApproval approval, FrozenPaymentInstruction? frozenTerms = null, DateOnly? settlementBusinessDate = null)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.ApproveReconciliation(expectedVersion, reconciliationId, approval, frozenTerms, settlementBusinessDate));
    }

    /// <summary>Applies AllocateBookingPayment through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> AllocateBookingPayment(Guid bookingId, long expectedVersion, Guid receiptId, IReadOnlyList<InstallmentAllocation> allocations, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.Allocate(expectedVersion, receiptId, allocations, approval));
    }

    /// <summary>Applies ReleaseBookingPaymentAllocations through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ReleaseBookingPaymentAllocations(Guid bookingId, long expectedVersion, IReadOnlyList<InstallmentAllocation> allocations, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.ReleaseAllocations(expectedVersion, allocations, approval));
    }

    /// <summary>Applies ReverseBookingReceipt through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ReverseBookingReceipt(Guid bookingId, long expectedVersion, Guid receiptId, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.ReverseReceipt(expectedVersion, receiptId, approval));
    }

    /// <summary>Applies DraftBookingPaymentRefund through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> DraftBookingPaymentRefund(Guid bookingId, long expectedVersion, Guid receiptId, decimal originalCashBasis, decimal euroCreditReversed, decimal euroCashPayable, Guid? quoteId, IReadOnlyList<InstallmentAllocation> reversedAllocations, string reference, DateTimeOffset performedAt, string actorId, DateTimeOffset now, decimal? actualSettlement = null) =>
        ApplyBookingFinancialChange(bookingId, now, (booking, history) => history.DraftRefund(expectedVersion, receiptId, originalCashBasis, euroCreditReversed, euroCashPayable, quoteId, reversedAllocations, reference, performedAt, actorId, now, actualSettlement));

    /// <summary>Applies ApproveBookingPaymentRefund through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ApproveBookingPaymentRefund(Guid bookingId, long expectedVersion, Guid refundId, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.ApproveRefund(expectedVersion, refundId, approval));
    }

    /// <summary>Applies CorrectBookingPaymentRefund through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> CorrectBookingPaymentRefund(Guid bookingId, long expectedVersion, Guid refundId, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.CorrectRefund(expectedVersion, refundId, approval));
    }

    /// <summary>Applies ReviewBookingFinancialRecord through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> ReviewBookingFinancialRecord(Guid bookingId, long expectedVersion, Guid targetId, bool changesRequested, string actorId, DateTimeOffset now) =>
        ApplyBookingFinancialChange(bookingId, now, (booking, history) => history.Review(expectedVersion, targetId, changesRequested, actorId, now));

    /// <summary>Applies WithdrawBookingFinancialApproval through the owning Tour with a required financial revision precondition.</summary>
    public Result<Guid> WithdrawBookingFinancialApproval(Guid bookingId, long expectedVersion, Guid targetId, FinancialApproval approval)
    {
        ArgumentNullException.ThrowIfNull(approval);
        return ApplyBookingFinancialChange(bookingId, approval.ApprovedAt, (booking, history) => history.WithdrawApproval(expectedVersion, targetId, approval));
    }

    /// <summary>Records actual cash without silently settling installments; Operators may select proposed allocations.</summary>
    public Result<Guid> RecordBookingReceipt(Guid bookingId, long expectedVersion, ReceiptSettlement settlement, DateOnly paymentDate, DateTimeOffset? settledAt,
        PaymentMethod method, string reference, IReadOnlyList<InstallmentAllocation> allocations, string actorId, DateTimeOffset now, DateOnly businessDate)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        if (!Enum.IsDefined(method))
        {
            return FinancialErrors.Invalid("method").ConvertError<Guid>();
        }
        return ApplyBookingFinancialChange(bookingId, now, (booking, history) =>
        {
            var result = history.RecordReceipt(expectedVersion, settlement, paymentDate, settledAt, method, reference, allocations, actorId, now, businessDate);
            return result;
        });
    }

    /// <summary>Freezes server-generated outstanding amounts against the explicitly selected verified account revision.</summary>
    public Result<FrozenPaymentInstruction> CreateBookingPaymentInstruction(Guid bookingId, PaymentAccount account, IReadOnlyList<Guid> installmentIds, Guid? quoteId, string reference, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(installmentIds);
        var booking = _bookings.SingleOrDefault(item => item.Id == bookingId);
        return Pricing.Currency == Currency.Euro && booking?.FinancialHistory is { } history ? FrozenPaymentInstruction.Create(history, booking.TotalPrice, booking.Status, account, installmentIds, quoteId, reference, now)
            : FinancialErrors.Unavailable().ConvertError<FrozenPaymentInstruction>();
    }

    /// <summary>Rechecks cancellation, debt, price, schedule, account revision and quote expiry before instruction delivery.</summary>
    public bool CanDeliverBookingPaymentInstruction(FrozenPaymentInstruction instruction, PaymentAccount account, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentNullException.ThrowIfNull(account);
        var booking = _bookings.SingleOrDefault(item => item.Id == instruction.BookingId);
        return Pricing.Currency == Currency.Euro && booking?.FinancialHistory is { } history && instruction.CanDeliver(history, booking.TotalPrice, booking.Status, account, now);
    }

    /// <summary>Attaches loaded financial history to the matching booking of this tour; throws when no booking matches.</summary>
    internal void RestoreBookingFinancialHistory(BookingFinancialHistory history)
    {
        var booking = _bookings.SingleOrDefault(item => item.Id == history.BookingId)
            ?? throw new InvalidOperationException("Financial history belongs to another tour.");
        booking.RestoreFinancialHistory(history);
    }

    private Result<Guid> ApplyBookingFinancialChange(Guid bookingId, DateTimeOffset now, Func<Booking, BookingFinancialHistory, Result<Guid>> operation)
    {
        if (Pricing.Currency != Currency.Euro)
        {
            return FinancialErrors.Invalid("obligationCurrency").ConvertError<Guid>();
        }
        var booking = _bookings.SingleOrDefault(item => item.Id == bookingId);
        if (booking is null)
        {
            return BookingErrors.BookingNotFound().ConvertError<Guid>();
        }
        var history = booking.FinancialHistory ?? BookingFinancialHistory.Create(booking.Id, now);
        var result = operation(booking, history);
        if (result.IsSuccess)
        {
            booking.RestoreFinancialHistory(history);
        }
        return result;
    }
}
