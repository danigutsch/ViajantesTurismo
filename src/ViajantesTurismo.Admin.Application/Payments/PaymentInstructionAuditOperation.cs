namespace ViajantesTurismo.Admin.Application.Payments;

/// <summary>Defines metadata-only payment instruction audit operations.</summary>
public enum PaymentInstructionAuditOperation
{
    /// <summary>Generated an immutable draft.</summary>
    Generated,
    /// <summary>Explicitly approved the current confirmation summary.</summary>
    Approved,
    /// <summary>Finalized a freshly eligible protected artifact.</summary>
    Finalized,
    /// <summary>Released a freshly eligible artifact for authenticated download.</summary>
    Downloaded,
    /// <summary>Accepted an independent revision review.</summary>
    ReviewAccepted,
    /// <summary>Requested attention without withdrawing existing approval.</summary>
    ReviewChangesRequested,
    /// <summary>Explicitly withdrew approval.</summary>
    ApprovalWithdrawn,
    /// <summary>Explicitly voided the instruction.</summary>
    Voided
}
