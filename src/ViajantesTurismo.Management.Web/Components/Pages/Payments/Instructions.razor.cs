using PandaBear.AspNetCore;
using Microsoft.AspNetCore.Components;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using PandaBear.Blazor;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Generates and explicitly approves immutable protected payment instruction revisions.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515", Justification = "Blazor instantiates public page components.")]
public partial class Instructions
{
    /// <summary>Gets or sets the owning booking.</summary>
    [Parameter]
    public Guid BookingId
    {
        get; set;
    }
    private IReadOnlyList<GetPaymentInstructionDto> _instructions = [];
    private IReadOnlyList<GetPaymentAccountDto> _accounts = [];
    private GetBookingFinancialHistoryDto? _history;
    private GetPaymentInstructionDto? _selected;
    private GetPaymentInstructionDto? _regenerate;
    private readonly HashSet<Guid> _selectedInstallments = [];
    private Guid _accountId;
    private Guid? _quoteId;
    private string? _actor;
    private string? _error;
    private string _action = string.Empty;
    private bool _loading;
    private bool _confirmed;
    private bool _changesRequested;
    private readonly IdempotentSubmission<GetPaymentInstructionDto> _submission = new();
    private bool Blocked => _loading || _submission.IsBusy || _submission.HasPending;
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
            var financials = await PaymentsApi.GetByBookingId(BookingId, CancellationToken.None);
            var accounts = await AccountsApi.List(CancellationToken.None);
            var instructions = await InstructionsApi.ListForBooking(BookingId, CancellationToken.None);
            if (financials is { Kind: HttpOutcomeKind.Succeeded, Value: { } history })
            {
                _history = history;
            }
            else
            {
                _history = null;
                _error = "Approve a EUR schedule before generating instructions.";
            }
            if (accounts is { Kind: HttpOutcomeKind.Succeeded, Value: { } recipients })
            {
                _accounts = recipients;
            }
            else
            {
                _accounts = [];
                _error = "Recipient accounts could not be loaded.";
            }
            if (instructions is { Kind: HttpOutcomeKind.Succeeded, Value: { } values })
            {
                _instructions = values;
            }
            else
            {
                _error = new HttpOutcomeMessages("Instruction revisions could not be loaded.").For(instructions.Kind);
            }
        }
        finally { _loading = false; }
    }
    private void Toggle(Guid id, ChangeEventArgs args)
    {
        if (args.Value is true)
        {
            _selectedInstallments.Add(id);
        }
        else
        {
            _selectedInstallments.Remove(id);
        }
    }
    private void RegenerateSelection(GetPaymentInstructionDto item)
    {
        if (!Blocked)
        {
            _regenerate = item;
        }
    }
    private void Select(GetPaymentInstructionDto item, string action)
    {
        if (Blocked)
        {
            return;
        }
        _selected = item;
        _action = action;
        _confirmed = false;
        _changesRequested = false;
    }
    private void Cancel()
    {
        _selected = null;
        _confirmed = false;
    }
    private async Task Generate()
    {
        if (_history is null || _accounts.SingleOrDefault(item => item.Id == _accountId) is not { } account)
        {
            _error = "Select a verified recipient and approved schedule.";
            return;
        }
        var input = new GeneratePaymentInstructionsDto
        {
            ExpectedVersion = _history.Version,
            AccountId = account.Id,
            ExpectedAccountVersion = account.Version,
            QuoteId = _quoteId,
            InstallmentIds = _selectedInstallments.Order().ToArray(),
            ExpectedDocumentVersion = _regenerate?.DocumentVersion
        };
        var sourceId = _regenerate?.Document.Id;
        _error = null;
        await Complete(await _submission.Submit("generating instruction revision", sourceId is { } id
            ? (key, ct) => InstructionsApi.Regenerate(BookingId, id, input, key, ct)
            : (key, ct) => InstructionsApi.Generate(BookingId, input, key, ct), CancellationToken.None));
    }
    private async Task Confirm()
    {
        if (!_confirmed || _selected is not { } selected || Blocked)
        {
            return;
        }
        var reason = _action switch
        {
            "approve" => FinancialReasonDto.InstructionApproved,
            "finalize" => FinancialReasonDto.InstructionFinalized,
            "withdraw" => FinancialReasonDto.ApprovalWithdrawn,
            "void" => FinancialReasonDto.InstructionVoided,
            _ => FinancialReasonDto.None
        };
        var input = new UpdatePaymentInstructionsDto { ExpectedDocumentVersion = selected.DocumentVersion, Reason = reason, Confirmed = true };
        var review = new ReviewPaymentInstructionsDto { ExpectedDocumentVersion = selected.DocumentVersion, ChangesRequested = _changesRequested };
        Func<string, CancellationToken, Task<HttpOutcome<GetPaymentInstructionDto>>>? command = _action switch
        {
            "approve" => (key, ct) => InstructionsApi.Approve(selected.Document.Id, input, key, ct),
            "finalize" => (key, ct) => InstructionsApi.Finalize(selected.Document.Id, input, key, ct),
            "withdraw" => (key, ct) => InstructionsApi.WithdrawApproval(selected.Document.Id, input, key, ct),
            "void" => (key, ct) => InstructionsApi.Void(selected.Document.Id, input, key, ct),
            "review" => (key, ct) => InstructionsApi.Review(selected.Document.Id, review, key, ct),
            _ => null
        };
        if (command is null)
        {
            return;
        }
        _error = null;
        await Complete(await _submission.Submit(_action, command, CancellationToken.None));
    }
    private async Task Retry() => await Complete(await _submission.Retry(CancellationToken.None));
    private async Task Complete(HttpOutcome<GetPaymentInstructionDto>? result)
    {
        if (result is null)
        {
            return;
        }
        if (result.Kind == HttpOutcomeKind.Succeeded)
        {
            Cancel();
            _regenerate = null;
            await Load();
        }
        else if (result.Kind == HttpOutcomeKind.Conflict)
        {
            Cancel();
            _regenerate = null;
            await Load();
            _error = "Instruction or source terms changed. Refresh and review before confirming again.";
        }
        else
        {
            _error = new HttpOutcomeMessages("The instruction action could not be completed. Check current terms, recipient verification and quote expiry.").For(result.Kind);
        }
    }
}
