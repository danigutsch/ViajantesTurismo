namespace ViajantesTurismo.Admin.Domain.Payments;

/// <summary>Persisted withdrawal of a previously granted approval.</summary>
/// <param name="Id">Withdrawal identifier.</param>
/// <param name="TargetId">Identifier of the schedule or quote whose approval is withdrawn.</param>
/// <param name="Approval">Approval that authorized the withdrawal.</param>
internal sealed record FinancialApprovalWithdrawalState(Guid Id, Guid TargetId, FinancialApprovalState Approval);
