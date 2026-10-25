namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>The recovery transition an audit entry records.</summary>
public enum BrandingRecoveryAuditKind
{
    /// <summary>An authorized reseed reapplied the current source epoch.</summary>
    SameEpochReseed = 0,

    /// <summary>An authorized reseed moved the projection to a new source epoch, retiring the old one.</summary>
    EpochTransition = 1
}
