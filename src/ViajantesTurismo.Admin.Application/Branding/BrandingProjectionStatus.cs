namespace ViajantesTurismo.Admin.Application.Branding;

/// <summary>Whether the Admin Branding projection may serve document generation.</summary>
public enum BrandingProjectionStatus
{
    /// <summary>The projection follows ordinary delivery and may be read.</summary>
    Ready = 0,

    /// <summary>The projection is fenced; only an authorized reseed may change it.</summary>
    Recovering = 1
}
