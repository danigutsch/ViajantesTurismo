namespace ViajantesTurismo.Admin.Domain.Payments;

internal sealed record FinancialApprovalWithdrawalState(Guid Id, Guid TargetId, FinancialApprovalState Approval);
