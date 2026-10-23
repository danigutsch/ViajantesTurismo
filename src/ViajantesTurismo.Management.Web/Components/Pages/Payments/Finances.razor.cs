using PandaBear.AspNetCore;
using Microsoft.AspNetCore.Components;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Management.Web.Helpers;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Manages explicit EUR terms and actual settlement without automatic approvals.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515", Justification = "Blazor instantiates public page components.")]
public partial class Finances
{
    /// <summary>Gets or sets the booking whose financial history is managed.</summary>
    [Parameter]
    public Guid BookingId
    {
        get; set;
    }
    private GetBookingDto? _booking;
    private GetBookingFinancialHistoryDto? _history;
    private string? _error;
    private string? _actor;
    private bool _loading;
    private readonly IdempotentSubmission<GetPaymentCommandResultDto> _submission = new();
    private readonly PaymentScheduleFormModel _schedule = new();
    private readonly PaymentQuoteFormModel _quote = new();
    private readonly PaymentReceiptFormModel _receipt = new();
    private readonly PaymentReconciliationFormModel _reconciliation = new();
    private readonly PaymentRefundFormModel _refund = new();
    private readonly List<PaymentAllocationFormRow> _allocations = [];
    private readonly List<PaymentAllocationFormRow> _releases = [];
    private Guid _allocationReceipt;
    private string _instructionId = string.Empty;
    private string? _action;
    private Guid _target;
    private string _summary = string.Empty;
    private FinancialReasonDto _reason;
    private bool _confirmed;
    private bool _changesRequested;
    private long Version => _history?.Version ?? 0;
    private bool Blocked => _loading || _submission.IsBusy || _submission.HasPending;
    private IReadOnlyList<GetBookingReceiptDto> Receipts => _history?.Receipts ?? [];
    private IReadOnlyList<GetPaymentQuoteDto> Quotes => _history?.Quotes ?? [];
    private IReadOnlyList<PaymentTargetChoice> InstallmentChoices => _history?.Schedules.Where(item => item.Status == PaymentScheduleStatusDto.Approved)
        .SelectMany(item => item.Installments).Select(item => new PaymentTargetChoice(item.Id, $"{item.DueDate}: {item.OutstandingEuro} EUR outstanding ({item.Id})")).ToArray() ?? [];
    private IReadOnlyList<PaymentTargetChoice> ReceiptInstallmentChoices => _history is { Schedules.Count: > 0 } history
        ? history.Schedules[^1].Installments.Select(item => new PaymentTargetChoice(item.Id, $"{item.DueDate}: {item.EuroAmount} EUR proposed terms ({item.Id})")).ToArray() : [];
    private IReadOnlyList<PaymentTargetChoice> AllocationChoices => _history?.Allocations.Where(item => item.ActiveEuroCredit > 0)
        .Select(item => new PaymentTargetChoice(item.Id, $"Receipt {item.ReceiptId}: {item.ActiveEuroCredit} EUR ({item.Id})")).ToArray() ?? [];
    private bool RequiresAttention(Guid id) => _history?.Reviews.LastOrDefault(item => item.TargetId == id)?.ChangesRequested == true;
    private IEnumerable<FinancialReasonDto> Reasons => _action switch
    {
        "schedule" => [FinancialReasonDto.InitialObligation, FinancialReasonDto.TermsChanged, FinancialReasonDto.CancellationAdjustment],
        "reconciliation" => [FinancialReasonDto.ExactReceipt, FinancialReasonDto.LateReceipt, FinancialReasonDto.ShortReceipt, FinancialReasonDto.ExcessReceipt, FinancialReasonDto.FeeDifference, FinancialReasonDto.MismatchedReceipt],
        _ => [_reason]
    };
    protected override async Task OnParametersSetAsync()
    {
        _actor = (await Authentication.GetAuthenticationStateAsync()).User.GetSubjectId();
        await Load();
    }
    private async Task Load()
    {
        _loading = true;
        _error = null;
        try
        {
            var booking = await BookingsApi.GetBookingById(BookingId, CancellationToken.None);
            if (booking is not { Kind: HttpOutcomeKind.Succeeded, Value: { } value })
            {
                _error = "Booking could not be loaded.";
                return;
            }
            _booking = value;
            if (value.Currency != CurrencyDto.Euro)
            {
                return;
            }
            var result = await PaymentsApi.GetByBookingId(BookingId, CancellationToken.None);
            if (result is { Kind: HttpOutcomeKind.Succeeded, Value: { } history })
            {
                _history = history;
            }
            else if (result.Kind == HttpOutcomeKind.NotFound && value.FinancialVersion is null)
            {
                _history = null;
            }
            else
            {
                _error = HttpOutcomeUserMessage.For(result.Kind, "Financial history could not be loaded.");
            }
        }
        finally { _loading = false; }
    }
    private async Task LoadEvidence()
    {
        if (Blocked)
        {
            return;
        }
        _error = null;
        var result = await PaymentsApi.GetSensitiveDetails(BookingId, CancellationToken.None);
        if (result is { Kind: HttpOutcomeKind.Succeeded, Value: { } history })
        {
            _history = history;
        }
        else
        {
            _error = HttpOutcomeUserMessage.For(result.Kind, "Sensitive evidence could not be loaded.");
        }
    }
    private void Select(string action, Guid target, string summary, FinancialReasonDto reason)
    {
        if (Blocked)
        {
            return;
        }
        _action = action;
        _target = target;
        _summary = summary;
        _reason = reason;
        _confirmed = false;
        _changesRequested = false;
    }
    private void Cancel()
    {
        _action = null;
        _confirmed = false;
    }
    private static DateTimeOffset Utc(DateTime? value) => value is { } date ? new(DateTime.SpecifyKind(date, DateTimeKind.Utc)) : default;
    private static InstallmentAllocationDto[] Selections(IEnumerable<PaymentAllocationFormRow> rows) => rows.Select(row => new InstallmentAllocationDto { TargetId = row.TargetId, EuroAmount = row.EuroAmount }).ToArray();
    private async Task SaveSchedule()
    {
        var input = new CreatePaymentScheduleDto
        {
            ExpectedVersion = Version,
            ObligationEuro = _schedule.ObligationEuro,
            Installments = _schedule.Installments.Select(row => new PaymentInstallmentDto { EuroAmount = row.EuroAmount, DueDate = row.DueDate }).ToArray()
        };
        await Submit("saving schedule draft", (key, ct) => PaymentsApi.DraftSchedule(BookingId, input, key, ct));
    }
    private async Task SaveQuote()
    {
        var input = new CreatePaymentQuoteDto
        {
            ExpectedVersion = Version,
            BrlPerEuro = _quote.BrlPerEuro,
            Source = _quote.Source,
            Reference = _quote.Reference,
            AgreedAt = Utc(_quote.AgreedAtUtc),
            ExpiresAt = Utc(_quote.ExpiresAtUtc),
            ForRefund = _quote.ForRefund
        };
        await Submit("saving quote draft", (key, ct) => PaymentsApi.DraftQuote(BookingId, input, key, ct));
    }
    private async Task RecordReceipt()
    {
        var input = new CreateBookingReceiptDto
        {
            ExpectedVersion = Version,
            SettlementCurrency = _receipt.SettlementCurrency,
            Gross = _receipt.Gross,
            Fees = _receipt.Fees,
            PaymentDate = _receipt.PaymentDate,
            SettledAt = _receipt.SettledAtUtc is null ? null : Utc(_receipt.SettledAtUtc),
            Reference = _receipt.Reference,
            Method = _receipt.Method,
            ProposedAllocations = Selections(_receipt.Allocations)
        };
        await Submit("recording pending receipt", (key, ct) => PaymentsApi.RecordReceipt(BookingId, input, key, ct));
    }
    private async Task SaveReconciliation()
    {
        if (_instructionId.Length > 0 && !Guid.TryParse(_instructionId, out _))
        {
            _error = "Enter a valid instruction ID.";
            return;
        }
        var receiptId = _reconciliation.ReceiptId;
        var input = new CreateReceiptReconciliationDto
        {
            ExpectedVersion = Version,
            EuroCredit = _reconciliation.EuroCredit,
            QuoteId = _reconciliation.QuoteId,
            InstructionId = _instructionId.Length == 0 ? null : Guid.Parse(_instructionId),
            Allocations = Selections(_reconciliation.Allocations)
        };
        await Submit("saving reconciliation draft", (key, ct) => PaymentsApi.DraftReconciliation(BookingId, receiptId, input, key, ct));
    }
    private async Task SaveRefund()
    {
        if (!_refund.PerformedAttested)
        {
            return;
        }
        var receiptId = _refund.ReceiptId;
        var input = new CreatePaymentRefundDto
        {
            ExpectedVersion = Version,
            ActualSettlement = _refund.ActualSettlement,
            OriginalCashBasis = _refund.OriginalCashBasis,
            EuroCreditReversed = _refund.EuroCreditReversed,
            EuroCashPayable = _refund.EuroCashPayable,
            RefundQuoteId = _refund.RefundQuoteId,
            ReversedAllocations = Selections(_refund.Allocations),
            Reference = _refund.Reference,
            PerformedAt = Utc(_refund.PerformedAtUtc)
        };
        await Submit("saving performed refund draft", (key, ct) => PaymentsApi.DraftRefund(BookingId, receiptId, input, key, ct));
    }
    private async Task Confirm()
    {
        if (!_confirmed || _action is null || Blocked)
        {
            return;
        }
        var target = _target;
        var input = new ApproveFinancialRecordDto { ExpectedVersion = Version, Reason = _reason, Confirmed = true };
        var review = new ReviewFinancialRecordDto { ExpectedVersion = Version, ChangesRequested = _changesRequested };
        var allocation = new AllocatePaymentCreditDto { ExpectedVersion = Version, Reason = _reason, Confirmed = true, Allocations = Selections(_action == "release" ? _releases : _allocations) };
        Func<string, CancellationToken, Task<HttpOutcome<GetPaymentCommandResultDto>>>? command = _action switch
        {
            "schedule" => (key, ct) => PaymentsApi.ApproveSchedule(BookingId, target, input, key, ct),
            "quote" => (key, ct) => PaymentsApi.ApproveQuote(BookingId, target, input, key, ct),
            "reconciliation" => (key, ct) => PaymentsApi.ApproveReconciliation(BookingId, target, input, key, ct),
            "refund" => (key, ct) => PaymentsApi.ApproveRefund(BookingId, target, input, key, ct),
            "correct" => (key, ct) => PaymentsApi.CorrectRefund(BookingId, target, input, key, ct),
            "reverse" => (key, ct) => PaymentsApi.ReverseReceipt(BookingId, target, input, key, ct),
            "withdraw" => (key, ct) => PaymentsApi.WithdrawApproval(BookingId, target, input, key, ct),
            "review" => (key, ct) => PaymentsApi.Review(BookingId, target, review, key, ct),
            "allocate" => (key, ct) => PaymentsApi.Allocate(BookingId, target, allocation, key, ct),
            "release" => (key, ct) => PaymentsApi.ReleaseAllocations(BookingId, allocation, key, ct),
            _ => null
        };
        if (command is not null)
        {
            await Submit(_action, command);
        }
    }
    private async Task Submit(string operation, Func<string, CancellationToken, Task<HttpOutcome<GetPaymentCommandResultDto>>> command)
    {
        _error = null;
        await Complete(await _submission.Submit(operation, command, CancellationToken.None));
    }
    private async Task Retry() => await Complete(await _submission.Retry(CancellationToken.None));
    private async Task Complete(HttpOutcome<GetPaymentCommandResultDto>? result)
    {
        if (result is null)
        {
            return;
        }
        if (result.Kind == HttpOutcomeKind.Succeeded)
        {
            Cancel();
            await Load();
        }
        else if (result.Kind == HttpOutcomeKind.Conflict)
        {
            Cancel();
            await Load();
            _error = "Financial history changed. Review the current revision before submitting again.";
        }
        else
        {
            _error = HttpOutcomeUserMessage.For(result.Kind, "The financial action could not be completed. Check the selected amounts, dates and required evidence.");
        }
    }
}
