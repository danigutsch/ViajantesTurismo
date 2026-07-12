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

    /// <summary>The maximum branding logo URI length.</summary>
    public const int MaxBrandingLogoUriLength = 2048;

    /// <summary>The maximum branding token length.</summary>
    public const int MaxBrandingTokenLength = 128;

    /// <summary>The maximum field identifier length.</summary>
    public const int MaxFieldIdLength = 64;

    /// <summary>The maximum field label length.</summary>
    public const int MaxFieldLabelLength = 128;

    /// <summary>The maximum field value length.</summary>
    public const int MaxFieldValueLength = 4_000;

    /// <summary>The maximum void reason length.</summary>
    public const int MaxVoidReasonLength = 512;

    /// <summary>The retention period for unfinalized drafts.</summary>
    public const int DraftRetentionDays = 30;

    /// <summary>The provisional retention period for finalized artifacts.</summary>
    public const int FinalizedRetentionYears = 7;
}
