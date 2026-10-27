using System.Net;
using System.Text;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

/// <summary>Fake HTTP handler serving canned payment payloads and recording submitted requests.</summary>
internal sealed class PaymentPageHttpHandler : HttpMessageHandler
{
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
    public List<string> Paths { get; } = [];
    /// <summary>Idempotency keys of submitted non-GET requests.</summary>
    public List<string> Keys { get; } = [];
    /// <summary>Request bodies of submitted non-GET requests.</summary>
    public List<string> Payloads { get; } = [];
    /// <summary>When true, the next submission throws once and the flag resets.</summary>
    public bool FailNextSubmission
    {
        get; set;
    }
    /// <summary>JSON body returned for booking financial history reads.</summary>
    public string FinancialHistory { get; set; } = """
        {"bookingId":"11111111-1111-1111-1111-111111111111","version":0,"obligationCurrency":1,"termsStale":true,
        "approvedObligationEuro":0,"outstandingEuro":0,"unallocatedEuroCredit":0,"businessDate":"2030-01-05","overdue":false,
        "schedules":[],"quotes":[],"receipts":[],"reconciliations":[],"allocations":[],"refunds":[],"reversals":[],"reviews":[]}
        """;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Method != HttpMethod.Get)
        {
            Paths.Add(request.RequestUri?.AbsolutePath ?? string.Empty);
            Keys.Add(request.Headers.GetValues("Idempotency-Key").Single());
            Payloads.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
            if (FailNextSubmission)
            {
                FailNextSubmission = false;
                throw new HttpRequestException("Private bank data must never reach the UI");
            }
            var response = request.RequestUri?.AbsolutePath.Contains("payment-accounts", StringComparison.Ordinal) == true ? PaymentPageData.Account
                : request.RequestUri?.AbsolutePath.Contains("payment-instructions", StringComparison.Ordinal) == true ? PaymentPageData.Instruction
                : "{\"bookingId\":\"11111111-1111-1111-1111-111111111111\",\"resourceId\":\"22222222-2222-2222-2222-222222222222\"}";
            return new HttpResponseMessage(SubmissionStatus) { Content = new StringContent(response, Encoding.UTF8, "application/json") };
        }
        var path = request.RequestUri?.AbsolutePath ?? string.Empty;
        var json = path.EndsWith("/finances", StringComparison.Ordinal) || path.EndsWith("/finances/", StringComparison.Ordinal) || path.EndsWith("/details", StringComparison.Ordinal) ? FinancialHistory
            : path.Contains("payment-instructions", StringComparison.Ordinal) ? Instructions
            : path.EndsWith("/recipient", StringComparison.Ordinal) ? Recipient : Accounts;
        return new HttpResponseMessage(ReadStatus) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }
}
