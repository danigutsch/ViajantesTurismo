using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Requests the explicit WithdrawPaymentInstructionApproval action for one immutable document revision.</summary>
public sealed record WithdrawPaymentInstructionApprovalCommand(Guid DocumentId, UpdatePaymentInstructionsDto Input, string ActorId, string? IdempotencyKey);
