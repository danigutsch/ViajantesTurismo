using System.Text.Json;
using System.Text.Json.Serialization;

namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Owns JSON metadata for booking financial contracts.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
[JsonSerializable(typeof(UpdatePaymentInstructionsDto))]
[JsonSerializable(typeof(ReviewPaymentInstructionsDto))]
[JsonSerializable(typeof(GetPaymentInstructionDto))]
[JsonSerializable(typeof(IReadOnlyList<GetPaymentInstructionDto>))]
[JsonSerializable(typeof(GetPaymentCommandResultDto))]
[JsonSerializable(typeof(PaymentInstallmentDto))]
[JsonSerializable(typeof(InstallmentAllocationDto))]
[JsonSerializable(typeof(CreatePaymentScheduleDto))]
[JsonSerializable(typeof(CreatePaymentQuoteDto))]
[JsonSerializable(typeof(ApproveFinancialRecordDto))]
[JsonSerializable(typeof(ReviewFinancialRecordDto))]
[JsonSerializable(typeof(CreateBookingReceiptDto))]
[JsonSerializable(typeof(CreateReceiptReconciliationDto))]
[JsonSerializable(typeof(AllocatePaymentCreditDto))]
[JsonSerializable(typeof(CreatePaymentRefundDto))]
[JsonSerializable(typeof(GeneratePaymentInstructionsDto))]
[JsonSerializable(typeof(FinancialApprovalDto))]
[JsonSerializable(typeof(GetPaymentInstallmentDto))]
[JsonSerializable(typeof(GetPaymentScheduleDto))]
[JsonSerializable(typeof(GetPaymentQuoteDto))]
[JsonSerializable(typeof(GetBookingReceiptDto))]
[JsonSerializable(typeof(GetReceiptReconciliationDto))]
[JsonSerializable(typeof(GetPaymentAllocationDto))]
[JsonSerializable(typeof(GetPaymentRefundDto))]
[JsonSerializable(typeof(GetFinancialReversalDto))]
[JsonSerializable(typeof(GetFinancialReviewDto))]
[JsonSerializable(typeof(GetBookingFinancialHistoryDto))]
public sealed partial class PaymentFinancialContractJsonContext : JsonSerializerContext;
