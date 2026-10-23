using System.Net;
using System.Text;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Payments;

internal sealed class PaymentPageHttpHandler : HttpMessageHandler
{
    public string Accounts { get; set; } = "[]";
    public string Recipient { get; set; } = PaymentPageData.Recipient;
    public string Instructions { get; set; } = "[]";
    public HttpStatusCode SubmissionStatus { get; set; } = HttpStatusCode.OK;
    public HttpStatusCode ReadStatus { get; set; } = HttpStatusCode.OK;
    public List<string> Paths { get; } = [];
    public List<string> Keys { get; } = [];
    public List<string> Payloads { get; } = [];
    public bool FailNextSubmission
    {
        get; set;
    }
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
