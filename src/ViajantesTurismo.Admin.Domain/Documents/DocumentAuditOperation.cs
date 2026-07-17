namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Identifies an auditable document operation.</summary>
public enum DocumentAuditOperation
{
    /// <summary>A customer-facing document draft was requested.</summary>
    Generate = 0,

    /// <summary>A document was read.</summary>
    Read = 1,

    /// <summary>Staff review was started.</summary>
    BeginReview = 2,

    /// <summary>Changes were requested.</summary>
    RequestChanges = 3,

    /// <summary>An editable field was updated.</summary>
    UpdateField = 4,

    /// <summary>A document was approved.</summary>
    Approve = 5,

    /// <summary>A document artifact was finalized.</summary>
    Finalize = 6,

    /// <summary>A replacement document revision was requested.</summary>
    Regenerate = 7,

    /// <summary>A document was voided.</summary>
    Void = 8,

    /// <summary>A finalized artifact was downloaded.</summary>
    Download = 9
}
