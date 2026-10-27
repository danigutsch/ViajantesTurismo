using PandaBear.Domain;
using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Owns the append-only EUR obligation and settlement history of a booking, mutated through Tour.</summary>
public sealed partial class BookingFinancialHistory
{
    private BookingFinancialState _state;
    private readonly List<IDomainEvent> _events = [];
    private BookingFinancialHistory(BookingFinancialState state) => _state = state;
    /// <summary>Gets the owning booking.</summary>
    public Guid BookingId => _state.BookingId;
    /// <summary>Gets the optimistic concurrency version.</summary>
    public long Version => _state.Version;
    /// <summary>Gets the current approved terms; drafts never imply approval.</summary>
    public PaymentScheduleRevision? ApprovedSchedule => _state.Schedules.LastOrDefault(item => item.Approval is not null && !item.Superseded) is { } state ? PaymentScheduleRevision.Restore(state).Value : null;
    /// <summary>Gets active EUR allocations to the current approved installments, net of refunds and reversals.</summary>
    public decimal AllocatedEuroCredit => ApprovedSchedule?.Installments.Sum(item => PaidToInstallment(item.Id)) ?? 0;
    /// <summary>Gets outstanding EUR debt without conflating overdue status.</summary>
    public decimal OutstandingEuro => (ApprovedSchedule?.Obligation.Amount ?? 0) - AllocatedEuroCredit;
    /// <summary>Gets approved unapplied customer EUR credit, separately from pending receipts.</summary>
    public decimal UnallocatedEuroCredit => _state.Receipts.Where(receipt => !Reversed(receipt.Id)).Sum(receipt => AvailableCredit(receipt.Id));
    /// <summary>Gets whether a calendar due date has passed with debt remaining, using the caller's controlled business date.</summary>
    public bool IsOverdue(DateOnly businessDate) => ApprovedSchedule?.Installments.Any(item => item.DueDate < businessDate && OutstandingFor(item.Id) > 0) == true;
    /// <summary>Gets the remaining EUR balance for an approved installment.</summary>
    public decimal OutstandingFor(Guid installmentId) => ApprovedSchedule?.Installments.FirstOrDefault(item => item.Id == installmentId) is { } item
        ? item.Amount.Amount - PaidToInstallment(installmentId) : 0;
    /// <summary>Checks approved terms and the current booking price before issuing or delivering instructions.</summary>
    public bool CanInstruct(decimal bookingPrice, BookingStatus status) => status != BookingStatus.Cancelled
        && !TermsAreStale(bookingPrice);
    /// <summary>Gets whether an unapproved replacement, changed price or explicit withdrawal prevents new instructions.</summary>
    public bool TermsAreStale(decimal bookingPrice) => ApprovedSchedule is not { } approved || approved.BookingPrice.Amount != bookingPrice
        || _state.Schedules[^1].Id != approved.Id || Withdrawn(approved.Id);
    /// <summary>Gets a specifically approved quote; expiry must additionally be checked at the relevant instant.</summary>
    public AgreedFxQuote? ApprovedQuote(Guid id) => _state.Quotes.SingleOrDefault(item => item.Id == id) is { Approval: not null } quote && !Withdrawn(id)
        ? QuoteTerms(quote) : null;
    /// <inheritdoc />
    public override string ToString() => "BookingFinancialHistory [redacted]";

    /// <summary>Creates an empty history for a booking.</summary>
    internal static BookingFinancialHistory Create(Guid bookingId, DateTimeOffset now) =>
        new(new BookingFinancialState(1, bookingId, 0, now, [], [], [], [], [], [], [], [], [], [], []));

    /// <summary>Drafts a new payment schedule revision.</summary>
    internal Result<Guid> DraftSchedule(long expectedVersion, decimal obligation, decimal bookingPrice, IReadOnlyList<PaymentInstallment> installments, string actorId, DateTimeOffset now)
    {
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var draft = PaymentScheduleRevision.Create(_state.Schedules.Length + 1, obligation, bookingPrice, installments, actorId, now);
        if (draft.IsFailure)
        {
            return draft.ConvertError<PaymentScheduleRevision, Guid>();
        }
        _state = _state with
        {
            Schedules = [.. _state.Schedules, draft.Value.ExportState()]
        };
        Changed(draft.Value.Id, actorId, now, "ScheduleDrafted", FinancialReason.TermsChanged, false);
        return Result.Ok(draft.Value.Id);
    }

    /// <summary>Approves the latest drafted schedule, superseding earlier approved ones.</summary>
    internal Result<Guid> ApproveSchedule(long expectedVersion, Guid scheduleId, decimal bookingPrice, BookingStatus bookingStatus, FinancialApproval approval)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        if (_state.Schedules.LastOrDefault() is not { Approval: null } draft || draft.Id != scheduleId || draft.BookingPrice != bookingPrice
            || bookingStatus == BookingStatus.Cancelled && approval.Reason != FinancialReason.CancellationAdjustment
            || ApprovedSchedule is null && approval.Reason is not (FinancialReason.InitialObligation or FinancialReason.CancellationAdjustment)
            || ApprovedSchedule is not null && approval.Reason is not (FinancialReason.TermsChanged or FinancialReason.CancellationAdjustment))
        {
            return FinancialErrors.Changed().ConvertError<Guid>();
        }
        var updated = PaymentScheduleRevision.Restore(draft).Value.Approve(approval).ExportState();
        _state = _state with
        {
            Schedules = [.. _state.Schedules.Select(item => item.Id == scheduleId ? updated : item.Approval is null ? item : item with { Superseded = true })]
        };
        Changed(scheduleId, approval.ActorId, approval.ApprovedAt, "ScheduleApproved", approval.Reason, approval.ActorId == draft.CreatedBy);
        return Result.Ok(scheduleId);
    }

    /// <summary>Drafts an agreed FX quote.</summary>
    internal Result<Guid> DraftQuote(long expectedVersion, AgreedFxQuote terms, bool forRefund, string actorId, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(terms);
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        if (terms.AgreedAt > now)
        {
            return FinancialErrors.Invalid("agreementTime").ConvertError<Guid>();
        }
        var id = Guid.CreateVersion7();
        _state = _state with
        {
            Quotes = [.. _state.Quotes, new BookingQuoteState(id, terms.BrlPerEuro, terms.Source, terms.Reference, terms.AgreedAt, terms.ExpiresAt, forRefund, actorId, now, null)]
        };
        Changed(id, actorId, now, "QuoteDrafted", FinancialReason.QuoteAgreed, false);
        return Result.Ok(id);
    }

    /// <summary>Approves a drafted quote.</summary>
    internal Result<Guid> ApproveQuote(long expectedVersion, Guid quoteId, FinancialApproval approval)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var quote = _state.Quotes.SingleOrDefault(item => item.Id == quoteId);
        if (quote is null || quote.Approval is not null || approval.Reason != FinancialReason.QuoteAgreed)
        {
            return FinancialErrors.Unavailable().ConvertError<Guid>();
        }
        _state = _state with
        {
            Quotes = [.. _state.Quotes.Select(item => item.Id == quoteId ? item with { Approval = Stamp(approval) } : item)]
        };
        Changed(quoteId, approval.ActorId, approval.ApprovedAt, "QuoteApproved", approval.Reason, approval.ActorId == quote.CreatedBy);
        return Result.Ok(quoteId);
    }

    /// <summary>Records a review decision on a draft.</summary>
    internal Result<Guid> Review(long expectedVersion, Guid targetId, bool changesRequested, string actorId, DateTimeOffset now)
    {
        var check = CanChange(expectedVersion, actorId, now);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        var creator = _state.Schedules.FirstOrDefault(item => item.Id == targetId)?.CreatedBy
            ?? _state.Quotes.FirstOrDefault(item => item.Id == targetId)?.CreatedBy
            ?? _state.Reconciliations.FirstOrDefault(item => item.Id == targetId)?.CreatedBy
            ?? _state.Refunds.FirstOrDefault(item => item.Id == targetId)?.CreatedBy;
        var decision = FinancialReviewDecision.Create(targetId, _state.Schedules.FirstOrDefault(item => item.Id == targetId)?.Number ?? 1, creator ?? string.Empty, actorId, now, changesRequested);
        if (decision.IsFailure)
        {
            return decision.ConvertError<FinancialReviewDecision, Guid>();
        }
        var review = new FinancialReviewState(Guid.CreateVersion7(), targetId, decision.Value.Revision, actorId, now, changesRequested);
        _state = _state with
        {
            Reviews = [.. _state.Reviews, review]
        };
        Changed(targetId, actorId, now, changesRequested ? "ReviewChangesRequested" : "ReviewAccepted", changesRequested ? FinancialReason.ReviewChangesRequested : FinancialReason.ReviewAccepted, false);
        return Result.Ok(targetId);
    }

    /// <summary>Withdraws the approval of a schedule or quote.</summary>
    internal Result<Guid> WithdrawApproval(long expectedVersion, Guid targetId, FinancialApproval approval)
    {
        var check = CanChange(expectedVersion, approval.ActorId, approval.ApprovedAt);
        if (check.IsFailure)
        {
            return check.ConvertError<Guid>();
        }
        if (approval.Reason != FinancialReason.ApprovalWithdrawn || Withdrawn(targetId)
            || !_state.Quotes.Any(item => item.Id == targetId && item.Approval is not null) && !_state.Schedules.Any(item => item.Id == targetId && item.Approval is not null && !item.Superseded))
        {
            return FinancialErrors.Unavailable().ConvertError<Guid>();
        }
        _state = _state with
        {
            Withdrawals = [.. _state.Withdrawals, new FinancialApprovalWithdrawalState(Guid.CreateVersion7(), targetId, Stamp(approval))]
        };
        Changed(targetId, approval.ActorId, approval.ApprovedAt, "ApprovalWithdrawn", approval.Reason, false);
        return Result.Ok(targetId);
    }

    /// <summary>Exports the current snapshot for persistence.</summary>
    internal BookingFinancialState ExportState() => _state;
    /// <summary>Gets the domain events raised since they were last cleared.</summary>
    internal IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _events.AsReadOnly();
    /// <summary>Clears the raised domain events.</summary>
    internal void ClearDomainEvents() => _events.Clear();
    private bool Withdrawn(Guid id) => _state.Withdrawals.Any(item => item.TargetId == id);
    private static FinancialApprovalState Stamp(FinancialApproval approval) => new(approval.ActorId, approval.ApprovedAt, approval.Reason);
    private static AgreedFxQuote QuoteTerms(BookingQuoteState quote) => AgreedFxQuote.Create(quote.BrlPerEuro, quote.Source, quote.Reference, quote.AgreedAt, quote.ExpiresAt, quote.CreatedAt).Value;
    private Result CanChange(long expectedVersion, string actorId, DateTimeOffset now) => expectedVersion != Version ? FinancialErrors.Changed()
        : !FinancialApproval.ValidActor(actorId) || now == default || now.Offset != TimeSpan.Zero || now < _state.LastChangedAt ? FinancialErrors.Invalid("action") : Result.Ok();
    private void Changed(Guid targetId, string actorId, DateTimeOffset now, string operation, FinancialReason reason, bool selfApproved)
    {
        _state = _state with
        {
            Version = Version + 1,
            LastChangedAt = now
        };
        _events.Add(new FinancialLifecycleDomainEvent(Guid.CreateVersion7(), BookingId, Version, targetId, actorId, now, operation, reason, selfApproved));
    }
}
