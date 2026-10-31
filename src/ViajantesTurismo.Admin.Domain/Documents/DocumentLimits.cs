namespace ViajantesTurismo.Admin.Domain.Documents;

/// <summary>
/// Defines validation and retention limits for generated documents.
/// </summary>
public static class DocumentLimits
{
    /// <summary>The maximum template identifier length.</summary>
    public const int MaxTemplateIdLength = 128;

    /// <summary>The maximum template version length.</summary>
    public const int MaxTemplateVersionLength = 64;

    /// <summary>The maximum source-version length.</summary>
    public const int MaxSourceVersionLength = 64;

    /// <summary>The maximum branding-version length.</summary>
    public const int MaxBrandingVersionLength = 64;

    /// <summary>The maximum branding display-name length.</summary>
    public const int MaxBrandingNameLength = 128;

    /// <summary>The maximum branding logo data URI length; a 40 KiB WebP logo encodes to about 54,600 characters.</summary>
    public const int MaxBrandingLogoUriLength = 60_000;

    /// <summary>The maximum branding token length.</summary>
    public const int MaxBrandingTokenLength = 128;

    /// <summary>The maximum field identifier length.</summary>
    public const int MaxFieldIdLength = 64;

    /// <summary>The maximum field label length.</summary>
    public const int MaxFieldLabelLength = 128;

    /// <summary>The maximum staff-entered field override length.</summary>
    public const int MaxStaffOverrideLength = 4_000;

    /// <summary>The maximum persisted void reason code length.</summary>
    public const int MaxVoidReasonLength = 64;

    /// <summary>The retention period for unfinalized drafts.</summary>
    public const int DraftRetentionDays = 30;

}
