namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>Classifies an audit outcome without recording personal data or free-form content.</summary>
public enum DocumentAuditReasonCode
{
    /// <summary>No additional reason is required.</summary>
    None = 0,

    /// <summary>The operation was manually requested by authorized staff.</summary>
    ManualOperation = 1,

    /// <summary>A document was not found.</summary>
    DocumentNotFound = 2,

    /// <summary>A source booking was not found.</summary>
    BookingNotFound = 3,

    /// <summary>The document or booking state did not allow the operation.</summary>
    StateConflict = 4,

    /// <summary>Request validation prevented the operation.</summary>
    ValidationRejected = 5,

    /// <summary>A finalized artifact was not available.</summary>
    ArtifactUnavailable = 6,

    /// <summary>A document finalization was manually requested by authorized staff.</summary>
    ManualFinalize = 7,

    /// <summary>A document was manually voided by authorized staff.</summary>
    ManualVoid = 8,

    /// <summary>A replacement revision was manually requested by authorized staff.</summary>
    ManualRegeneration = 9,

    /// <summary>A source tour was not found.</summary>
    TourNotFound = 10,

    /// <summary>Branding could not be captured from a ready source, so no revision was created.</summary>
    BrandingUnavailable = 11,

    /// <summary>A disposition hold was manually placed by authorized staff.</summary>
    ManualHoldPlacement = 12,

    /// <summary>A disposition hold was manually released by authorized staff.</summary>
    ManualHoldRelease = 13,

    /// <summary>A hold was not found on the lineage of the document.</summary>
    HoldNotFound = 14
}
