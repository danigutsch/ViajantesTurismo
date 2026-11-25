using SharedKernel.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;
using SharedKernel.Blazor;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

/// <summary>Generates and explicitly approves immutable protected payment instruction revisions.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1515", Justification = "Blazor instantiates public page components.")]
public sealed partial class Instructions : IDisposable
{
    private const string GenerateOperation = "generating instruction revision";

    /// <summary>Gets or sets the owning booking.</summary>
    [Parameter]
    public Guid BookingId
    {
        get; set;
    }
    private GetPaymentInstructionPageDto? _instructionPage;
    private GetPaymentAccountPageDto? _recipientPage;
    private GetPaymentAccountDto? _selectedRecipient;
    private GetBookingFinancialHistoryDto? _history;
    private GetPaymentInstructionDto? _selected;
    private GetPaymentInstructionDto? _regenerate;
    private readonly HashSet<Guid> _selectedInstallments = [];
    private Guid _accountId;
    private Guid _instructionBookingId;
    private Guid? _quoteId;
    private string? _actor;
    private string? _error;
    private string _action = string.Empty;
    private bool _loading;
    private bool _loadingInstructionPage;
    private bool _confirmed;
    private bool _changesRequested;
    private readonly IdempotentSubmission<GetPaymentInstructionDto> _submission = new();
    private bool _loadingRecipients;
    private bool Blocked => _loading || _loadingRecipients || _loadingInstructionPage || _submission.IsBusy || _submission.HasPending;
    /// <inheritdoc />
    public void Dispose() => Authentication.AuthenticationStateChanged -= OnAuthenticationStateChanged;

    /// <inheritdoc />
    protected override void OnInitialized() => Authentication.AuthenticationStateChanged += OnAuthenticationStateChanged;

    private async void OnAuthenticationStateChanged(Task<AuthenticationState> state)
    {
        var actor = (await state).User.GetSubjectId();
        await InvokeAsync(() =>
        {
            _actor = actor;
            StateHasChanged();
        });
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_instructionBookingId != BookingId)
        {
            _instructionBookingId = BookingId;
            _instructionPage = null;
            _regenerate = null;
            _selectedInstallments.Clear();
            _quoteId = null;
            Cancel();
        }
        _actor = (await Authentication.GetAuthenticationStateAsync()).User.GetSubjectId();
        await Load();
    }
    private async Task Load()
    {
        var bookingId = BookingId;
        _loading = true;
        _error = null;
        try
        {
            var financials = await PaymentsApi.GetByBookingId(bookingId, CancellationToken.None);
            if (financials is { Kind: HttpOutcomeKind.Succeeded, Value: { } history })
            {
                _history = history;
            }
            else
            {
                _history = null;
                _error = "Approve a EUR schedule before generating instructions.";
            }
            await LoadRecipients(_recipientPage?.Page ?? 1);
            await RefreshSelectedRecipient();
            await LoadInstructionPage(bookingId, _instructionPage?.Page ?? 1);
        }
        finally { _loading = false; }
    }

    private Task NavigateInstructionPage(int page)
    {
        _error = null;
        return LoadInstructionPage(BookingId, page);
    }

    private async Task LoadInstructionPage(Guid bookingId, int page)
    {
        _loadingInstructionPage = true;
        try
        {
            var outcome = await InstructionsApi.ListPageForBooking(bookingId, page, ContractConstants.DefaultPaymentInstructionPageSize, CancellationToken.None);
            if (bookingId != BookingId)
            {
                return;
            }
            if (outcome is { Kind: HttpOutcomeKind.Succeeded, Value: { } revisions })
            {
                _instructionPage = revisions;
            }
            else
            {
                _error = new HttpOutcomeMessages("Instruction revisions could not be loaded.").For(outcome.Kind);
            }
        }
        finally
        {
            _loadingInstructionPage = false;
        }
    }

    private async Task LoadRecipients(int page)
    {
        _loadingRecipients = true;
        try
        {
            var outcome = await AccountsApi.ListPage(page, ContractConstants.DefaultPaymentAccountPageSize, verifiedOnly: true, CancellationToken.None);
            if (outcome is { Kind: HttpOutcomeKind.Succeeded, Value: { } recipients })
            {
                _recipientPage = recipients;
            }
            else
            {
                _recipientPage = null;
                _selectedRecipient = null;
                _accountId = Guid.Empty;
                _error = "Recipient accounts could not be loaded.";
            }
        }
        finally
        {
            _loadingRecipients = false;
        }
    }

    private void SelectRecipient()
    {
        if (_selectedRecipient?.Id != _accountId)
        {
            _selectedRecipient = _recipientPage?.Items.SingleOrDefault(
                account => account.Id == _accountId && account.Status == PaymentAccountStatusDto.Verified);
        }
    }

    private async Task RefreshSelectedRecipient()
    {
        if (_selectedRecipient is not { } selected)
        {
            return;
        }

        var current = _recipientPage?.Items.SingleOrDefault(account => account.Id == selected.Id);
        if (current is null)
        {
            var outcome = await AccountsApi.GetById(selected.Id, CancellationToken.None);
            current = outcome.Kind == HttpOutcomeKind.Succeeded ? outcome.Value : null;
        }

        _selectedRecipient = current is { Status: PaymentAccountStatusDto.Verified } ? current : null;
        _accountId = _selectedRecipient?.Id ?? Guid.Empty;
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
        if (_history is null || _selectedRecipient is not { } account)
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
            InstallmentIds = [.. _selectedInstallments.Order()],
            ExpectedDocumentVersion = _regenerate?.DocumentVersion
        };
        var sourceId = _regenerate?.Document.Id;
        var bookingId = BookingId;
        _error = null;
        await Complete(await _submission.Submit(GenerateOperation, sourceId is { } id
            ? (key, ct) => InstructionsApi.Regenerate(bookingId, id, input, key, ct)
            : (key, ct) => InstructionsApi.Generate(bookingId, input, key, ct), CancellationToken.None));
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
            if (_submission.Operation == GenerateOperation)
            {
                _instructionPage = null;
            }
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
