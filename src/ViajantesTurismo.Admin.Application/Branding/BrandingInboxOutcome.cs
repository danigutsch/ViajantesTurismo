namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>How the Admin Branding inbox settled one delivered event.</summary>
public enum BrandingInboxOutcome
{
    /// <summary>The event replaced the projection.</summary>
    Applied,

    /// <summary>The event matched or predated the projection and changed nothing.</summary>
    Stale,

    /// <summary>The event was rejected and is kept for diagnosis and audited replay.</summary>
    Rejected
}
