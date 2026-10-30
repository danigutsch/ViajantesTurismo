namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Classifies why a finalized document revision was voided without recording free-form text.</summary>
public enum DocumentVoidReason
{
    /// <summary>Authorized staff voided the revision.</summary>
    AdministrativeVoid = 0,

    /// <summary>Approval was withdrawn after the revision was finalized.</summary>
    ApprovalWithdrawn = 1,

    /// <summary>The revision was voided before bounded reasons existed; this value cannot be assigned to new voids.</summary>
    Legacy = 2
}
