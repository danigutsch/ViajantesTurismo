using PandaBear.HttpClients;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Contracts.Http;

/// <summary>Provides typed access to EUR booking terms, actual settlement, allocations and append-only corrections.</summary>
public interface IBookingPaymentsApiClient
{
    /// <summary>Gets masked financial history.</summary>
    Task<HttpOutcome<GetBookingFinancialHistoryDto>> GetByBookingId(Guid bookingId, CancellationToken ct);
    /// <summary>Gets explicitly authorized full evidence references, recording a server audit.</summary>
    Task<HttpOutcome<GetBookingFinancialHistoryDto>> GetSensitiveDetails(Guid bookingId, CancellationToken ct);
    /// <summary>Executes DraftSchedule with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> DraftSchedule(Guid bookingId, CreatePaymentScheduleDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes ApproveSchedule with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveSchedule(Guid bookingId, Guid scheduleId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes DraftQuote with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> DraftQuote(Guid bookingId, CreatePaymentQuoteDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes ApproveQuote with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveQuote(Guid bookingId, Guid quoteId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes RecordReceipt with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> RecordReceipt(Guid bookingId, CreateBookingReceiptDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes DraftReconciliation with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> DraftReconciliation(Guid bookingId, Guid receiptId, CreateReceiptReconciliationDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes ApproveReconciliation with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveReconciliation(Guid bookingId, Guid reconciliationId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Allocate with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> Allocate(Guid bookingId, Guid receiptId, AllocatePaymentCreditDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes ReleaseAllocations with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> ReleaseAllocations(Guid bookingId, AllocatePaymentCreditDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes ReverseReceipt with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> ReverseReceipt(Guid bookingId, Guid receiptId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes DraftRefund with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> DraftRefund(Guid bookingId, Guid receiptId, CreatePaymentRefundDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes ApproveRefund with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> ApproveRefund(Guid bookingId, Guid refundId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes CorrectRefund with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> CorrectRefund(Guid bookingId, Guid refundId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes Review with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> Review(Guid bookingId, Guid targetId, ReviewFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
    /// <summary>Executes WithdrawApproval with a stable key retained across retries.</summary>
    Task<HttpOutcome<GetPaymentCommandResultDto>> WithdrawApproval(Guid bookingId, Guid targetId, ApproveFinancialRecordDto input, string idempotencyKey, CancellationToken ct);
}
