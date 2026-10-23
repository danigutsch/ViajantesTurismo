using PandaBear.Results;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>Maps exact financial inputs to invariant-enforcing domain values.</summary>
public static class FinancialMappings
{
    /// <summary>Maps a bounded reason without accepting undefined values.</summary>
    public static FinancialReason Map(FinancialReasonDto value) => value switch
    {
        FinancialReasonDto.None => FinancialReason.None,
        FinancialReasonDto.InitialObligation => FinancialReason.InitialObligation,
        FinancialReasonDto.TermsChanged => FinancialReason.TermsChanged,
        FinancialReasonDto.CancellationAdjustment => FinancialReason.CancellationAdjustment,
        FinancialReasonDto.QuoteAgreed => FinancialReason.QuoteAgreed,
        FinancialReasonDto.ExactReceipt => FinancialReason.ExactReceipt,
        FinancialReasonDto.LateReceipt => FinancialReason.LateReceipt,
        FinancialReasonDto.ShortReceipt => FinancialReason.ShortReceipt,
        FinancialReasonDto.ExcessReceipt => FinancialReason.ExcessReceipt,
        FinancialReasonDto.FeeDifference => FinancialReason.FeeDifference,
        FinancialReasonDto.MismatchedReceipt => FinancialReason.MismatchedReceipt,
        FinancialReasonDto.AllocationSelected => FinancialReason.AllocationSelected,
        FinancialReasonDto.ReceiptError => FinancialReason.ReceiptError,
        FinancialReasonDto.RefundPerformed => FinancialReason.RefundPerformed,
        FinancialReasonDto.RefundRecordError => FinancialReason.RefundRecordError,
        FinancialReasonDto.ApprovalWithdrawn => FinancialReason.ApprovalWithdrawn,
        FinancialReasonDto.DraftSaved => FinancialReason.DraftSaved,
        FinancialReasonDto.ReceiptRecorded => FinancialReason.ReceiptRecorded,
        FinancialReasonDto.ReviewAccepted => FinancialReason.ReviewAccepted,
        FinancialReasonDto.ReviewChangesRequested => FinancialReason.ReviewChangesRequested,
        FinancialReasonDto.InstructionGenerated => FinancialReason.InstructionGenerated,
        FinancialReasonDto.InstructionApproved => FinancialReason.InstructionApproved,
        FinancialReasonDto.InstructionFinalized => FinancialReason.InstructionFinalized,
        FinancialReasonDto.InstructionDownloaded => FinancialReason.InstructionDownloaded,
        FinancialReasonDto.InstructionVoided => FinancialReason.InstructionVoided,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown financial reason.")
    };
    /// <summary>Maps a persisted bounded reason to its contract counterpart.</summary>
    public static FinancialReasonDto Map(FinancialReason value) => value switch
    {
        FinancialReason.None => FinancialReasonDto.None,
        FinancialReason.InitialObligation => FinancialReasonDto.InitialObligation,
        FinancialReason.TermsChanged => FinancialReasonDto.TermsChanged,
        FinancialReason.CancellationAdjustment => FinancialReasonDto.CancellationAdjustment,
        FinancialReason.QuoteAgreed => FinancialReasonDto.QuoteAgreed,
        FinancialReason.ExactReceipt => FinancialReasonDto.ExactReceipt,
        FinancialReason.LateReceipt => FinancialReasonDto.LateReceipt,
        FinancialReason.ShortReceipt => FinancialReasonDto.ShortReceipt,
        FinancialReason.ExcessReceipt => FinancialReasonDto.ExcessReceipt,
        FinancialReason.FeeDifference => FinancialReasonDto.FeeDifference,
        FinancialReason.MismatchedReceipt => FinancialReasonDto.MismatchedReceipt,
        FinancialReason.AllocationSelected => FinancialReasonDto.AllocationSelected,
        FinancialReason.ReceiptError => FinancialReasonDto.ReceiptError,
        FinancialReason.RefundPerformed => FinancialReasonDto.RefundPerformed,
        FinancialReason.RefundRecordError => FinancialReasonDto.RefundRecordError,
        FinancialReason.ApprovalWithdrawn => FinancialReasonDto.ApprovalWithdrawn,
        FinancialReason.DraftSaved => FinancialReasonDto.DraftSaved,
        FinancialReason.ReceiptRecorded => FinancialReasonDto.ReceiptRecorded,
        FinancialReason.ReviewAccepted => FinancialReasonDto.ReviewAccepted,
        FinancialReason.ReviewChangesRequested => FinancialReasonDto.ReviewChangesRequested,
        FinancialReason.InstructionGenerated => FinancialReasonDto.InstructionGenerated,
        FinancialReason.InstructionApproved => FinancialReasonDto.InstructionApproved,
        FinancialReason.InstructionFinalized => FinancialReasonDto.InstructionFinalized,
        FinancialReason.InstructionDownloaded => FinancialReasonDto.InstructionDownloaded,
        FinancialReason.InstructionVoided => FinancialReasonDto.InstructionVoided,
        _ => throw new ArgumentOutOfRangeException(nameof(value), "Unknown financial reason.")
    };
    /// <summary>Validates explicitly selected installment or allocation targets.</summary>
    public static Result<IReadOnlyList<InstallmentAllocation>> Allocations(IReadOnlyList<InstallmentAllocationDto>? inputs)
    {
        if (inputs is null || inputs.Any(item => item is null))
        {
            return FinancialErrors.Invalid("allocations").ConvertError<IReadOnlyList<InstallmentAllocation>>();
        }
        var result = new List<InstallmentAllocation>();
        foreach (var input in inputs)
        {
            var selected = InstallmentAllocation.Create(input.TargetId, input.EuroAmount);
            if (selected.IsFailure)
            {
                return selected.ConvertError<InstallmentAllocation, IReadOnlyList<InstallmentAllocation>>();
            }
            result.Add(selected.Value);
        }
        return Result.Ok<IReadOnlyList<InstallmentAllocation>>(result.AsReadOnly());
    }

    /// <summary>Validates exact EUR installments without mutating approved terms.</summary>
    public static Result<IReadOnlyList<PaymentInstallment>> Installments(IReadOnlyList<PaymentInstallmentDto>? inputs)
    {
        if (inputs is null || inputs.Any(item => item is null))
        {
            return FinancialErrors.Invalid("installments").ConvertError<IReadOnlyList<PaymentInstallment>>();
        }
        var result = new List<PaymentInstallment>();
        foreach (var input in inputs)
        {
            var installment = PaymentInstallment.Create(input.EuroAmount, input.DueDate);
            if (installment.IsFailure)
            {
                return installment.ConvertError<PaymentInstallment, IReadOnlyList<PaymentInstallment>>();
            }
            result.Add(installment.Value);
        }
        return Result.Ok<IReadOnlyList<PaymentInstallment>>(result.AsReadOnly());
    }
}
