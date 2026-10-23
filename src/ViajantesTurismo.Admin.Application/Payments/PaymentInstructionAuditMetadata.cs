using ViajantesTurismo.Admin.Domain.Payments;

namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Contains revision-bound audit metadata without account details or financial payloads.</summary>
public sealed record PaymentInstructionAuditMetadata(Guid DocumentId, int Revision, string ActorId, DateTimeOffset OccurredAt,
    PaymentInstructionAuditOperation Operation, FinancialReason Reason, bool SelfApproved);
