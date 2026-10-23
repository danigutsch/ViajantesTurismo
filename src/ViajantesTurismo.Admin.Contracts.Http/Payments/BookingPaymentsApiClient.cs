using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Implements typed financial commands without logging account or financial payloads.</summary>
public sealed class BookingPaymentsApiClient(HttpClient httpClient) : IBookingPaymentsApiClient
{
    private const string Prefix = "/api/v1/bookings";
    private static readonly PaymentFinancialContractJsonContext Json = PaymentFinancialContractJsonContext.Default;
    private static JsonTypeInfo<ContractValidationProblemDto> Validation => PaymentHttpProblemJsonContext.Default.ContractValidationProblemDto;
    /// <inheritdoc />
    public Task<HttpOutcome<GetBookingFinancialHistoryDto>> GetByBookingId(Guid bookingId, CancellationToken ct) => Read($"{Prefix}/{bookingId}/finances", ct);
    /// <inheritdoc />
    public Task<HttpOutcome<GetBookingFinancialHistoryDto>> GetSensitiveDetails(Guid bookingId, CancellationToken ct) => Read($"{Prefix}/{bookingId}/finances/details", ct);

    private Task<HttpOutcome<GetBookingFinancialHistoryDto>> Read(string route, CancellationToken ct) => HttpOutcomeMapper.Send(httpClient,
        new HttpRequestMessage(HttpMethod.Get, new Uri(route, UriKind.Relative)), HttpStatusCode.OK, Json.GetBookingFinancialHistoryDto, Validation, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> DraftSchedule(Guid bookingId, CreatePaymentScheduleDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/schedules", input, Json.CreatePaymentScheduleDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveSchedule(Guid bookingId, Guid scheduleId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/schedules/{scheduleId}/approve", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> DraftQuote(Guid bookingId, CreatePaymentQuoteDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/quotes", input, Json.CreatePaymentQuoteDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveQuote(Guid bookingId, Guid quoteId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/quotes/{quoteId}/approve", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> RecordReceipt(Guid bookingId, CreateBookingReceiptDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/receipts", input, Json.CreateBookingReceiptDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> DraftReconciliation(Guid bookingId, Guid receiptId, CreateReceiptReconciliationDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/receipts/{receiptId}/reconciliations", input, Json.CreateReceiptReconciliationDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveReconciliation(Guid bookingId, Guid reconciliationId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/reconciliations/{reconciliationId}/approve", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> Allocate(Guid bookingId, Guid receiptId, AllocatePaymentCreditDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/receipts/{receiptId}/allocations", input, Json.AllocatePaymentCreditDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> ReleaseAllocations(Guid bookingId, AllocatePaymentCreditDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/allocation-releases", input, Json.AllocatePaymentCreditDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> ReverseReceipt(Guid bookingId, Guid receiptId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/receipts/{receiptId}/reversals", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> DraftRefund(Guid bookingId, Guid receiptId, CreatePaymentRefundDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/receipts/{receiptId}/refunds", input, Json.CreatePaymentRefundDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveRefund(Guid bookingId, Guid refundId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/refunds/{refundId}/approve", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> CorrectRefund(Guid bookingId, Guid refundId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/refunds/{refundId}/corrections", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> Review(Guid bookingId, Guid targetId, ReviewFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/records/{targetId}/review", input, Json.ReviewFinancialRecordDto, idempotencyKey, ct);

    /// <inheritdoc />
    public Task<HttpOutcome<GetPaymentCommandResultDto>> WithdrawApproval(Guid bookingId, Guid targetId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct) =>
        Send($"{Prefix}/{bookingId}/finances/records/{targetId}/withdraw-approval", input, Json.ApproveFinancialRecordDto, idempotencyKey, ct);

    private Task<HttpOutcome<GetPaymentCommandResultDto>> Send<T>(string route, T input, JsonTypeInfo<T> type, string key, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(route, UriKind.Relative)) { Content = JsonContent.Create(input, type) };
        request.Headers.Add(ContractConstants.IdempotencyKeyHeaderName, key);
        return HttpOutcomeMapper.Send(httpClient, request, HttpStatusCode.OK, Json.GetPaymentCommandResultDto, Validation, ct);
    }
}
