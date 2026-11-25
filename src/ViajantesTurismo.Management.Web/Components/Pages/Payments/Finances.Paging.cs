using SharedKernel.Blazor;
using SharedKernel.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Components.Pages.Payments;

public sealed partial class Finances
{
    private readonly Dictionary<(BookingFinancialSection Section, Guid? ParentId), BookingFinancialPageWindowDto> _windows = [];
    private readonly HashSet<Guid> _attention = [];
    private GetBookingFinancialPageDto? _choices;
    private GetBookingFinancialHistoryDto? _evidence;
    private int _requestSerial;
    private int _authenticationSerial;

    private IReadOnlyList<GetBookingReceiptDto> Receipts => _choices?.ReceiptChoices ?? [];
    private IReadOnlyList<GetPaymentQuoteDto> SettlementQuotes => _choices?.SettlementQuoteChoices ?? [];
    private IReadOnlyList<GetPaymentQuoteDto> RefundQuotes => _choices?.RefundQuoteChoices ?? [];
    private IReadOnlyList<PaymentTargetChoice> InstallmentChoices => _choices?.ApprovedInstallmentChoices
        .Select(item => new PaymentTargetChoice(item.Id, $"{item.DueDate}: {item.OutstandingEuro} EUR outstanding ({item.Id})")).ToArray() ?? [];
    private IReadOnlyList<PaymentTargetChoice> ReceiptInstallmentChoices => _choices?.ProposedInstallmentChoices
        .Select(item => new PaymentTargetChoice(item.Id, $"{item.DueDate}: {item.EuroAmount} EUR proposed terms ({item.Id})")).ToArray() ?? [];
    private IReadOnlyList<PaymentTargetChoice> AllocationChoices => _choices?.ActiveAllocationChoices
        .Select(item => new PaymentTargetChoice(item.Id, $"Receipt {item.ReceiptId}: {item.ActiveEuroCredit} EUR ({item.Id})")).ToArray() ?? [];
    private bool RequiresAttention(Guid id) => _attention.Contains(id);

    private BookingFinancialPageWindowDto? Window(BookingFinancialSection section, Guid? parentId = null) =>
        _windows.GetValueOrDefault((section, parentId));

    private void ResetProjection()
    {
        _history = null;
        _evidence = null;
        _choices = null;
        _attention.Clear();
        _windows.Clear();
    }

    private void ApplyInitialPage(GetBookingFinancialPageDto page)
    {
        ResetProjection();
        _history = page.History;
        _choices = page;
        AddWindows(page);
    }

    private void AddWindows(GetBookingFinancialPageDto page)
    {
        if (_history is { } history)
        {
            var visibleReviewable = history.Schedules.Select(item => item.Id).Concat(history.Quotes.Select(item => item.Id))
                .Concat(history.Reconciliations.Select(item => item.Id)).Concat(history.Refunds.Select(item => item.Id)).ToHashSet();
            _attention.IntersectWith(visibleReviewable);
            _attention.UnionWith(page.AttentionRequested);
            var visibleParents = visibleReviewable.Concat(history.Reversals.Select(item => item.Id)).ToHashSet();
            foreach (var key in _windows.Keys.Where(key => key.ParentId is { } id && !visibleParents.Contains(id)).ToArray())
            {
                _windows.Remove(key);
            }
        }
        foreach (var window in page.Windows)
        {
            _windows[(window.Section, window.ParentId)] = window;
        }
    }

    private Task Browse(BookingFinancialSection section, int page, Guid? parentId = null) => ReadWindow(section, page, parentId, sensitive: false);

    private Task LoadEvidence(BookingFinancialSection section)
    {
        var page = Window(section)?.Page ?? 1;
        return ReadWindow(section, page, null, sensitive: true);
    }

    private async Task ReadWindow(BookingFinancialSection section, int page, Guid? parentId, bool sensitive)
    {
        if (Blocked || _history is null)
        {
            return;
        }
        _loading = true;
        _error = null;
        _evidence = null;
        var serial = ++_requestSerial;
        var authenticationSerial = _authenticationSerial;
        var bookingId = BookingId;
        var request = new BookingFinancialPageRequestDto { Section = section, Page = page, PageSize = Window(section, parentId)?.PageSize, ParentId = parentId };
        try
        {
            var result = sensitive
                ? await PaymentsApi.GetSensitivePage(bookingId, request, CancellationToken.None)
                : await PaymentsApi.GetPage(bookingId, request, CancellationToken.None);
            if (serial != _requestSerial || authenticationSerial != _authenticationSerial)
            {
                return;
            }
            if (result is not { Kind: HttpOutcomeKind.Succeeded, Value: { } response })
            {
                _error = new HttpOutcomeMessages(sensitive ? "Sensitive evidence could not be loaded." : "Financial records could not be loaded.").For(result.Kind);
                return;
            }
            if (response.History.Version != Version)
            {
                Cancel();
                await Load();
                _error = "Financial history changed. Review the current revision before submitting again.";
                return;
            }
            if (sensitive)
            {
                _evidence = response.History;
                return;
            }
            ApplyWindow(response, section, parentId);
        }
        finally
        {
            if (serial == _requestSerial)
            {
                _loading = false;
            }
        }
    }

    private void ApplyWindow(GetBookingFinancialPageDto page, BookingFinancialSection section, Guid? parentId)
    {
        if (_history is not { } history || _choices is not { } choices)
        {
            return;
        }
        var incoming = page.History;
        _history = section switch
        {
            BookingFinancialSection.Schedules => history with
            {
                Schedules = incoming.Schedules
            },
            BookingFinancialSection.Quotes => history with
            {
                Quotes = incoming.Quotes
            },
            BookingFinancialSection.Receipts => history with
            {
                Receipts = incoming.Receipts
            },
            BookingFinancialSection.Reconciliations => history with
            {
                Reconciliations = incoming.Reconciliations
            },
            BookingFinancialSection.Allocations => history with
            {
                Allocations = incoming.Allocations
            },
            BookingFinancialSection.Refunds => history with
            {
                Refunds = incoming.Refunds
            },
            BookingFinancialSection.Reversals => history with
            {
                Reversals = incoming.Reversals
            },
            BookingFinancialSection.Reviews => history with
            {
                Reviews = incoming.Reviews
            },
            BookingFinancialSection.ScheduleInstallments => history with
            {
                Schedules = ReplaceParent(history.Schedules, incoming.Schedules, parentId, item => item.Id)
            },
            BookingFinancialSection.ReconciliationSelections => history with
            {
                Reconciliations = ReplaceParent(history.Reconciliations, incoming.Reconciliations, parentId, item => item.Id)
            },
            BookingFinancialSection.RefundSelections => history with
            {
                Refunds = ReplaceParent(history.Refunds, incoming.Refunds, parentId, item => item.Id)
            },
            BookingFinancialSection.ReversalLinks => history with
            {
                Reversals = ReplaceParent(history.Reversals, incoming.Reversals, parentId, item => item.Id)
            },
            _ => history
        };
        _choices = section switch
        {
            BookingFinancialSection.ReceiptChoices => choices with
            {
                ReceiptChoices = page.ReceiptChoices
            },
            BookingFinancialSection.SettlementQuoteChoices => choices with
            {
                SettlementQuoteChoices = page.SettlementQuoteChoices
            },
            BookingFinancialSection.RefundQuoteChoices => choices with
            {
                RefundQuoteChoices = page.RefundQuoteChoices
            },
            BookingFinancialSection.ApprovedInstallmentChoices => choices with
            {
                ApprovedInstallmentChoices = page.ApprovedInstallmentChoices
            },
            BookingFinancialSection.ProposedInstallmentChoices => choices with
            {
                ProposedInstallmentChoices = page.ProposedInstallmentChoices
            },
            BookingFinancialSection.ActiveAllocationChoices => choices with
            {
                ActiveAllocationChoices = page.ActiveAllocationChoices
            },
            _ => choices
        };
        AddWindows(page);
    }

    private static IReadOnlyList<T> ReplaceParent<T>(IReadOnlyList<T> items, IReadOnlyList<T> replacement, Guid? parentId, Func<T, Guid> id) =>
        replacement.Count == 1 ? [.. items.Select(item => id(item) == parentId ? replacement[0] : item)] : items;

    private string ReceiptReference(GetBookingReceiptDto receipt) => _evidence?.Receipts.FirstOrDefault(item => item.Id == receipt.Id)?.Reference ?? receipt.Reference;
    private string QuoteSource(GetPaymentQuoteDto quote) => _evidence?.Quotes.FirstOrDefault(item => item.Id == quote.Id)?.Source ?? quote.Source;
    private string QuoteReference(GetPaymentQuoteDto quote) => _evidence?.Quotes.FirstOrDefault(item => item.Id == quote.Id)?.Reference ?? quote.Reference;
    private string RefundReference(GetPaymentRefundDto refund) => _evidence?.Refunds.FirstOrDefault(item => item.Id == refund.Id)?.Reference ?? refund.Reference;
}
