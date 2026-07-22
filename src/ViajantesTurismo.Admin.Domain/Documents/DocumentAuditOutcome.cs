namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Describes the outcome of an auditable document operation.</summary>
public enum DocumentAuditOutcome
{
    /// <summary>The operation completed successfully.</summary>
    Succeeded = 0,

    /// <summary>The operation was rejected without completing.</summary>
    Rejected = 1
}
