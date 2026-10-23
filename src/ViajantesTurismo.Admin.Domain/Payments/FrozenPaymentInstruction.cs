using PandaBear.Results;
using ViajantesTurismo.Admin.Domain.PaymentAccounts;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Freezes generated financial terms; recipient details and artifacts are protected separately.</summary>
public sealed class FrozenPaymentInstruction
{
    private readonly PaymentInstructionTermsState _state;
    private FrozenPaymentInstruction(PaymentInstructionTermsState state) => _state = state;
    /// <summary>Gets the owning booking.</summary>
    public Guid BookingId => _state.BookingId;
    /// <summary>Gets the pinned schedule revision.</summary>
    public Guid ScheduleId => _state.ScheduleId;
    /// <summary>Gets the pinned account.</summary>
    public Guid AccountId => _state.AccountId;
    /// <summary>Gets the verified recipient revision.</summary>
    public Guid AccountRevisionId => _state.AccountRevisionId;
    /// <summary>Gets the frozen EUR amount.</summary>
    public decimal EuroAmount => _state.EuroAmount;
    /// <summary>Gets the frozen settlement total.</summary>
    public decimal SettlementAmount => _state.SettlementAmount;
    /// <summary>Gets the explicit settlement currency.</summary>
    public Currency Currency => _state.Currency;
    /// <summary>Gets the payment reference.</summary>
    public string Reference => _state.Reference;
    /// <summary>Gets immutable selected installments, including every explicit rounding adjustment.</summary>
    public IReadOnlyList<FrozenPaymentInstallment> Installments => Array.AsReadOnly(_state.Installments.Select(FrozenPaymentInstallment.FromState).ToArray());
    /// <summary>Gets the frozen agreed quote, independently of current public rates or quote expiry.</summary>
    public AgreedFxQuote? Quote => _state.Quote is { } quote ? AgreedFxQuote.Create(quote.BrlPerEuro, quote.Source, quote.Reference, quote.AgreedAt, quote.ExpiresAt, quote.CreatedAt).Value : null;
    /// <summary>Gets the frozen quote's exclusive expiry, if applicable.</summary>
    public DateTimeOffset? QuoteExpiresAt => _state.Quote?.ExpiresAt;

    internal static Result<FrozenPaymentInstruction> Create(BookingFinancialHistory history, decimal bookingPrice, BookingStatus status,
        PaymentAccount account, IReadOnlyList<Guid> selectedInstallments, Guid? quoteId, string reference, DateTimeOffset now)
    {
        if (now == default || now.Offset != TimeSpan.Zero || now < history.ExportState().LastChangedAt || account.Current.VerifiedAt is not { } verifiedAt || now < verifiedAt
            || !history.CanInstruct(bookingPrice, status) || !account.CanDeliver(account.Current.Id) || selectedInstallments.Count == 0
            || selectedInstallments.Distinct().Count() != selectedInstallments.Count || string.IsNullOrWhiteSpace(reference) || reference.Length > 128 || reference.Any(char.IsControl))
        {
            return FinancialErrors.Unavailable().ConvertError<FrozenPaymentInstruction>();
        }
        var schedule = history.ApprovedSchedule;
        if (schedule is null)
        {
            return FinancialErrors.Unavailable().ConvertError<FrozenPaymentInstruction>();
        }
        var selected = schedule.Installments.Where(item => selectedInstallments.Contains(item.Id)).ToArray();
        if (selected.Length != selectedInstallments.Count || selected.Any(item => history.OutstandingFor(item.Id) <= 0))
        {
            return FinancialErrors.Invalid("installments").ConvertError<FrozenPaymentInstruction>();
        }
        var currency = account.Current.Recipient.SettlementCurrency;
        var quoteState = quoteId is { } id ? history.ExportState().Quotes.SingleOrDefault(item => item.Id == id) : null;
        var quote = quoteId is { } approvedId ? history.ApprovedQuote(approvedId) : null;
        if (currency == Currency.Euro && quoteId is not null || currency == Currency.Real && (quote is null || quoteState is not { ForRefund: false } || !quote.IsTimely(now)))
        {
            return FinancialErrors.Unavailable().ConvertError<FrozenPaymentInstruction>();
        }
        var parts = new List<PaymentInstructionInstallmentState>();
        decimal cumulativeEuro = 0;
        decimal cumulativeSettlement = 0;
        foreach (var item in selected)
        {
            var euro = history.OutstandingFor(item.Id);
            cumulativeEuro += euro;
            var current = currency == Currency.Euro ? PaymentMoney.Create(cumulativeEuro, currency) : quote!.Settle(cumulativeEuro);
            var individuallyRounded = currency == Currency.Euro ? PaymentMoney.Create(euro, currency) : quote!.Settle(euro);
            if (current.IsFailure || individuallyRounded.IsFailure)
            {
                return FinancialErrors.Invalid("settlement").ConvertError<FrozenPaymentInstruction>();
            }
            var amount = current.Value.Amount - cumulativeSettlement;
            parts.Add(new PaymentInstructionInstallmentState(item.Id, item.DueDate, euro, amount, amount - individuallyRounded.Value.Amount));
            cumulativeSettlement = current.Value.Amount;
        }
        if (cumulativeSettlement <= 0)
        {
            return FinancialErrors.Invalid("settlement").ConvertError<FrozenPaymentInstruction>();
        }
        return Result.Ok(new FrozenPaymentInstruction(new PaymentInstructionTermsState(history.BookingId, schedule.Id, account.Id, account.Current.Id,
            currency, quoteId, quoteState, [.. parts], cumulativeEuro, cumulativeSettlement, reference, now)));
    }

    internal bool CanDeliver(BookingFinancialHistory history, decimal bookingPrice, BookingStatus status, PaymentAccount account, DateTimeOffset now) =>
        now >= _state.CreatedAt && now >= history.ExportState().LastChangedAt && now.Offset == TimeSpan.Zero && account.Current.VerifiedAt is { } verifiedAt && now >= verifiedAt
        && history.BookingId == BookingId && account.Id == AccountId && account.CanDeliver(AccountRevisionId)
        && account.Current.Recipient.SettlementCurrency == Currency && history.CanInstruct(bookingPrice, status) && history.ApprovedSchedule?.Id == ScheduleId
        && (Currency == Currency.Euro || _state.QuoteId is { } quoteId && history.ApprovedQuote(quoteId)?.IsTimely(now) == true)
        && _state.Installments.All(item => history.OutstandingFor(item.InstallmentId) == item.EuroAmount);

    internal bool IsTimelySettlement(DateOnly businessDate) => businessDate != default && businessDate <= _state.Installments.Min(item => item.DueDate);

    internal bool MatchesReceipt(Guid bookingId, Currency currency, decimal gross, decimal fees, DateOnly paymentDate, DateTimeOffset? settledAt, string reference, decimal euroCredit, Guid? quoteId) =>
        bookingId == BookingId && currency == Currency && gross == SettlementAmount && fees == 0 && reference == Reference && euroCredit == EuroAmount
        && paymentDate <= _state.Installments.Min(item => item.DueDate) && quoteId == _state.QuoteId && settledAt is { } instant && instant >= _state.CreatedAt
        && (_state.Quote is null || instant >= _state.Quote.AgreedAt && instant < _state.Quote.ExpiresAt);
    internal bool MatchesAllocations(PaymentSelectionState[] allocations) => allocations.Length == _state.Installments.Length
        && allocations.All(item => _state.Installments.Any(installment => installment.InstallmentId == item.TargetId && installment.EuroAmount == item.EuroAmount));

    internal PaymentInstructionTermsState ExportState() => _state;
    internal static Result<FrozenPaymentInstruction> Restore(PaymentInstructionTermsState? state)
    {
        try
        {
            return RestoreValidated(state);
        }
        catch (OverflowException) { return FinancialErrors.Invalid("storedInstructionTotals").ConvertError<FrozenPaymentInstruction>(); }
    }
    private static Result<FrozenPaymentInstruction> RestoreValidated(PaymentInstructionTermsState? state)
    {
        if (state is null || state.BookingId == Guid.Empty || state.ScheduleId == Guid.Empty || state.AccountId == Guid.Empty || state.AccountRevisionId == Guid.Empty
            || state.Currency is not (Currency.Euro or Currency.Real) || state.CreatedAt == default || state.CreatedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(state.Reference) || state.Reference.Length > 128 || state.Reference.Any(char.IsControl)
            || state.Installments is not { Length: > 0 } || state.Installments.Any(item => item is null || item.InstallmentId == Guid.Empty || item.EuroAmount <= 0
                || decimal.Round(item.EuroAmount, 2) != item.EuroAmount || item.DueDate == default || item.SettlementAmount < 0 || decimal.Round(item.SettlementAmount, 2) != item.SettlementAmount)
            || state.Installments.Select(item => item.InstallmentId).Distinct().Count() != state.Installments.Length
            || state.EuroAmount != state.Installments.Sum(item => item.EuroAmount) || state.SettlementAmount <= 0 || state.SettlementAmount != state.Installments.Sum(item => item.SettlementAmount)
            || state.Currency == Currency.Euro && (state.Quote is not null || state.QuoteId is not null || state.EuroAmount != state.SettlementAmount)
            || state.Currency == Currency.Real && (state.Quote is not { Approval: not null, ForRefund: false } || state.Quote.Id != state.QuoteId))
        {
            return FinancialErrors.Invalid("storedInstruction").ConvertError<FrozenPaymentInstruction>();
        }
        if (state.Quote is { } quote)
        {
            var terms = AgreedFxQuote.Create(quote.BrlPerEuro, quote.Source, quote.Reference, quote.AgreedAt, quote.ExpiresAt, quote.CreatedAt);
            if (terms.IsFailure || quote.Approval is null || quote.Approval.ApprovedAt > state.CreatedAt || !terms.Value.IsTimely(state.CreatedAt) || terms.Value.Settle(state.EuroAmount).IsFailure || terms.Value.Settle(state.EuroAmount).Value.Amount != state.SettlementAmount)
            {
                return FinancialErrors.Invalid("storedQuote").ConvertError<FrozenPaymentInstruction>();
            }
        }
        decimal euroTotal = 0;
        decimal settlementTotal = 0;
        DateOnly? previousDueDate = null;
        foreach (var item in state.Installments)
        {
            euroTotal += item.EuroAmount;
            var cumulative = state.Quote is { } agreed ? QuoteAmount(agreed, euroTotal) : PaymentMoney.Create(euroTotal, Currency.Euro);
            var individual = state.Quote is { } rate ? QuoteAmount(rate, item.EuroAmount) : PaymentMoney.Create(item.EuroAmount, Currency.Euro);
            if (cumulative.IsFailure || individual.IsFailure || item.SettlementAmount != cumulative.Value.Amount - settlementTotal
                || item.RoundingAdjustment != item.SettlementAmount - individual.Value.Amount || previousDueDate is { } due && item.DueDate < due)
            {
                return FinancialErrors.Invalid("storedInstructionInstallments").ConvertError<FrozenPaymentInstruction>();
            }
            settlementTotal = cumulative.Value.Amount;
            previousDueDate = item.DueDate;
        }
        return Result.Ok(new FrozenPaymentInstruction(state));
    }
    private static Result<PaymentMoney> QuoteAmount(BookingQuoteState quote, decimal euro) =>
        AgreedFxQuote.Create(quote.BrlPerEuro, quote.Source, quote.Reference, quote.AgreedAt, quote.ExpiresAt, quote.CreatedAt).Value.Settle(euro);
    /// <inheritdoc />
    public override string ToString() => "FrozenPaymentInstruction [redacted]";
}
