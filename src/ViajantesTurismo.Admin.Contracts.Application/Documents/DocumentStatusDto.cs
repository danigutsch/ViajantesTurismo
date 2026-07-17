namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Represents the lifecycle state of a generated document revision.</summary>
public enum DocumentStatusDto
{
    /// <summary>Source data produced an editable draft.</summary>
    DraftGenerated = 0,

    /// <summary>Staff is reviewing the draft.</summary>
    InReview = 1,

    /// <summary>Staff requested corrections before approval.</summary>
    ChangesRequested = 2,

    /// <summary>Staff approved the draft for finalization.</summary>
    Approved = 3,

    /// <summary>The final artifact was created and sealed.</summary>
    Finalized = 4,

    /// <summary>A newer finalized revision replaced this artifact.</summary>
    Superseded = 5,

    /// <summary>The document must not be used.</summary>
    Voided = 6
}
