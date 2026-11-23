using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

public sealed partial class Instructions
{
    private const BookingFinancialSection QuoteSection = BookingFinancialSection.InstructionSettlementQuoteChoices;
    private const BookingFinancialSection InstallmentSection = BookingFinancialSection.InstructionOutstandingInstallmentChoices;
    private GetBookingFinancialPageDto? _quoteChoicePage;
    private GetBookingFinancialPageDto? _installmentChoicePage;
    private GetPaymentQuoteDto? _selectedQuote;
    private bool _loadingFinancialChoices;

    private IReadOnlyList<GetPaymentQuoteDto> QuoteChoices => _quoteChoicePage?.SettlementQuoteChoices ?? [];
    private IReadOnlyList<GetPaymentInstallmentDto> InstallmentChoices => _installmentChoicePage?.ApprovedInstallmentChoices ?? [];
    private BookingFinancialPageWindowDto? QuoteWindow => _quoteChoicePage?.Windows.SingleOrDefault(window => window.Section == QuoteSection);
    private BookingFinancialPageWindowDto? InstallmentWindow => _installmentChoicePage?.Windows.SingleOrDefault(window => window.Section == InstallmentSection);

    private async Task LoadFinancialChoices(Guid bookingId)
    {
        var quotes = ReadChoicePage(bookingId, QuoteSection, QuoteWindow?.Page ?? 1);
        var installments = ReadChoicePage(bookingId, InstallmentSection, InstallmentWindow?.Page ?? 1);
        await Task.WhenAll(quotes, installments);
        if (bookingId != BookingId)
        {
            return;
        }

        if (await quotes is not { Kind: HttpOutcomeKind.Succeeded, Value: { } quotePage } ||
            await installments is not { Kind: HttpOutcomeKind.Succeeded, Value: { } installmentPage })
        {
            ResetInstructionChoices();
            _error = "Approve a EUR schedule before generating instructions.";
            return;
        }

        if (quotePage.History.Version != installmentPage.History.Version)
        {
            ResetInstructionChoices();
            _error = "Financial terms changed while choices were loading. Refresh before selecting terms.";
            return;
        }

        if (_history is { } previous && previous.Version != quotePage.History.Version)
        {
            ClearFinancialSelections();
        }
        _history = quotePage.History;
        _quoteChoicePage = quotePage;
        _installmentChoicePage = installmentPage;
    }

    private async Task NavigateFinancialChoices(BookingFinancialSection section, int page)
    {
        if (Blocked)
        {
            return;
        }

        var bookingId = BookingId;
        _loadingFinancialChoices = true;
        _error = null;
        try
        {
            var result = await ReadChoicePage(bookingId, section, page);
            if (bookingId != BookingId)
            {
                return;
            }
            if (result is not { Kind: HttpOutcomeKind.Succeeded, Value: { } choicePage })
            {
                _error = "Financial choices could not be loaded.";
                return;
            }

            if (choicePage.History.Version != _history?.Version)
            {
                ResetInstructionChoices();
                await LoadFinancialChoices(bookingId);
                if (bookingId == BookingId && _history is not null)
                {
                    _error = "Financial terms changed. Select the quote and installments again.";
                }
            }
            else if (section == QuoteSection)
            {
                _quoteChoicePage = choicePage;
            }
            else
            {
                _installmentChoicePage = choicePage;
            }
        }
        finally
        {
            _loadingFinancialChoices = false;
        }
    }

    private Task<HttpOutcome<GetBookingFinancialPageDto>> ReadChoicePage(Guid bookingId, BookingFinancialSection section, int page) =>
        PaymentsApi.GetPage(bookingId, new BookingFinancialPageRequestDto { Section = section, Page = page }, CancellationToken.None);

    private void SelectQuote() => _selectedQuote = QuoteChoices.FirstOrDefault(item => item.Id == _quoteId) ??
        (_selectedQuote?.Id == _quoteId ? _selectedQuote : null);

    private void ResetInstructionChoices()
    {
        _history = null;
        _quoteChoicePage = null;
        _installmentChoicePage = null;
        ClearFinancialSelections();
    }

    private void ClearFinancialSelections()
    {
        _quoteId = null;
        _selectedQuote = null;
        _selectedInstallments.Clear();
    }
}
