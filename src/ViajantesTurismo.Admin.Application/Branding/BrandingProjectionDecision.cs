namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>What the Admin Branding projection does with an incoming Branding revision.</summary>
public enum BrandingProjectionDecision
{
    /// <summary>The revision is newer than the projection and replaces it.</summary>
    Apply,

    /// <summary>The projection already holds this exact revision.</summary>
    AlreadyApplied,

    /// <summary>The revision is older than the projection and is acknowledged without change.</summary>
    Stale,

    /// <summary>The projection holds this revision with different values.</summary>
    RevisionConflict,

    /// <summary>The revision belongs to a source lineage the projection has not accepted.</summary>
    EpochMismatch,

    /// <summary>The event lacks a source epoch or a positive source revision.</summary>
    InvalidEnvelope,

    /// <summary>The logo fields are partial, oversize, of the wrong type or do not match their SHA-256.</summary>
    InvalidLogo
}
