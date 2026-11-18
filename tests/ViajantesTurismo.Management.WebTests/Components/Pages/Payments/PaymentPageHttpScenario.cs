using System.Net;
using PandaBear.Testing.Http;

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
        var json = path.EndsWith("/finances", StringComparison.Ordinal) || path.EndsWith("/finances/", StringComparison.Ordinal) || path.EndsWith("/details", StringComparison.Ordinal) ? FinancialHistory
            : path.Contains("payment-instructions", StringComparison.Ordinal) ? Instructions
            : path.EndsWith("/recipient", StringComparison.Ordinal) ? Recipient : Accounts;
        return HttpResponseFactory.Json(json, ReadStatus);
    }
}
