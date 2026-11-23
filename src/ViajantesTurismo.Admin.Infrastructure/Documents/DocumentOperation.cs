namespace ViajantesTurismo.Admin.Infrastructure.Documents;

/// <summary>Document operations included in the Admin observability contract.</summary>
public enum DocumentOperation
{
    /// <summary>Generates an initial draft.</summary>
    Generate,

    /// <summary>Reads a revision for preview.</summary>
    Preview,

    /// <summary>Reviews a draft.</summary>
    Review,

    /// <summary>Requests changes to a draft.</summary>
    RequestChanges,

    /// <summary>Acknowledges changes in a regenerated draft.</summary>
    AcknowledgeChanges,

    /// <summary>Edits a draft field.</summary>
    UpdateField,

    /// <summary>Approves a draft.</summary>
    Approve,

    /// <summary>Seals a finalized artifact.</summary>
    Finalize,

    /// <summary>Generates a replacement revision.</summary>
    Regenerate,

    /// <summary>Withdraws approval of protected payment instructions.</summary>
    WithdrawApproval,

    /// <summary>Voids a revision.</summary>
    Void,

    /// <summary>Opens a finalized artifact for delivery.</summary>
    Download,

    /// <summary>Purges a batch of expired drafts.</summary>
    PurgeDrafts,

    /// <summary>Purges a batch of expired audit records.</summary>
    PurgeAudits,

    /// <summary>Purges a batch of expired idempotency entries.</summary>
    PurgeIdempotency,
}
