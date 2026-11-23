using System.Net;
using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using PandaBear.BuildingBlocks;
using PandaBear.Testing.Http;
using AdminContractConstants = ViajantesTurismo.Admin.Contracts.Application.ContractConstants;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Payment responses and recorded submissions for component tests.</summary>
internal sealed class PaymentPageHttpScenario : IDisposable
{
    /// <summary>Sets up the SharedKernel recording transport for this payment scenario.</summary>
    public PaymentPageHttpScenario()
    {
        Transport = new RecordingHttpMessageHandler(Respond);
    }

    /// <summary>Gets the HTTP transport registered with the component test client.</summary>
    public RecordingHttpMessageHandler Transport
    {
        get;
    }

    /// <summary>JSON body returned for payment account list reads.</summary>
    public string Accounts { get; set; } = "[]";
    /// <summary>JSON body returned for recipient reads.</summary>
    public string Recipient { get; set; } = PaymentPageData.Recipient;
    /// <summary>JSON body returned for payment instruction list reads.</summary>
    public string Instructions { get; set; } = "[]";
    /// <summary>Status code returned for non-GET submissions.</summary>
    public HttpStatusCode SubmissionStatus { get; set; } = HttpStatusCode.OK;
    /// <summary>Status code returned for GET reads.</summary>
    public HttpStatusCode ReadStatus { get; set; } = HttpStatusCode.OK;
    /// <summary>Supplies a separate revision to the next installment choices to simulate a concurrent update.</summary>
    public long? InstructionInstallmentVersion
    {
        get; set;
    }
    /// <summary>Absolute paths of submitted non-GET requests.</summary>
    public IReadOnlyList<string> Paths => [.. Submissions.Select(static request => request.Message.RequestUri?.AbsolutePath ?? string.Empty)];
    /// <summary>Idempotency keys of submitted non-GET requests.</summary>
    public IReadOnlyList<string> Keys => [.. Submissions.Select(static request => request.Message.Headers.GetValues("Idempotency-Key").Single())];
    /// <summary>Request bodies of submitted non-GET requests.</summary>
    public IReadOnlyList<string> Payloads => [.. Submissions.Select(static request => request.Body ?? string.Empty)];
    /// <summary>When true, the next submission throws once and the flag resets.</summary>
    public bool FailNextSubmission
    {
        get; set;
    }
    /// <summary>JSON body returned for booking financial history reads.</summary>
    public string FinancialHistory { get; set; } = """
        {"bookingId":"11111111-1111-1111-1111-111111111111","version":0,"obligationCurrency":"EUR","termsStale":true,
        "approvedObligationEuro":0,"outstandingEuro":0,"unallocatedEuroCredit":0,"businessDate":"2030-01-05","overdue":false,
        "schedules":[],"quotes":[],"receipts":[],"reconciliations":[],"allocations":[],"refunds":[],"reversals":[],"reviews":[]}
        """;
    /// <summary>Unmasked receipt reference returned only on explicit evidence requests.</summary>
    public string? ReceiptEvidence
    {
        get; set;
    }
    private IEnumerable<CapturedHttpRequest> Submissions => Transport.Requests.Where(static request => request.Method != HttpMethod.Get);

    /// <inheritdoc />
    public void Dispose() => Transport.Dispose();

    private HttpResponseMessage Respond(CapturedHttpRequest request)
    {
        var path = request.Message.RequestUri?.AbsolutePath ?? string.Empty;
        if (request.Method != HttpMethod.Get)
        {
            if (FailNextSubmission)
            {
                FailNextSubmission = false;
                throw new HttpRequestException("Private bank data must never reach the UI");
            }
            var response = path.Contains("payment-accounts", StringComparison.Ordinal) ? PaymentPageData.Account
                : path.Contains("payment-instructions", StringComparison.Ordinal) ? PaymentPageData.Instruction
                : "{\"bookingId\":\"11111111-1111-1111-1111-111111111111\",\"resourceId\":\"22222222-2222-2222-2222-222222222222\"}";
            return HttpResponseFactory.Json(response, SubmissionStatus);
        }
        const string accountPrefix = "/api/v1/payment-accounts/";
        if (path.StartsWith(accountPrefix, StringComparison.Ordinal) && Guid.TryParse(path.AsSpan(accountPrefix.Length), out var accountId))
        {
            var account = ReadAccounts().SingleOrDefault(item => item.Id == accountId);
            return account is null
                ? HttpResponseFactory.Json("{}", HttpStatusCode.NotFound)
                : HttpResponseFactory.Json(JsonSerializer.Serialize(account, PaymentAccountContractJsonContext.Default.GetPaymentAccountDto), ReadStatus);
        }

        var queryString = request.Message.RequestUri?.Query ?? string.Empty;
        var json = path.Contains("/finances/projection", StringComparison.Ordinal) ? PageFinances(queryString, path.EndsWith("/details", StringComparison.Ordinal))
            : path.EndsWith("/finances", StringComparison.Ordinal) || path.EndsWith("/finances/", StringComparison.Ordinal) || path.EndsWith("/details", StringComparison.Ordinal) ? FinancialHistory
            : path.EndsWith("/payment-instructions/page", StringComparison.Ordinal) ? PageInstructions(queryString)
            : path.EndsWith("/payment-instructions/reviews", StringComparison.Ordinal) ? PageInstructionReviews(path, queryString)
            : path.Contains("payment-instructions", StringComparison.Ordinal) ? Instructions
            : string.Equals(path, "/api/v1/payment-accounts/page", StringComparison.Ordinal) ? PageAccounts(queryString)
            : path.EndsWith("/recipient", StringComparison.Ordinal) ? Recipient : Accounts;
        return HttpResponseFactory.Json(json, ReadStatus);
    }

    private string PageFinances(string queryString, bool evidence)
    {
        var query = QueryHelpers.ParseQuery(queryString);
        var section = Enum.TryParse<BookingFinancialSection>(query.GetValueOrDefault("section").ToString(), out var requested) ? requested : (BookingFinancialSection?)null;
        var pageNumber = int.TryParse(query.GetValueOrDefault("page").ToString(), CultureInfo.InvariantCulture, out var page) ? page : (int?)null;
        var pageSize = int.TryParse(query.GetValueOrDefault("pageSize").ToString(), CultureInfo.InvariantCulture, out var size) ? size : AdminContractConstants.DefaultBookingFinancialPageSize;
        var json = PaymentFinancialContractJsonContext.Default;
        var history = JsonSerializer.Deserialize(FinancialHistory, json.GetBookingFinancialHistoryDto).ShouldNotBeNull();
        var windows = new List<BookingFinancialPageWindowDto>();
        var instructionQuotes = section == BookingFinancialSection.InstructionSettlementQuoteChoices;
        var instructionInstallments = section == BookingFinancialSection.InstructionOutstandingInstallmentChoices;
        if (instructionInstallments && InstructionInstallmentVersion is { } revision)
        {
            history = history with
            {
                Version = revision
            };
        }
        var settlementSection = instructionQuotes ? BookingFinancialSection.InstructionSettlementQuoteChoices : BookingFinancialSection.SettlementQuoteChoices;
        var installmentSection = instructionInstallments ? BookingFinancialSection.InstructionOutstandingInstallmentChoices : BookingFinancialSection.ApprovedInstallmentChoices;
        var installments = history.Schedules.LastOrDefault(item => item.Status == PaymentScheduleStatusDto.Approved)?.Installments ?? [];
        var receiptPage = Window(BookingFinancialSection.Receipts, history.Receipts);
        if (evidence && ReceiptEvidence is { } reference)
        {
            receiptPage = [.. receiptPage.Select(receipt => receipt with
            {
                Reference = reference
            })];
        }
        var result = new GetBookingFinancialPageDto
        {
            History = history with
            {
                Schedules = Window(BookingFinancialSection.Schedules, history.Schedules),
                Quotes = Window(BookingFinancialSection.Quotes, history.Quotes),
                Receipts = receiptPage,
                Reconciliations = Window(BookingFinancialSection.Reconciliations, history.Reconciliations),
                Allocations = Window(BookingFinancialSection.Allocations, history.Allocations),
                Refunds = Window(BookingFinancialSection.Refunds, history.Refunds),
                Reversals = Window(BookingFinancialSection.Reversals, history.Reversals),
                Reviews = Window(BookingFinancialSection.Reviews, history.Reviews)
            },
            Windows = windows,
            AttentionRequested = [.. history.Reviews.GroupBy(item => item.TargetId).Where(items => items.Last().ChangesRequested).Select(items => items.Key)],
            ReceiptChoices = Window(BookingFinancialSection.ReceiptChoices, history.Receipts),
            SettlementQuoteChoices = Window(settlementSection, history.Quotes.Where(quote => !quote.ForRefund &&
                (!instructionQuotes || (quote.Approval.ApprovedAt is not null && !quote.ApprovalWithdrawn))).ToArray()),
            RefundQuoteChoices = Window(BookingFinancialSection.RefundQuoteChoices, history.Quotes.Where(quote => quote.ForRefund).ToArray()),
            ApprovedInstallmentChoices = Window(installmentSection, instructionInstallments ? [.. installments.Where(item => item.OutstandingEuro > 0)] : installments),
            ProposedInstallmentChoices = Window(BookingFinancialSection.ProposedInstallmentChoices, history.Schedules.Count > 0 ? history.Schedules[^1].Installments : []),
            ActiveAllocationChoices = Window(BookingFinancialSection.ActiveAllocationChoices, history.Allocations.Where(item => item.ActiveEuroCredit > 0).ToArray())
        };
        return JsonSerializer.Serialize(result, json.GetBookingFinancialPageDto);

        IReadOnlyList<T> Window<T>(BookingFinancialSection name, IReadOnlyList<T> items)
        {
            if (section is not null && section != name)
            {
                return [];
            }
            var window = PageWindow.Create(pageNumber, pageSize, items.Count);
            windows.Add(new BookingFinancialPageWindowDto(name, null, window.Page, window.PageSize, window.Total, window.PageCount));
            return [.. items.Skip(window.Skip).Take(window.Take)];
        }
    }

    private string PageInstructions(string queryString)
    {
        var query = QueryHelpers.ParseQuery(queryString);
        var page = int.Parse(query["page"].ToString(), CultureInfo.InvariantCulture);
        var pageSize = int.Parse(query["pageSize"].ToString(), CultureInfo.InvariantCulture);
        var latestReviewOnly = bool.TryParse(query["latestReviewOnly"].ToString(), out var latest) && latest;
        var json = PaymentFinancialContractJsonContext.Default;
        var revisions = JsonSerializer.Deserialize(Instructions, json.IReadOnlyListGetPaymentInstructionDto) ?? [];
        var window = PageWindow.Create(page, pageSize, revisions.Count);
        var result = new GetPaymentInstructionPageDto
        {
            Items = [.. revisions.OrderByDescending(item => item.Document.Revision).ThenBy(item => item.Document.Id).Skip(window.Skip).Take(window.Take)
                .Select(item => latestReviewOnly ? item with { Reviews = [.. item.Reviews.TakeLast(1)] } : item)],
            Page = window.Page,
            PageSize = window.PageSize,
            Total = window.Total
        };
        return JsonSerializer.Serialize(result, json.GetPaymentInstructionPageDto);
    }

    private string PageInstructionReviews(string path, string queryString)
    {
        var query = QueryHelpers.ParseQuery(queryString);
        var page = int.Parse(query["page"].ToString(), CultureInfo.InvariantCulture);
        var pageSize = int.Parse(query["pageSize"].ToString(), CultureInfo.InvariantCulture);
        var id = Guid.Parse(path.Split('/')[4]);
        var json = PaymentFinancialContractJsonContext.Default;
        var revisions = JsonSerializer.Deserialize(Instructions, json.IReadOnlyListGetPaymentInstructionDto) ?? [];
        var reviews = revisions.Single(item => item.Document.Id == id).Reviews;
        var window = PageWindow.Create(page, pageSize, reviews.Count);
        var result = new GetPaymentInstructionReviewPageDto
        {
            Items = [.. reviews.Skip(window.Skip).Take(window.Take)],
            Page = window.Page,
            PageSize = window.PageSize,
            Total = window.Total,
        };
        return JsonSerializer.Serialize(result, json.GetPaymentInstructionReviewPageDto);
    }

    private string PageAccounts(string queryString)
    {
        var query = QueryHelpers.ParseQuery(queryString);
        var page = int.Parse(query["page"].ToString(), CultureInfo.InvariantCulture);
        var pageSize = int.Parse(query["pageSize"].ToString(), CultureInfo.InvariantCulture);
        var verifiedOnly = bool.TryParse(query.GetValueOrDefault("verifiedOnly").ToString(), out var verified) && verified;
        var json = PaymentAccountContractJsonContext.Default;
        var matching = ReadAccounts()
            .Where(account => !verifiedOnly || account.Status == PaymentAccountStatusDto.Verified)
            .OrderBy(account => account.Id)
            .ToArray();
        var window = PageWindow.Create(page, pageSize, matching.Length);
        var result = new GetPaymentAccountPageDto
        {
            Items = [.. matching.Skip(window.Skip).Take(window.Take)],
            Page = window.Page,
            PageSize = window.PageSize,
            Total = window.Total
        };
        return JsonSerializer.Serialize(result, json.GetPaymentAccountPageDto);
    }

    private IReadOnlyList<GetPaymentAccountDto> ReadAccounts() =>
        JsonSerializer.Deserialize(Accounts, PaymentAccountContractJsonContext.Default.IReadOnlyListGetPaymentAccountDto) ?? [];
}
