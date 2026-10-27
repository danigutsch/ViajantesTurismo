using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>An immutable terms revision; approval freezes obligation, booking price and ordered installments.</summary>
public sealed class PaymentScheduleRevision
{
    private PaymentScheduleRevision(Guid id, int number, PaymentMoney obligation, PaymentMoney bookingPrice, PaymentInstallment[] installments,
        string createdBy, DateTimeOffset createdAt, FinancialApproval? approval, bool superseded)
    {
        Id = id;
        Number = number;
        Obligation = obligation;
        BookingPrice = bookingPrice;
        _installments = installments;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        Approval = approval;
        Superseded = superseded;
    }
    private readonly PaymentInstallment[] _installments;
    /// <summary>Gets the immutable revision identifier.</summary>
    public Guid Id
    {
        get;
    }
    /// <summary>Gets the sequential revision number.</summary>
    public int Number
    {
        get;
    }
    /// <summary>Gets the positive EUR obligation.</summary>
    public PaymentMoney Obligation
    {
        get;
    }
    /// <summary>Gets the booking-price snapshot at revision creation.</summary>
    public PaymentMoney BookingPrice
    {
        get;
    }
    /// <summary>Gets installments in approved calendar order.</summary>
    public IReadOnlyList<PaymentInstallment> Installments => Array.AsReadOnly(_installments);
    /// <summary>Gets the entering actor.</summary>
    public string CreatedBy
    {
        get;
    }
    /// <summary>Gets draft creation time.</summary>
    public DateTimeOffset CreatedAt
    {
        get;
    }
    /// <summary>Gets the explicit approval, if any.</summary>
    public FinancialApproval? Approval
    {
        get;
    }
    /// <summary>Gets whether an explicitly approved replacement superseded these terms.</summary>
    public bool Superseded
    {
        get;
    }
    /// <summary>Gets whether creator and approver are the same Admin.</summary>
    public bool SelfApproved => Approval?.ActorId == CreatedBy;

    /// <summary>Validates and creates a draft schedule revision.</summary>
    internal static Result<PaymentScheduleRevision> Create(int number, decimal obligation, decimal bookingPrice, IReadOnlyList<PaymentInstallment> installments, string actorId, DateTimeOffset now)
    {
        var money = PaymentMoney.Create(obligation, Currency.Euro);
        var price = PaymentMoney.Create(bookingPrice, Currency.Euro);
        if (money.IsFailure || price.IsFailure || obligation < 0 || bookingPrice <= 0 || number < 1 || !FinancialApproval.ValidActor(actorId)
            || now == default || now.Offset != TimeSpan.Zero || installments is null || obligation > 0 && installments.Count == 0 || installments.Any(item => item is null)
            || installments.Select(item => item.Id).Distinct().Count() != installments.Count)
        {
            return FinancialErrors.Invalid("schedule").ConvertError<PaymentScheduleRevision>();
        }
        decimal sum = 0;
        DateOnly previousDate = default;
        foreach (var installment in installments)
        {
            if (installment.Amount.Amount <= 0 || installment.Amount.Currency != Currency.Euro || installment.Amount.Amount > obligation - sum || installment.DueDate < previousDate)
            {
                return FinancialErrors.Invalid("installments").ConvertError<PaymentScheduleRevision>();
            }
            sum += installment.Amount.Amount;
            previousDate = installment.DueDate;
        }
        return sum == obligation ? Result.Ok(new PaymentScheduleRevision(Guid.CreateVersion7(), number, money.Value, price.Value, [.. installments], actorId, now, null, false))
            : FinancialErrors.Invalid("installments").ConvertError<PaymentScheduleRevision>();
    }
    /// <summary>Exports the revision for persistence.</summary>
    internal PaymentScheduleState ExportState() => new(Id, Number, Obligation.Amount, BookingPrice.Amount,
        _installments.Select(item => new PaymentInstallmentState(item.Id, item.Amount.Amount, item.DueDate)).ToArray(), CreatedBy, CreatedAt,
        Approval is null ? null : new FinancialApprovalState(Approval.ActorId, Approval.ApprovedAt, Approval.Reason), Superseded);

    /// <summary>Rebuilds a revision from a stored snapshot, failing when it is inconsistent.</summary>
    internal static Result<PaymentScheduleRevision> Restore(PaymentScheduleState state)
    {
        if (state.Id == Guid.Empty || state.Installments is null || state.Installments.Any(item => item is null || item.Id == Guid.Empty))
        {
            return FinancialErrors.Invalid("storedSchedule").ConvertError<PaymentScheduleRevision>();
        }
        var installments = new List<PaymentInstallment>();
        foreach (var item in state.Installments)
        {
            var validated = PaymentInstallment.Create(item.Amount, item.DueDate);
            if (validated.IsFailure)
            {
                return validated.ConvertError<PaymentInstallment, PaymentScheduleRevision>();
            }
            installments.Add(PaymentInstallment.Restore(item.Id, validated.Value.Amount, item.DueDate));
        }
        var result = Create(state.Number, state.Obligation, state.BookingPrice, installments, state.CreatedBy, state.CreatedAt);
        if (result.IsFailure)
        {
            return result;
        }
        FinancialApproval? approval = null;
        if (state.Approval is { } stored)
        {
            var approved = FinancialApproval.Create(stored.ActorId, stored.ApprovedAt, stored.Reason, true);
            if (approved.IsFailure || stored.ApprovedAt < state.CreatedAt)
            {
                return FinancialErrors.Invalid("storedApproval").ConvertError<PaymentScheduleRevision>();
            }
            approval = approved.Value;
        }
        var value = result.Value;
        return Result.Ok(new PaymentScheduleRevision(state.Id, state.Number, value.Obligation, value.BookingPrice, [.. installments], state.CreatedBy, state.CreatedAt, approval, state.Superseded));
    }

    /// <summary>Returns this revision with the given approval.</summary>
    internal PaymentScheduleRevision Approve(FinancialApproval approval) => new(Id, Number, Obligation, BookingPrice, _installments, CreatedBy, CreatedAt, approval, false);
    /// <summary>Returns this revision marked as superseded.</summary>
    internal PaymentScheduleRevision Supersede() => new(Id, Number, Obligation, BookingPrice, _installments, CreatedBy, CreatedAt, Approval, true);
}
